using GestionFinanciera.Application.Common.Audit;
using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Accounts.DTOs;
using GestionFinanciera.Application.Features.Accounts.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Accounts;

/// <summary>
/// Account business rules: the caller only ever sees its own account; approving
/// or suspending clients is reserved for the bank Admin; every mutation is audited.
/// </summary>
public sealed class AccountService(
    IAccountRepository repository,
    IAuditService audit) : IAccountService
{
    public async Task<Result<AccountDto>> GetMyAccountAsync(
        Guid companyId, Guid userId, CancellationToken ct)
    {
        var account = await repository.GetByOwnerUserIdAsync(companyId, userId, ct);
        if (account is null)
            return Result<AccountDto>.Failure(ErrorCode.NotFound, "No account is linked to this user.");

        return Result<AccountDto>.Success(AccountDto.FromEntity(account));
    }

    public async Task<Result<IReadOnlyList<AccountRefDto>>> GetCounterpartiesAsync(
        Guid companyId, Guid userId, CancellationToken ct)
    {
        var mine = await repository.GetByOwnerUserIdAsync(companyId, userId, ct);
        if (mine is null)
            return Result<IReadOnlyList<AccountRefDto>>.Failure(ErrorCode.NotFound, "No account is linked to this user.");

        var accounts = await repository.GetActiveCounterpartiesAsync(companyId, mine.Id, ct);
        var result = accounts
            .Select(a => new AccountRefDto(a.Id, a.DisplayName, a.IsTreasury))
            .ToList();

        return Result<IReadOnlyList<AccountRefDto>>.Success(result);
    }

    public async Task<Result<IReadOnlyList<AccountDto>>> ListClientsAsync(
        Guid companyId, string role, AccountStatus? status, CancellationToken ct)
    {
        if (!IsAdmin(role))
            return Result<IReadOnlyList<AccountDto>>.Failure(ErrorCode.Forbidden, "Only the bank Admin can list clients.");

        var clients = await repository.GetClientsAsync(companyId, status, ct);
        return Result<IReadOnlyList<AccountDto>>.Success(
            clients.Select(AccountDto.FromEntity).ToList());
    }

    public async Task<Result<AccountDto>> ApproveClientAsync(
        Guid accountId, Guid companyId, Guid adminUserId, string role, string? ipAddress, CancellationToken ct)
    {
        if (!IsAdmin(role))
            return Result<AccountDto>.Failure(ErrorCode.Forbidden, "Only the bank Admin can approve clients.");

        var account = await GetClientAsync(accountId, companyId, ct);
        if (account is null)
            return Result<AccountDto>.Failure(ErrorCode.NotFound, "Client account not found.");

        if (account.Status != AccountStatus.Pending && account.Status != AccountStatus.Suspended)
            return Result<AccountDto>.Failure(ErrorCode.Conflict, "Only pending or suspended clients can be approved.");

        string beforeJson = AuditJson.Serialize(account.ToAuditSnapshot());

        account.Status = AccountStatus.Active;
        account.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.UpdateAsync(account, ct);
        await audit.RecordAsync(
            companyId, adminUserId, AuditAction.Update, nameof(ClientAccount), account.Id,
            beforeJson, AuditJson.Serialize(account.ToAuditSnapshot()), ipAddress, ct);

        return Result<AccountDto>.Success(AccountDto.FromEntity(account));
    }

    public async Task<Result<AccountDto>> SuspendClientAsync(
        Guid accountId, Guid companyId, Guid adminUserId, string role, string? ipAddress, CancellationToken ct)
    {
        if (!IsAdmin(role))
            return Result<AccountDto>.Failure(ErrorCode.Forbidden, "Only the bank Admin can suspend clients.");

        var account = await GetClientAsync(accountId, companyId, ct);
        if (account is null)
            return Result<AccountDto>.Failure(ErrorCode.NotFound, "Client account not found.");

        if (account.Status != AccountStatus.Active)
            return Result<AccountDto>.Failure(ErrorCode.Conflict, "Only active clients can be suspended.");

        string beforeJson = AuditJson.Serialize(account.ToAuditSnapshot());

        account.Status = AccountStatus.Suspended;
        account.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.UpdateAsync(account, ct);
        await audit.RecordAsync(
            companyId, adminUserId, AuditAction.Update, nameof(ClientAccount), account.Id,
            beforeJson, AuditJson.Serialize(account.ToAuditSnapshot()), ipAddress, ct);

        return Result<AccountDto>.Success(AccountDto.FromEntity(account));
    }

    // Only real client accounts (never the treasury) can be approved/suspended.
    private async Task<ClientAccount?> GetClientAsync(
        Guid accountId, Guid companyId, CancellationToken ct)
    {
        var account = await repository.GetByIdAsync(accountId, companyId, ct);
        return account is { IsTreasury: false } ? account : null;
    }

    private static bool IsAdmin(string role) => role == nameof(UserRole.Admin);
}
