using FluentValidation;

using GestionFinanciera.Application.Features.Claims.DTOs;

namespace GestionFinanciera.Application.Features.Claims.Validators;

/// <summary>Validates opening a claim.</summary>
public sealed class OpenClaimValidator : AbstractValidator<OpenClaimDto>
{
    public OpenClaimValidator()
    {
        RuleFor(x => x.MovementId)
            .NotEmpty()
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}
