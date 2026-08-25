using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Features.Dashboard.DTOs;
using GestionFinanciera.Application.Features.Transactions.DTOs;
using GestionFinanciera.Application.Features.Transactions.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>
/// In-memory transaction repository for unit tests. Mirrors the EF Core behaviour
/// including the dashboard aggregations. Category names are resolved from the
/// <see cref="CategoryNames"/> dictionary (the tests populate it).
/// </summary>
internal sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly List<Transaction> _items = [];

    public List<Transaction> Items => _items;

    /// <summary>CategoryId → name, used to hydrate the Category navigation for DTOs.</summary>
    public Dictionary<Guid, string> CategoryNames { get; } = [];

    public Task<Transaction?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct)
    {
        var transaction = _items.FirstOrDefault(t => t.Id == id && t.CompanyId == companyId);
        if (transaction is not null && transaction.Category is null
            && CategoryNames.TryGetValue(transaction.CategoryId, out string? name))
        {
            transaction.Category = new Category { Id = transaction.CategoryId, Name = name };
        }

        return Task.FromResult(transaction);
    }

    public Task<PagedResult<Transaction>> GetByCompanyAsync(
        Guid companyId, TransactionQueryDto query, CancellationToken ct)
    {
        IEnumerable<Transaction> source = _items.Where(t => t.CompanyId == companyId);

        if (query.Type.HasValue)
            source = source.Where(t => t.Type == query.Type.Value);

        if (query.CategoryId.HasValue)
            source = source.Where(t => t.CategoryId == query.CategoryId.Value);

        if (query.From.HasValue)
            source = source.Where(t => t.Date >= query.From.Value);

        if (query.To.HasValue)
            source = source.Where(t => t.Date <= query.To.Value);

        var ordered = source.OrderByDescending(t => t.Date).ToList();
        var page = ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();

        return Task.FromResult(new PagedResult<Transaction>(page, ordered.Count, query.Page, query.PageSize));
    }

    public Task<int> CountByCategoryAsync(Guid companyId, Guid categoryId, CancellationToken ct) =>
        Task.FromResult(_items.Count(t => t.CompanyId == companyId && t.CategoryId == categoryId));

    public Task AddAsync(Transaction transaction, CancellationToken ct)
    {
        _items.Add(transaction);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Transaction transaction, CancellationToken ct)
    {
        transaction.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Transaction transaction, CancellationToken ct)
    {
        _items.Remove(transaction);
        return Task.CompletedTask;
    }

    // ── Dashboard aggregations ───────────────────────────────────────────

    public Task<decimal> SumByTypeAsync(
        Guid companyId, TransactionType type, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) =>
        Task.FromResult(_items
            .Where(t => t.CompanyId == companyId && t.Type == type && InRange(t, from, to))
            .Sum(t => t.Amount));

    public Task<int> CountAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) =>
        Task.FromResult(_items.Count(t => t.CompanyId == companyId && InRange(t, from, to)));

    public Task<IReadOnlyList<CategoryBreakdownDto>> GetCategoryBreakdownAsync(
        Guid companyId, TransactionType type, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var rows = _items
            .Where(t => t.CompanyId == companyId && t.Type == type && InRange(t, from, to))
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryBreakdownDto(
                g.Key,
                CategoryNames.GetValueOrDefault(g.Key, "Unknown"),
                type,
                g.Sum(t => t.Amount),
                0))
            .OrderByDescending(d => d.Amount)
            .ToList();

        return Task.FromResult<IReadOnlyList<CategoryBreakdownDto>>(rows);
    }

    public Task<IReadOnlyList<MonthlyPointDto>> GetMonthlySeriesAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var rows = _items
            .Where(t => t.CompanyId == companyId && InRange(t, from, to))
            .GroupBy(t => (t.Date.Year, t.Date.Month))
            .Select(g => new MonthlyPointDto(
                g.Key.Year,
                g.Key.Month,
                g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                0))
            .OrderBy(p => p.Year)
            .ThenBy(p => p.Month)
            .ToList();

        return Task.FromResult<IReadOnlyList<MonthlyPointDto>>(rows);
    }

    private static bool InRange(Transaction t, DateTimeOffset? from, DateTimeOffset? to) =>
        (!from.HasValue || t.Date >= from.Value) && (!to.HasValue || t.Date <= to.Value);
}
