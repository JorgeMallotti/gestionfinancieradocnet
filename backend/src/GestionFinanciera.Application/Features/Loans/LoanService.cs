using FluentValidation;

using GestionFinanciera.Application.Common.Audit;
using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Accounts.Interfaces;
using GestionFinanciera.Application.Features.Loans.DTOs;
using GestionFinanciera.Application.Features.Loans.Interfaces;
using GestionFinanciera.Application.Features.Movements.Interfaces;
using GestionFinanciera.Application.Features.Notifications.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Loans;

/// <summary>
/// Loan business rules (simple MVP loans — no interest, no deadlines):
/// - only Active clients can request loans (never the treasury);
/// - approving requires the bank treasury to have the funds (it can never go
///   negative either) — "the bank cannot lend more than it has";
/// - disbursement is a ledger movement treasury → client (append-only);
/// - the client repays anytime, full or partial; repayment is a ledger movement
///   client → treasury; when fully repaid the loan becomes Repaid.
/// </summary>
public sealed class LoanService(
    ILoanRepository loans,
    IAccountRepository accounts,
    IMovementRepository movements,
    IAuditService audit,
    INotificationService notifications,
    IValidator<RequestLoanDto> requestValidator,
    IValidator<DecideLoanDto> decideValidator,
    IValidator<RepayLoanDto> repayValidator) : ILoanService
{
    public async Task<Result<LoanDto>> RequestAsync(
        RequestLoanDto dto, Guid companyId, Guid clientUserId, CancellationToken ct)
    {
        var validation = await requestValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<LoanDto>.Failure(validation.Errors.First().ErrorMessage);

        var client = await accounts.GetByOwnerUserIdAsync(companyId, clientUserId, ct);
        if (client is null || client.IsTreasury)
            return Result<LoanDto>.Failure(ErrorCode.NotFound, "Client account not found.");

        if (client.Status != AccountStatus.Active)
            return Result<LoanDto>.Failure(ErrorCode.Forbidden, "Your account is not active.");

        var loan = new Loan
        {
            CompanyId = companyId,
            ClientAccountId = client.Id,
            Amount = dto.Amount,
            Currency = client.Currency,
            Reason = dto.Reason.Trim(),
            Status = LoanStatus.Pending,
        };

        await loans.AddAsync(loan, ct);
        await audit.RecordAsync(
            companyId, clientUserId, AuditAction.Create, nameof(Loan), loan.Id,
            beforeJson: null, afterJson: AuditJson.Serialize(loan.ToAuditSnapshot()), ipAddress: null, ct);

        // Bell: the bank operator (owner of the treasury) learns that a client
        // requested a loan and must decide it. Resolved via the repository —
        // never through a lazy navigation.
        var treasury = await accounts.GetTreasuryAsync(companyId, ct);
        if (treasury is not null)
        {
            await notifications.NotifyAsync(
                companyId, treasury.OwnerUserId, NotificationType.LoanRequested,
                loan.Id, client.DisplayName, loan.Amount, ct);
        }

        return Result<LoanDto>.Success(LoanDto.FromEntity(loan));
    }

    public async Task<Result<IReadOnlyList<LoanDto>>> GetMyLoansAsync(
        Guid companyId, Guid clientUserId, CancellationToken ct)
    {
        var client = await accounts.GetByOwnerUserIdAsync(companyId, clientUserId, ct);
        if (client is null)
            return Result<IReadOnlyList<LoanDto>>.Failure(ErrorCode.NotFound, "Client account not found.");

        var list = await loans.GetByClientAsync(companyId, client.Id, ct);
        return Result<IReadOnlyList<LoanDto>>.Success(list.Select(LoanDto.FromEntity).ToList());
    }

    public async Task<Result<IReadOnlyList<LoanDto>>> GetAllAsync(
        Guid companyId, string role, LoanStatus? status, CancellationToken ct)
    {
        if (!IsAdmin(role))
            return Result<IReadOnlyList<LoanDto>>.Failure(ErrorCode.Forbidden, "Only the bank Admin can view all loans.");

        var list = await loans.GetByCompanyAsync(companyId, status, ct);
        return Result<IReadOnlyList<LoanDto>>.Success(list.Select(LoanDto.FromEntity).ToList());
    }

    public async Task<Result<LoanDto>> DecideAsync(
        Guid id, DecideLoanDto dto, Guid companyId, Guid adminUserId, string role, CancellationToken ct)
    {
        if (!IsAdmin(role))
            return Result<LoanDto>.Failure(ErrorCode.Forbidden, "Only the bank Admin can decide loans.");

        var validation = await decideValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<LoanDto>.Failure(validation.Errors.First().ErrorMessage);

        var loan = await loans.GetByIdAsync(id, companyId, ct);
        if (loan is null)
            return Result<LoanDto>.Failure(ErrorCode.NotFound, "Loan not found.");

        if (loan.Status != LoanStatus.Pending)
            return Result<LoanDto>.Failure(ErrorCode.Conflict, "This loan is already decided.");

        string beforeJson = AuditJson.Serialize(loan.ToAuditSnapshot());

        if (!dto.Approve)
        {
            loan.Status = LoanStatus.Rejected;
            loan.DecidedByUserId = adminUserId;
            loan.DecidedAt = DateTimeOffset.UtcNow;
            loan.DecisionNote = dto.Note?.Trim();

            await loans.UpdateAsync(loan, ct);
            await audit.RecordAsync(
                companyId, adminUserId, AuditAction.Update, nameof(Loan), loan.Id,
                beforeJson, AuditJson.Serialize(loan.ToAuditSnapshot()), ipAddress: null, ct);

            // Bell: tell the client their request was rejected. The owner user
            // is resolved through the repository — never via a lazy navigation.
            var loanOwner = await accounts.GetByIdAsync(loan.ClientAccountId, companyId, ct);
            if (loanOwner is not null)
            {
                await notifications.NotifyAsync(
                    companyId, loanOwner.OwnerUserId, NotificationType.LoanRejected,
                    loan.Id, null, loan.Amount, ct);
            }

            return Result<LoanDto>.Success(LoanDto.FromEntity(loan));
        }

        // Approval: treasury → client disbursement. The treasury can never lend
        // more than it holds (same no-negative rule as any account).
        var treasury = await accounts.GetTreasuryAsync(companyId, ct);
        if (treasury is null)
            return Result<LoanDto>.Failure(ErrorCode.NotFound, "The bank treasury was not found.");

        if (treasury.Balance < loan.Amount)
            return Result<LoanDto>.Failure(
                ErrorCode.Conflict, "The bank treasury does not have enough funds for this loan.");

        var disbursement = new Movement
        {
            CompanyId = companyId,
            FromAccountId = treasury.Id,
            ToAccountId = loan.ClientAccountId,
            Type = MovementType.LoanDisbursement,
            Amount = loan.Amount,
            Currency = loan.Currency,
            Description = $"Loan disbursement — {loan.Reason}",
            OccurredAt = DateTimeOffset.UtcNow,
        };

        await movements.AddAsync(disbursement, ct);

        await audit.RecordAsync(
            companyId, adminUserId, AuditAction.Create, nameof(Movement), disbursement.Id,
            beforeJson: null, afterJson: AuditJson.Serialize(disbursement.ToAuditSnapshot()),
            ipAddress: null, ct);

        loan.Status = LoanStatus.Approved;
        loan.DecidedByUserId = adminUserId;
        loan.DecidedAt = DateTimeOffset.UtcNow;
        loan.DecisionNote = dto.Note?.Trim();

        await loans.UpdateAsync(loan, ct);
        await audit.RecordAsync(
            companyId, adminUserId, AuditAction.Update, nameof(Loan), loan.Id,
            beforeJson, AuditJson.Serialize(loan.ToAuditSnapshot()), ipAddress: null, ct);

        // Bell: tell the client their loan was approved and funded.
        var approvedLoanOwner = await accounts.GetByIdAsync(loan.ClientAccountId, companyId, ct);
        if (approvedLoanOwner is not null)
        {
            await notifications.NotifyAsync(
                companyId, approvedLoanOwner.OwnerUserId, NotificationType.LoanApproved,
                loan.Id, null, loan.Amount, ct);
        }

        return Result<LoanDto>.Success(LoanDto.FromEntity(loan));
    }

    public async Task<Result<LoanDto>> RepayAsync(
        Guid id, RepayLoanDto dto, Guid companyId, Guid clientUserId, CancellationToken ct)
    {
        var validation = await repayValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<LoanDto>.Failure(validation.Errors.First().ErrorMessage);

        var client = await accounts.GetByOwnerUserIdAsync(companyId, clientUserId, ct);
        if (client is null || client.IsTreasury)
            return Result<LoanDto>.Failure(ErrorCode.NotFound, "Client account not found.");

        if (client.Status != AccountStatus.Active)
            return Result<LoanDto>.Failure(ErrorCode.Forbidden, "Your account is not active.");

        var loan = await loans.GetByIdAsync(id, companyId, ct);
        if (loan is null || loan.ClientAccountId != client.Id)
            return Result<LoanDto>.Failure(ErrorCode.NotFound, "Loan not found.");

        if (loan.Status != LoanStatus.Approved)
            return Result<LoanDto>.Failure(ErrorCode.Conflict, "Only approved loans can be repaid.");

        decimal outstanding = loan.Amount - loan.RepaidAmount;
        if (outstanding <= 0)
            return Result<LoanDto>.Failure(ErrorCode.Conflict, "This loan is already fully repaid.");

        if (dto.Amount > outstanding)
            return Result<LoanDto>.Failure(ErrorCode.Conflict, $"The outstanding amount is {outstanding:C} — repay less.");

        // The client cannot pay more than they hold (no negative balances).
        if (client.Balance < dto.Amount)
            return Result<LoanDto>.Failure(ErrorCode.Conflict, "Insufficient funds to repay this loan.");

        var treasury = await accounts.GetTreasuryAsync(companyId, ct);
        if (treasury is null)
            return Result<LoanDto>.Failure(ErrorCode.NotFound, "The bank treasury was not found.");

        string beforeJson = AuditJson.Serialize(loan.ToAuditSnapshot());

        var repayment = new Movement
        {
            CompanyId = companyId,
            FromAccountId = client.Id,
            ToAccountId = treasury.Id,
            Type = MovementType.LoanRepayment,
            Amount = dto.Amount,
            Currency = loan.Currency,
            Description = $"Loan repayment — {loan.Reason}",
            OccurredAt = DateTimeOffset.UtcNow,
        };

        await movements.AddAsync(repayment, ct);

        await audit.RecordAsync(
            companyId, clientUserId, AuditAction.Create, nameof(Movement), repayment.Id,
            beforeJson: null, afterJson: AuditJson.Serialize(repayment.ToAuditSnapshot()),
            ipAddress: null, ct);

        loan.RepaidAmount += dto.Amount;
        if (loan.RepaidAmount >= loan.Amount)
            loan.Status = LoanStatus.Repaid;

        await loans.UpdateAsync(loan, ct);
        await audit.RecordAsync(
            companyId, clientUserId, AuditAction.Update, nameof(Loan), loan.Id,
            beforeJson, AuditJson.Serialize(loan.ToAuditSnapshot()), ipAddress: null, ct);

        // Bell: the bank learns a client repaid (full or partial).
        await notifications.NotifyAsync(
            companyId, treasury.OwnerUserId, NotificationType.LoanRepaid,
            loan.Id, client.DisplayName, dto.Amount, ct);

        return Result<LoanDto>.Success(LoanDto.FromEntity(loan));
    }

    private static bool IsAdmin(string role) => role == nameof(UserRole.Admin);
}
