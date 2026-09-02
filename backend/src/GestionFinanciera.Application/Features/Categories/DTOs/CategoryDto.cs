using GestionFinanciera.Domain.Entities;

namespace GestionFinanciera.Application.Features.Categories.DTOs;

/// <summary>Category response — Admin-managed catalog, visible to all clients.</summary>
public sealed record CategoryDto(
    Guid Id,
    string Name,
    string? Description)
{
    public static CategoryDto FromEntity(Category category) =>
        new(category.Id, category.Name, category.Description);
}
