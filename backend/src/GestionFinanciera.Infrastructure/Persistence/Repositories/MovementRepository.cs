using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Features.Movements.DTOs;
using GestionFinanciera.Application.Features.Movements.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace GestionFinanciera.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the immutable ledger. Movements are append-only:
/// only Add + reads are exposed. AddAsync applies the double-entry balances
/// (debit From, credit To) and the new ledger row inside ONE database
/// transaction, so a crash can never leave the books unbalanced.
/// </summary>
public sealed class MovementRepository(ApplicationDbContext dbContext) : IMovementRepository
{
    public async Task<Movement?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct) =>
        await dbContext.Movements
            .Include(m => m.FromAccount)
            .Include(m => m.ToAccount)
            .Include(m => m.Category)
            .Where(m => m.Id == id && m.CompanyId == companyId)
            .SingleOrDefaultAsync(ct);

    public async Task<PagedResult<Movement>> GetByAccountAsync(
        Guid companyId, Guid accountId, MovementQueryDto query, CancellationToken ct)
    {
        IQueryable<Movement> source = dbContext.Movements
            .Include(m => m.FromAccount)
            .Include(m => m.ToAccount)
            .Include(m => m.Category)
            .Where(m => m.CompanyId == companyId
                && (m.FromAccountId == accountId || m.ToAccountId == accountId));

        if (query.Type.HasValue)
            source = source.Where(m => m.Type == query.Type.Value);

        if (query.From.HasValue)
            source = source.Where(m => m.OccurredAt >= query.From.Value);

        if (query.To.HasValue)
            source = source.Where(m => m.OccurredAt <= query.To.Value);

        int total = await source.CountAsync(ct);

        List<Movement> items = await source
            .OrderByDescending(m => m.OccurredAt)
            .ThenByDescending(m => m.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Movement>(items, total, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<Movement>> GetByAccountInRangeAsync(
        Guid companyId, Guid accountId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        IQueryable<Movement> source = dbContext.Movements
            .Include(m => m.FromAccount)
            .Include(m => m.ToAccount)
            .Include(m => m.Category)
            .Where(m => m.CompanyId == companyId
                && (m.FromAccountId == accountId || m.ToAccountId == accountId));

        if (from.HasValue)
            source = source.Where(m => m.OccurredAt >= from.Value);

        if (to.HasValue)
            source = source.Where(m => m.OccurredAt <= to.Value);

        return await source
            .OrderBy(m => m.OccurredAt)
            .ToListAsync(ct);
    }

    public async Task<long> CountByCategoryAsync(Guid companyId, Guid categoryId, CancellationToken ct) =>
        await dbContext.Movements
            .LongCountAsync(m => m.CompanyId == companyId && m.CategoryId == categoryId, ct);

    public async Task AddAsync(Movement movement, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        // Debit the payer (balance can never go negative — services validate first).
        var from = await dbContext.ClientAccounts
            .SingleAsync(a => a.Id == movement.FromAccountId, ct);
        from.Balance -= movement.Amount;
        from.UpdatedAt = DateTimeOffset.UtcNow;

        // Credit the payee.
        var to = await dbContext.ClientAccounts
            .SingleAsync(a => a.Id == movement.ToAccountId, ct);
        to.Balance += movement.Amount;
        to.UpdatedAt = DateTimeOffset.UtcNow;

        // Append the immutable ledger row.
        await dbContext.Movements.AddAsync(movement, ct);
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
