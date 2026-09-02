using FluentValidation;

using GestionFinanciera.Application.Features.Auth.DTOs;

namespace GestionFinanciera.Application.Features.Auth.Validators;

/// <summary>Validates the demo login contract (whitelist of well-known keys).</summary>
public sealed class DemoLoginValidator : AbstractValidator<DemoLoginDto>
{
    public DemoLoginValidator()
    {
        RuleFor(x => x.Account)
            .NotEmpty()
            .MaximumLength(32);
    }
}
