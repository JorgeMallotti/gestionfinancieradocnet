using FluentValidation;

using GestionFinanciera.Application.Common.Audit;
using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Accounts.Interfaces;
using GestionFinanciera.Application.Features.Claims.DTOs;
using GestionFinanciera.Application.Features.Claims.Interfaces;
using GestionFinanciera.Application.Features.Movements.Interfaces;
using GestionFinanciera.Application.Features.Notifications.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Claims;

/// <summary>
/// Claim business rules (Admin = mediator, ledger stays immutable):
/// - only an account involved in the disputed movement can open a claim;
/// - the Admin proposes a corrective transfer (who returns what to whom);
/// - BOTH parties must consent before anything moves (a refusal closes the claim);
/// - when both consent, the corrective movement is executed and STACKED on the
///   ledger (CorrectsMovementId → the original) — nothing is ever overwritten.
/// </summary>
public sealed class ClaimService(
    IClaimRepository claims,
    IMovementRepository movements,
    IAccountRepository accounts,
    IAuditService audit,
    INotificationService notifications,
    IValidator<OpenClaimDto> openValidator,
    IValidator<ProposeCorrectionDto> proposeValidator) : IClaimService
{
    public async Task<Result<ClaimDto>> OpenAsync(
        OpenClaimDto dto, Guid companyId, Guid claimantUserId, CancellationToken ct)
    {
        var validation = await openValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<ClaimDto>.Failure(validation.Errors.First().ErrorMessage);

        var claimant = await accounts.GetByOwnerUserIdAsync(companyId, claimantUserId, ct);
        if (claimant is null || claimant.IsTreasury)
            return Result<ClaimDto>.Failure(ErrorCode.NotFound, "Client account not found.");

        if (claimant.Status != AccountStatus.Active)
            return Result<ClaimDto>.Failure(ErrorCode.Forbidden, "Your account is not active.");

        var movement = await movements.GetByIdAsync(dto.MovementId, companyId, ct);
        if (movement is null)
            return Result<ClaimDto>.Failure(ErrorCode.NotFound, "Movement not found.");

        // Only the two accounts of the movement can dispute it.
        bool isInvolved = movement.FromAccountId == claimant.Id || movement.ToAccountId == claimant.Id;
        if (!isInvolved)
            return Result<ClaimDto>.Failure(ErrorCode.Forbidden, "You can only claim a movement you are part of.");

        var claim = new Claim
        {
            CompanyId = companyId,
            MovementId = movement.Id,
            ClaimantAccountId = claimant.Id,
            Reason = dto.Reason.Trim(),
            Status = ClaimStatus.Open,
        };

        await claims.AddAsync(claim, ct);
        await audit.RecordAsync(
            companyId, claimantUserId, AuditAction.Create, nameof(Claim), claim.Id,
            beforeJson: null, afterJson: AuditJson.Serialize(claim.ToAuditSnapshot()), ipAddress: null, ct);

        return Result<ClaimDto>.Success(ClaimDto.FromEntity(claim));
    }

    public async Task<Result<IReadOnlyList<ClaimDto>>> GetMyClaimsAsync(
        Guid companyId, Guid clientUserId, CancellationToken ct)
    {
        var client = await accounts.GetByOwnerUserIdAsync(companyId, clientUserId, ct);
        if (client is null)
            return Result<IReadOnlyList<ClaimDto>>.Failure(ErrorCode.NotFound, "Client account not found.");

        var list = await claims.GetByAccountAsync(companyId, client.Id, ct);
        return Result<IReadOnlyList<ClaimDto>>.Success(list.Select(ClaimDto.FromEntity).ToList());
    }

    public async Task<Result<IReadOnlyList<ClaimDto>>> GetAllAsync(
        Guid companyId, string role, ClaimStatus? status, CancellationToken ct)
    {
        if (!IsAdmin(role))
            return Result<IReadOnlyList<ClaimDto>>.Failure(ErrorCode.Forbidden, "Only the bank Admin can view all claims.");

        var list = await claims.GetByCompanyAsync(companyId, status, ct);
        return Result<IReadOnlyList<ClaimDto>>.Success(list.Select(ClaimDto.FromEntity).ToList());
    }

    public async Task<Result<ClaimDto>> ProposeAsync(
        Guid id, ProposeCorrectionDto dto, Guid companyId, Guid adminUserId, string role, CancellationToken ct)
    {
        if (!IsAdmin(role))
            return Result<ClaimDto>.Failure(ErrorCode.Forbidden, "Only the bank Admin can propose corrections.");

        var validation = await proposeValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<ClaimDto>.Failure(validation.Errors.First().ErrorMessage);

        var claim = await claims.GetByIdAsync(id, companyId, ct);
        if (claim is null)
            return Result<ClaimDto>.Failure(ErrorCode.NotFound, "Claim not found.");

        if (claim.Status is ClaimStatus.Resolved or ClaimStatus.Rejected)
            return Result<ClaimDto>.Failure(ErrorCode.Conflict, "This claim is already closed.");

        // The corrective transfer must be between the two accounts of the
        // original movement (payer and payee) — the mediator only picks direction.
        var movement = await movements.GetByIdAsync(claim.MovementId, companyId, ct);
        if (movement is null)
            return Result<ClaimDto>.Failure(ErrorCode.NotFound, "Original movement not found.");

        bool fromIsParty = dto.FromAccountId == movement.FromAccountId || dto.FromAccountId == movement.ToAccountId;
        bool toIsParty = dto.ToAccountId == movement.FromAccountId || dto.ToAccountId == movement.ToAccountId;
        if (!fromIsParty || !toIsParty)
            return Result<ClaimDto>.Failure(
                ErrorCode.Validation, "The corrective transfer must go between the two accounts of the original movement.");

        string beforeJson = AuditJson.Serialize(claim.ToAuditSnapshot());

        claim.Status = ClaimStatus.UnderReview;
        claim.ProposedAmount = dto.Amount;
        claim.CorrectiveFromAccountId = dto.FromAccountId;
        claim.CorrectiveToAccountId = dto.ToAccountId;
        claim.PayerConsented = false;
        claim.PayeeConsented = false;
        claim.ResolutionNote = dto.Note?.Trim();

        await claims.UpdateAsync(claim, ct);
        await audit.RecordAsync(
            companyId, adminUserId, AuditAction.Update, nameof(Claim), claim.Id,
            beforeJson, AuditJson.Serialize(claim.ToAuditSnapshot()), ipAddress: null, ct);

        // Bell: both parties of the proposed correction are asked for consent.
        var fromAccount = await accounts.GetByIdAsync(dto.FromAccountId, companyId, ct);
        var toAccount = await accounts.GetByIdAsync(dto.ToAccountId, companyId, ct);
        if (fromAccount is not null)
            await notifications.NotifyAsync(
                companyId, fromAccount.OwnerUserId, NotificationType.ClaimProposed,
                claim.Id, null, dto.Amount, ct);
        if (toAccount is not null && toAccount.Id != fromAccount?.Id)
            await notifications.NotifyAsync(
                companyId, toAccount.OwnerUserId, NotificationType.ClaimProposed,
                claim.Id, null, dto.Amount, ct);

        return Result<ClaimDto>.Success(ClaimDto.FromEntity(claim));
    }

    public async Task<Result<ClaimDto>> ConsentAsync(
        Guid id, bool approve, Guid companyId, Guid clientUserId, CancellationToken ct)
    {
        var consenting = await accounts.GetByOwnerUserIdAsync(companyId, clientUserId, ct);
        if (consenting is null || consenting.IsTreasury)
            return Result<ClaimDto>.Failure(ErrorCode.NotFound, "Client account not found.");

        var claim = await claims.GetByIdAsync(id, companyId, ct);
        if (claim is null)
            return Result<ClaimDto>.Failure(ErrorCode.NotFound, "Claim not found.");

        if (claim.Status != ClaimStatus.UnderReview)
            return Result<ClaimDto>.Failure(ErrorCode.Conflict, "This claim has no pending correction to consent to.");

        if (claim.CorrectiveFromAccountId is null || claim.CorrectiveToAccountId is null || claim.ProposedAmount is null)
            return Result<ClaimDto>.Failure(ErrorCode.Conflict, "No correction has been proposed yet.");

        // Only the two accounts of the proposed correction can consent.
        bool isFrom = consenting.Id == claim.CorrectiveFromAccountId.Value;
        bool isTo = consenting.Id == claim.CorrectiveToAccountId.Value;
        if (!isFrom && !isTo)
            return Result<ClaimDto>.Failure(ErrorCode.Forbidden, "Only the two accounts involved can consent.");

        var from = await accounts.GetByIdAsync(claim.CorrectiveFromAccountId.Value, companyId, ct);
        var to = await accounts.GetByIdAsync(claim.CorrectiveToAccountId.Value, companyId, ct);
        if (from is null || to is null)
            return Result<ClaimDto>.Failure(ErrorCode.NotFound, "Corrective account not found.");

        string beforeJson = AuditJson.Serialize(claim.ToAuditSnapshot());

        // A refusal from either side closes the claim without moving money.
        if (!approve)
        {
            claim.Status = ClaimStatus.Rejected;
            claim.ResolutionNote = "One of the parties refused the proposed correction.";

            await claims.UpdateAsync(claim, ct);
            await audit.RecordAsync(
                companyId, clientUserId, AuditAction.Update, nameof(Claim), claim.Id,
                beforeJson, AuditJson.Serialize(claim.ToAuditSnapshot()), ipAddress: null, ct);

            return Result<ClaimDto>.Success(ClaimDto.FromEntity(claim));
        }

        bool resolvedNow = false;
        if (isFrom)
            claim.PayerConsented = true;
        if (isTo)
            claim.PayeeConsented = true;

        // Execute only when BOTH parties have consented — the corrective
        // movement is stacked on the ledger (never overwrites the original).
        if (claim.PayerConsented && claim.PayeeConsented)
        {
            if (from.Status != AccountStatus.Active || to.Status != AccountStatus.Active)
                return Result<ClaimDto>.Failure(ErrorCode.Conflict, "One of the accounts is not active.");

            // No account can go negative — even a mediated correction.
            if (from.Balance < claim.ProposedAmount.Value)
                return Result<ClaimDto>.Failure(
                    ErrorCode.Conflict, "The paying account does not have enough funds for the correction.");

            var corrective = new Movement
            {
                CompanyId = companyId,
                FromAccountId = claim.CorrectiveFromAccountId.Value,
                ToAccountId = claim.CorrectiveToAccountId.Value,
                Type = MovementType.CorrectiveTransfer,
                Amount = claim.ProposedAmount.Value,
                Currency = from.Currency,
                CorrectsMovementId = claim.MovementId,
                Description = $"Corrective transfer after claim on {claim.MovementId}",
                OccurredAt = DateTimeOffset.UtcNow,
            };

            await movements.AddAsync(corrective, ct);

            await audit.RecordAsync(
                companyId, clientUserId, AuditAction.Create, nameof(Movement), corrective.Id,
                beforeJson: null, afterJson: AuditJson.Serialize(corrective.ToAuditSnapshot()),
                ipAddress: null, ct);

            claim.Status = ClaimStatus.Resolved;
            claim.ResolutionMovementId = corrective.Id;
            claim.ResolutionNote = "Both parties consented — corrective transfer executed.";
            resolvedNow = true;
        }

        await claims.UpdateAsync(claim, ct);
        await audit.RecordAsync(
            companyId, clientUserId, AuditAction.Update, nameof(Claim), claim.Id,
            beforeJson, AuditJson.Serialize(claim.ToAuditSnapshot()), ipAddress: null, ct);

        // Bell: after the first consent, nudge the counterparty; on resolution,
        // tell both parties the correction was executed.
        if (resolvedNow)
        {
            await notifications.NotifyAsync(
                companyId, from.OwnerUserId, NotificationType.ClaimResolved,
                claim.Id, null, claim.ProposedAmount.Value, ct);
            await notifications.NotifyAsync(
                companyId, to.OwnerUserId, NotificationType.ClaimResolved,
                claim.Id, null, claim.ProposedAmount.Value, ct);
        }
        else
        {
            Guid counterpartyUserId = isFrom ? to.OwnerUserId : from.OwnerUserId;
            await notifications.NotifyAsync(
                companyId, counterpartyUserId, NotificationType.ClaimCounterpartyConsented,
                claim.Id, consenting.DisplayName, claim.ProposedAmount.Value, ct);
        }

        return Result<ClaimDto>.Success(ClaimDto.FromEntity(claim));
    }

    private static bool IsAdmin(string role) => role == nameof(UserRole.Admin);
}
