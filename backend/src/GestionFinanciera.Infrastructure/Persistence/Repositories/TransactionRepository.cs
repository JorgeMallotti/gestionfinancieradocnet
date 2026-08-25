using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Features.Dashboard.DTOs;
using GestionFinanciera.Application.Features.Transactions.DTOs;
using GestionFinanciera.Application.Features.Transactions.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace GestionFinanciera.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the transaction repository, including the dashboard
/// aggregations (sums, breakdowns, monthly series) translated to SQL.
/// </summary>
public sealed class TransactionRepository(ApplicationDbContext dbContext) : ITransactionRepository
{
    public async Task<Transaction?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct) =>
        await dbContext.Transactions
            .Include(t => t.Category)
            .Where(t => t.Id == id && t.CompanyId == companyId)
            .SingleOrDefaultAsync(ct);

    public async Task<PagedResult<Transaction>> GetByCompanyAsync(
        Guid companyId, TransactionQueryDto query, CancellationToken ct)
    {
        IQueryable<Transaction> source = dbContext.Transactions
            .Include(t => t.Category)
            .Where(t => t.CompanyId == companyId);

        if (query.Type.HasValue)
            source = source.Where(t => t.Type == query.Type.Value);

        if (query.CategoryId.HasValue)
            source = source.Where(t => t.CategoryId == query.CategoryId.Value);

        if (query.From.HasValue)
            source = source.Where(t => t.Date >= query.From.Value);

        if (query.To.HasValue)
            source = source.Where(t => t.Date <= query.To.Value);

        int total = await source.CountAsync(ct);

        List<Transaction> items = await source
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Transaction>(items, total, query.Page, query.PageSize);
    }

    public async Task<int> CountByCategoryAsync(Guid companyId, Guid categoryId, CancellationToken ct) =>
        await dbContext.Transactions
            .CountAsync(t => t.CompanyId == companyId && t.CategoryId == categoryId, ct);

    public async Task AddAsync(Transaction transaction, CancellationToken ct)
    {
        await dbContext.Transactions.AddAsync(transaction, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken ct)
    {
        transaction.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.Transactions.Update(transaction);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Transaction transaction, CancellationToken ct)
    {
        dbContext.Transactions.Remove(transaction);
        await dbContext.SaveChangesAsync(ct);
    }

    // ── Dashboard aggregations ───────────────────────────────────────────

    public async Task<decimal> SumByTypeAsync(
        Guid companyId, TransactionType type, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        IQueryable<Transaction> source = dbContext.Transactions
            .Where(t => t.CompanyId == companyId && t.Type == type);

        if (from.HasValue)
            source = source.Where(t => t.Date >= from.Value);

        if (to.HasValue)
            source = source.Where(t => t.Date <= to.Value);

        return await source.SumAsync(t => t.Amount, ct);
    }

    public async Task<int> CountAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        IQueryable<Transaction> source = dbContext.Transactions
            .Where(t => t.CompanyId == companyId);

        if (from.HasValue)
            source = source.Where(t => t.Date >= from.Value);

        if (to.HasValue)
            source = source.Where(t => t.Date <= to.Value);

        return await source.CountAsync(ct);
    }

    public async Task<IReadOnlyList<CategoryBreakdownDto>> GetCategoryBreakdownAsync(
        Guid companyId, TransactionType type, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        IQueryable<Transaction> source = dbContext.Transactions
            .Where(t => t.CompanyId == companyId && t.Type == type);

        if (from.HasValue)
            source = source.Where(t => t.Date >= from.Value);

        if (to.HasValue)
            source = source.Where(t => t.Date <= to.Value);

        // Order BEFORE projecting to the DTO: EF Core cannot translate
        // OrderBy over a DTO property in a GroupBy shape.
        return await source
            .GroupBy(t => new { t.CategoryId, t.Category.Name })
            .Select(g => new { g.Key.CategoryId, g.Key.Name, Amount = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Amount)
            .Select(x => new CategoryBreakdownDto(x.CategoryId, x.Name, type, x.Amount, 0))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MonthlyPointDto>> GetMonthlySeriesAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        IQueryable<Transaction> source = dbContext.Transactions
            .Where(t => t.CompanyId == companyId);

        if (from.HasValue)
            source = source.Where(t => t.Date >= from.Value);

        if (to.HasValue)
            source = source.Where(t => t.Date <= to.Value);

        // Group by (year, month, type) in SQL (fully translatable), then pivot
        // to income/expense in memory. Conditional sums inside GroupBy+Select
        // are not reliably translated by EF Core 10.
        var rows = await source
            .GroupBy(t => new { t.Date.Year, t.Date.Month, t.Type })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Type, Amount = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => (r.Year, r.Month))
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyPointDto(
                g.Key.Year,
                g.Key.Month,
                g.Where(r => r.Type == TransactionType.Income).Sum(r => r.Amount),
                g.Where(r => r.Type == TransactionType.Expense).Sum(r => r.Amount),
                0)) // Balance computed in the service layer.
            .ToList();
    }
}
