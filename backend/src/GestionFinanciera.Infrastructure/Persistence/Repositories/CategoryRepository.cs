using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Features.Categories.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace GestionFinanciera.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of the category repository.</summary>
public sealed class CategoryRepository(ApplicationDbContext dbContext) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct) =>
        await dbContext.Categories
            .Where(c => c.Id == id && c.CompanyId == companyId)
            .SingleOrDefaultAsync(ct);

    public async Task<PagedResult<Category>> GetByCompanyAsync(
        Guid companyId, PaginationQuery query, CancellationToken ct)
    {
        IQueryable<Category> source = dbContext.Categories
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Name);

        int total = await source.CountAsync(ct);

        List<Category> items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Category>(items, total, query.Page, query.PageSize);
    }

    public async Task<IReadOnlyList<Category>> GetAllByCompanyAsync(
        Guid companyId, CancellationToken ct) =>
        await dbContext.Categories
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<bool> ExistsByNameAsync(Guid companyId, string name, CancellationToken ct) =>
        await dbContext.Categories
            .AnyAsync(c => c.CompanyId == companyId && c.Name == name, ct);

    public async Task AddAsync(Category category, CancellationToken ct)
    {
        await dbContext.Categories.AddAsync(category, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<Category> categories, CancellationToken ct)
    {
        await dbContext.Categories.AddRangeAsync(categories, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Category category, CancellationToken ct)
    {
        category.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.Categories.Update(category);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Category category, CancellationToken ct)
    {
        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(ct);
    }
}
