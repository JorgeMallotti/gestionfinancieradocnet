using FluentValidation;

using GestionFinanciera.Application.Features.Categories.DTOs;

namespace GestionFinanciera.Application.Features.Categories.Validators;

/// <summary>Validates the update-category contract.</summary>
public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
