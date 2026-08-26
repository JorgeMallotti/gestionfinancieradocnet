using GestionFinanciera.Domain.Entities;

namespace GestionFinanciera.Application.Features.Categories.DTOs;

/// <summary>Category response — returned by the API and used to update frontend state.</summary>
public sealed record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsDefault)
{
    public static CategoryDto FromEntity(Category category) =>
        new(category.Id, category.Name, category.Description, category.IsDefault);
}
