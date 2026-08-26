namespace GestionFinanciera.Application.Features.Categories.DTOs;

/// <summary>Update-category contract.</summary>
public sealed record UpdateCategoryDto(
    string Name,
    string? Description);
