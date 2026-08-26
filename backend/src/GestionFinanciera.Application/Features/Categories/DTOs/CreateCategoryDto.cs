namespace GestionFinanciera.Application.Features.Categories.DTOs;

/// <summary>Create-category contract (whitelist semantics: only these fields are accepted).</summary>
public sealed record CreateCategoryDto(
    string Name,
    string? Description);
