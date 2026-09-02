using FluentValidation;

using GestionFinanciera.Application.Common.Audit;
using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Categories.DTOs;
using GestionFinanciera.Application.Features.Categories.Interfaces;
using GestionFinanciera.Application.Features.Movements.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Categories;

/// <summary>
/// Category business rules. The catalog is managed by the bank Admin alone:
/// unique names, categories tagged by movements cannot be deleted, audit trail.
/// </summary>
public sealed class CategoryService(
    ICategoryRepository repository,
    IMovementRepository movements,
    IAuditService audit,
    IValidator<CreateCategoryDto> createValidator,
    IValidator<UpdateCategoryDto> updateValidator) : ICategoryService
{
    public async Task<Result<PagedResult<CategoryDto>>> GetByCompanyAsync(
        Guid companyId, PaginationQuery query, CancellationToken ct)
    {
        var result = await repository.GetByCompanyAsync(companyId, query, ct);
        var items = result.Items.Select(CategoryDto.FromEntity).ToList();
        return Result<PagedResult<CategoryDto>>.Success(
            new PagedResult<CategoryDto>(items, result.TotalCount, result.Page, result.PageSize));
    }

    public async Task<Result<IReadOnlyList<CategoryDto>>> GetAllByCompanyAsync(
        Guid companyId, CancellationToken ct)
    {
        var categories = await repository.GetAllByCompanyAsync(companyId, ct);
        return Result<IReadOnlyList<CategoryDto>>.Success(
            categories.Select(CategoryDto.FromEntity).ToList());
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(
        Guid id, Guid companyId, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(id, companyId, ct);
        if (category is null)
            return Result<CategoryDto>.Failure(ErrorCode.NotFound, "Category not found.");

        return Result<CategoryDto>.Success(CategoryDto.FromEntity(category));
    }

    public async Task<Result<CategoryDto>> CreateAsync(
        CreateCategoryDto dto, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct)
    {
        if (!CanManageCatalog(role))
            return Result<CategoryDto>.Failure(ErrorCode.Forbidden, "Only the bank Admin can manage the category catalog.");

        var validation = await createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<CategoryDto>.Failure(validation.Errors.First().ErrorMessage);

        if (await repository.ExistsByNameAsync(companyId, dto.Name.Trim(), ct))
            return Result<CategoryDto>.Failure(ErrorCode.Conflict, $"A category named '{dto.Name.Trim()}' already exists.");

        var category = new Category
        {
            CompanyId = companyId,
            Name = dto.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
        };

        await repository.AddAsync(category, ct);
        await audit.RecordAsync(
            companyId, userId, AuditAction.Create, nameof(Category), category.Id,
            beforeJson: null, afterJson: AuditJson.Serialize(category.ToAuditSnapshot()), ipAddress, ct);

        return Result<CategoryDto>.Success(CategoryDto.FromEntity(category));
    }

    public async Task<Result<CategoryDto>> UpdateAsync(
        Guid id, UpdateCategoryDto dto, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct)
    {
        if (!CanManageCatalog(role))
            return Result<CategoryDto>.Failure(ErrorCode.Forbidden, "Only the bank Admin can manage the category catalog.");

        var validation = await updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<CategoryDto>.Failure(validation.Errors.First().ErrorMessage);

        var category = await repository.GetByIdAsync(id, companyId, ct);
        if (category is null)
            return Result<CategoryDto>.Failure(ErrorCode.NotFound, "Category not found.");

        string newName = dto.Name.Trim();
        if (!string.Equals(newName, category.Name, StringComparison.OrdinalIgnoreCase)
            && await repository.ExistsByNameAsync(companyId, newName, ct))
            return Result<CategoryDto>.Failure(ErrorCode.Conflict, $"A category named '{newName}' already exists.");

        string beforeJson = AuditJson.Serialize(category.ToAuditSnapshot());

        category.Name = newName;
        category.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        await repository.UpdateAsync(category, ct);
        await audit.RecordAsync(
            companyId, userId, AuditAction.Update, nameof(Category), category.Id,
            beforeJson, AuditJson.Serialize(category.ToAuditSnapshot()), ipAddress, ct);

        return Result<CategoryDto>.Success(CategoryDto.FromEntity(category));
    }

    public async Task<Result> DeleteAsync(
        Guid id, Guid companyId, Guid userId, string role, string? ipAddress, CancellationToken ct)
    {
        if (!CanManageCatalog(role))
            return Result.Failure(ErrorCode.Forbidden, "Only the bank Admin can manage the category catalog.");

        var category = await repository.GetByIdAsync(id, companyId, ct);
        if (category is null)
            return Result.Failure(ErrorCode.NotFound, "Category not found.");

        if (await movements.CountByCategoryAsync(companyId, id, ct) > 0)
            return Result.Failure(ErrorCode.Conflict, "This category is used by movements and cannot be deleted.");

        string beforeJson = AuditJson.Serialize(category.ToAuditSnapshot());

        await repository.DeleteAsync(category, ct);
        await audit.RecordAsync(
            companyId, userId, AuditAction.Delete, nameof(Category), id,
            beforeJson, afterJson: null, ipAddress, ct);

        return Result.Success();
    }

    private static bool CanManageCatalog(string role) =>
        role == nameof(UserRole.Admin);
}
