using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Categories.DTOs;

namespace GestionFinanciera.Application.Features.Categories.Interfaces;

/// <summary>
/// Category business logic contract. The controller resolves companyId/userId/role
/// from the JWT and passes them here — services never see HttpContext.
/// Every mutation returns the full resource so the frontend can update state
/// without refetching (optimistic updates, AGENTS.md §12).
/// </summary>
public interface ICategoryService
{
    Task<Result<PagedResult<CategoryDto>>> GetByCompanyAsync(
        Guid companyId, PaginationQuery query, CancellationToken ct);

    Task<Result<IReadOnlyList<CategoryDto>>> GetAllByCompanyAsync(
        Guid companyId, CancellationToken ct);

    Task<Result<CategoryDto>> GetByIdAsync(
        Guid id, Guid companyId, CancellationToken ct);

    Task<Result<CategoryDto>> CreateAsync(
        CreateCategoryDto dto, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct);

    Task<Result<CategoryDto>> UpdateAsync(
        Guid id, UpdateCategoryDto dto, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct);

    Task<Result> DeleteAsync(
        Guid id, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct);
}
