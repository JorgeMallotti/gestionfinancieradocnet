using FluentValidation;

using GestionFinanciera.Application.Features.Categories.DTOs;

namespace GestionFinanciera.Application.Features.Categories.Validators;

/// <summary>Validates the create-category contract.</summary>
public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
