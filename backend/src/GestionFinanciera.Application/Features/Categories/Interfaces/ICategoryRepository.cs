using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Domain.Entities;

namespace GestionFinanciera.Application.Features.Categories.Interfaces;

/// <summary>
/// Data access contract for categories. Implemented in Infrastructure with EF Core.
/// Every method receives companyId explicitly — repositories never trust a global filter alone.
/// </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);

    Task<PagedResult<Category>> GetByCompanyAsync(
        Guid companyId, PaginationQuery query, CancellationToken ct);

    Task<IReadOnlyList<Category>> GetAllByCompanyAsync(Guid companyId, CancellationToken ct);

    Task<bool> ExistsByNameAsync(Guid companyId, string name, CancellationToken ct);

    Task AddAsync(Category category, CancellationToken ct);

    Task AddRangeAsync(IEnumerable<Category> categories, CancellationToken ct);

    Task UpdateAsync(Category category, CancellationToken ct);

    Task DeleteAsync(Category category, CancellationToken ct);
}
