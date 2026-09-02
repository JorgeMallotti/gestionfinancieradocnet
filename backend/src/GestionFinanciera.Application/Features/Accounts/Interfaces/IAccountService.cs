using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Accounts.DTOs;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Accounts.Interfaces;

/// <summary>
/// Account business logic contract. The controller resolves companyId/userId/role
/// from the JWT and passes them here. Every mutation returns the full resource.
/// </summary>
public interface IAccountService
{
    /// <summary>The caller's own account (clients see theirs; the Admin owns the treasury).</summary>
    Task<Result<AccountDto>> GetMyAccountAsync(
        Guid companyId, Guid userId, CancellationToken ct);

    /// <summary>Active accounts the caller may send money to (never itself).</summary>
    Task<Result<IReadOnlyList<AccountRefDto>>> GetCounterpartiesAsync(
        Guid companyId, Guid userId, CancellationToken ct);

    /// <summary>Admin: list client accounts, optionally filtered by status.</summary>
    Task<Result<IReadOnlyList<AccountDto>>> ListClientsAsync(
        Guid companyId, string role, AccountStatus? status, CancellationToken ct);

    /// <summary>Admin: approve a pending client so it can operate.</summary>
    Task<Result<AccountDto>> ApproveClientAsync(
        Guid accountId, Guid companyId, Guid adminUserId, string role, string? ipAddress, CancellationToken ct);

    /// <summary>Admin: suspend an active client (no operations until re-approved).</summary>
    Task<Result<AccountDto>> SuspendClientAsync(
        Guid accountId, Guid companyId, Guid adminUserId, string role, string? ipAddress, CancellationToken ct);
}
