using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Features.Categories.Interfaces;
using GestionFinanciera.Domain.Entities;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>In-memory category repository for unit tests (no database, no Moq).</summary>
internal sealed class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly List<Category> _items = [];

    public List<Category> Items => _items;

    public Task<Category?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct) =>
        Task.FromResult(_items.FirstOrDefault(c => c.Id == id && c.CompanyId == companyId));

    public Task<PagedResult<Category>> GetByCompanyAsync(
        Guid companyId, PaginationQuery query, CancellationToken ct)
    {
        var filtered = _items
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Name)
            .ToList();

        var page = filtered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return Task.FromResult(new PagedResult<Category>(page, filtered.Count, query.Page, query.PageSize));
    }

    public Task<IReadOnlyList<Category>> GetAllByCompanyAsync(Guid companyId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Category>>(
            _items.Where(c => c.CompanyId == companyId).OrderBy(c => c.Name).ToList());

    public Task<bool> ExistsByNameAsync(Guid companyId, string name, CancellationToken ct) =>
        Task.FromResult(_items.Any(c =>
            c.CompanyId == companyId && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(Category category, CancellationToken ct)
    {
        _items.Add(category);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<Category> categories, CancellationToken ct)
    {
        _items.AddRange(categories);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Category category, CancellationToken ct)
    {
        category.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Category category, CancellationToken ct)
    {
        _items.Remove(category);
        return Task.CompletedTask;
    }
}
