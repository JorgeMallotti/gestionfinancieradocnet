using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Features.Movements.DTOs;
using GestionFinanciera.Domain.Entities;

namespace GestionFinanciera.Application.Features.Movements.Interfaces;

/// <summary>
/// Data access contract for the immutable ledger. Movements are append-only:
/// the repository exposes add/read only — no update, no delete (AGENTS.md §2.1).
/// </summary>
public interface IMovementRepository
{
    Task<Movement?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);

    /// <summary>Paged movements involving an account (incoming or outgoing).</summary>
    Task<PagedResult<Movement>> GetByAccountAsync(
        Guid companyId, Guid accountId, MovementQueryDto query, CancellationToken ct);

    /// <summary>Movements between two dates involving an account (used by reports).</summary>
    Task<IReadOnlyList<Movement>> GetByAccountInRangeAsync(
        Guid companyId, Guid accountId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<long> CountByCategoryAsync(Guid companyId, Guid categoryId, CancellationToken ct);

    /// <summary>
    /// Appends a movement and applies the double-entry bookkeeping (debit the
    /// From account, credit the To account) in a single transaction. Services
    /// validate the business rules (sufficient funds, active accounts) BEFORE
    /// calling this — the repository only persists the ledger row + balances.
    /// </summary>
    Task AddAsync(Movement movement, CancellationToken ct);
}
