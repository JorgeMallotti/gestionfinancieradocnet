using FluentValidation;

using GestionFinanciera.Application.Common.Audit;
using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Accounts.Interfaces;
using GestionFinanciera.Application.Features.Categories.Interfaces;
using GestionFinanciera.Application.Features.Movements.DTOs;
using GestionFinanciera.Application.Features.Movements.Interfaces;
using GestionFinanciera.Application.Features.Notifications.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Movements;

/// <summary>
/// Ledger business rules:
/// - the payer is always the caller's own account (from the JWT);
/// - nobody can go negative (overdrafts are rejected);
/// - treasury accounts only RECEIVE money here — disbursements go through loans;
/// - movements are append-only: they are created once and never edited/deleted.
/// Balances live on ClientAccount and are updated together with the movement row.
/// </summary>
public sealed class MovementService(
    IMovementRepository movements,
    IAccountRepository accounts,
    ICategoryRepository categories,
    IAuditService audit,
    INotificationService notifications,
    IValidator<TransferDto> transferValidator) : IMovementService
{
    public async Task<Result<MovementDto>> TransferAsync(
        TransferDto dto, Guid companyId, Guid fromUserId, CancellationToken ct)
    {
        var validation = await transferValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<MovementDto>.Failure(validation.Errors.First().ErrorMessage);

        var payer = await accounts.GetByOwnerUserIdAsync(companyId, fromUserId, ct);
        if (payer is null)
            return Result<MovementDto>.Failure(ErrorCode.NotFound, "Your account was not found.");

        if (payer.Status != AccountStatus.Active)
            return Result<MovementDto>.Failure(ErrorCode.Forbidden, "Your account is not active.");

        if (payer.IsTreasury)
            return Result<MovementDto>.Failure(ErrorCode.Forbidden, "The bank treasury cannot transfer directly.");

        if (dto.ToAccountId == payer.Id)
            return Result<MovementDto>.Failure(ErrorCode.Validation, "You cannot transfer to your own account.");

        var payee = await accounts.GetByIdAsync(dto.ToAccountId, companyId, ct);
        if (payee is null)
            return Result<MovementDto>.Failure(ErrorCode.NotFound, "Receiver account not found.");
        if (payee.Status != AccountStatus.Active)
            return Result<MovementDto>.Failure(ErrorCode.Conflict, "Receiver account is not active.");

        // Hard rule: an account can never go negative.
        if (payer.Balance < dto.Amount)
            return Result<MovementDto>.Failure(
                ErrorCode.Conflict, "Insufficient funds — your account cannot go negative.");

        if (dto.CategoryId is not null &&
            await categories.GetByIdAsync(dto.CategoryId.Value, companyId, ct) is null)
            return Result<MovementDto>.Failure(ErrorCode.NotFound, "Category does not exist.");

        var movement = new Movement
        {
            CompanyId = companyId,
            FromAccountId = payer.Id,
            ToAccountId = payee.Id,
            Type = MovementType.Transfer,
            Amount = dto.Amount,
            Currency = payer.Currency,
            CategoryId = dto.CategoryId,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            OccurredAt = DateTimeOffset.UtcNow,
        };

        // The repository applies the double-entry (debit payer, credit payee)
        // atomically with the new ledger row.
        await movements.AddAsync(movement, ct);

        await audit.RecordAsync(
            companyId, fromUserId, AuditAction.Create, nameof(Movement), movement.Id,
            beforeJson: null, afterJson: AuditJson.Serialize(movement.ToAuditSnapshot()),
            ipAddress: null, ct);

        // Bell: the receiver learns who sent them money.
        await notifications.NotifyAsync(
            companyId, payee.OwnerUserId, NotificationType.TransferReceived,
            movement.Id, payer.DisplayName, dto.Amount, ct);

        return Result<MovementDto>.Success(MovementDto.FromEntity(movement));
    }

    public async Task<Result<PagedResult<MovementDto>>> GetMyMovementsAsync(
        Guid companyId, Guid userId, MovementQueryDto query, CancellationToken ct)
    {
        var mine = await accounts.GetByOwnerUserIdAsync(companyId, userId, ct);
        if (mine is null)
            return Result<PagedResult<MovementDto>>.Failure(ErrorCode.NotFound, "No account is linked to this user.");

        var result = await movements.GetByAccountAsync(companyId, mine.Id, query, ct);
        var items = result.Items.Select(MovementDto.FromEntity).ToList();
        return Result<PagedResult<MovementDto>>.Success(
            new PagedResult<MovementDto>(items, result.TotalCount, result.Page, result.PageSize));
    }

    public async Task<Result<MovementDto>> GetByIdAsync(
        Guid id, Guid companyId, Guid userId, string role, CancellationToken ct)
    {
        var movement = await movements.GetByIdAsync(id, companyId, ct);
        if (movement is null)
            return Result<MovementDto>.Failure(ErrorCode.NotFound, "Movement not found.");

        bool isAdmin = role == nameof(UserRole.Admin);
        if (isAdmin)
            return Result<MovementDto>.Success(MovementDto.FromEntity(movement));

        var mine = await accounts.GetByOwnerUserIdAsync(companyId, userId, ct);
        if (mine is null)
            return Result<MovementDto>.Failure(ErrorCode.NotFound, "No account is linked to this user.");

        bool isInvolved = movement.FromAccountId == mine.Id || movement.ToAccountId == mine.Id;
        if (!isInvolved)
            return Result<MovementDto>.Failure(ErrorCode.Forbidden, "You cannot view this movement.");

        return Result<MovementDto>.Success(MovementDto.FromEntity(movement));
    }
}
