using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Accounts.Interfaces;

/// <summary>
/// Data access contract for client accounts and the bank treasury. Every method
/// takes companyId explicitly — repositories never trust a global filter alone.
/// </summary>
public interface IAccountRepository
{
    Task<ClientAccount?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);

    /// <summary>The account owned by an ApplicationUser (a client — or the Admin, who owns the treasury).</summary>
    Task<ClientAccount?> GetByOwnerUserIdAsync(Guid companyId, Guid ownerUserId, CancellationToken ct);

    /// <summary>The single bank treasury account of a company.</summary>
    Task<ClientAccount?> GetTreasuryAsync(Guid companyId, CancellationToken ct);

    /// <summary>Client accounts (excluding the treasury), optionally filtered by status.</summary>
    Task<IReadOnlyList<ClientAccount>> GetClientsAsync(
        Guid companyId, AccountStatus? status, CancellationToken ct);

    /// <summary>Active accounts except the given one — used to pick transfer counterparties.</summary>
    Task<IReadOnlyList<ClientAccount>> GetActiveCounterpartiesAsync(
        Guid companyId, Guid excludeAccountId, CancellationToken ct);

    Task AddAsync(ClientAccount account, CancellationToken ct);

    Task UpdateAsync(ClientAccount account, CancellationToken ct);
}
