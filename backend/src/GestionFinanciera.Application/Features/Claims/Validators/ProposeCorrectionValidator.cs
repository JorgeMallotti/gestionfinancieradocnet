using FluentValidation;

using GestionFinanciera.Application.Features.Claims.DTOs;

namespace GestionFinanciera.Application.Features.Claims.Validators;

/// <summary>Validates the Admin's proposed correction.</summary>
public sealed class ProposeCorrectionValidator : AbstractValidator<ProposeCorrectionDto>
{
    public ProposeCorrectionValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be positive.");

        RuleFor(x => x.FromAccountId)
            .NotEmpty()
            .NotEqual(Guid.Empty);

        RuleFor(x => x.ToAccountId)
            .NotEmpty()
            .NotEqual(Guid.Empty)
            .NotEqual(x => x.FromAccountId)
            .WithMessage("From and To accounts must differ.");

        RuleFor(x => x.Note)
            .MaximumLength(500);
    }
}
