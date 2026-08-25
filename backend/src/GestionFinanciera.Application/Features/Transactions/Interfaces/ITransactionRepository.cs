using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Features.Dashboard.DTOs;
using GestionFinanciera.Application.Features.Transactions.DTOs;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Transactions.Interfaces;

/// <summary>
/// Data access contract for transactions. Implemented in Infrastructure with EF Core.
/// Every method receives companyId explicitly — repositories never trust a global
/// filter alone. Dashboard aggregations also live here (they query transactions).
/// </summary>
public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);

    Task<PagedResult<Transaction>> GetByCompanyAsync(
        Guid companyId, TransactionQueryDto query, CancellationToken ct);

    Task<int> CountByCategoryAsync(Guid companyId, Guid categoryId, CancellationToken ct);

    Task AddAsync(Transaction transaction, CancellationToken ct);

    Task UpdateAsync(Transaction transaction, CancellationToken ct);

    Task DeleteAsync(Transaction transaction, CancellationToken ct);

    // ── Dashboard aggregations ───────────────────────────────────────────

    Task<decimal> SumByTypeAsync(
        Guid companyId, TransactionType type, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<int> CountAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<IReadOnlyList<CategoryBreakdownDto>> GetCategoryBreakdownAsync(
        Guid companyId, TransactionType type, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<IReadOnlyList<MonthlyPointDto>> GetMonthlySeriesAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
}
