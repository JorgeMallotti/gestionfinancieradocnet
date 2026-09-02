using FluentValidation;

using GestionFinanciera.Application.Features.Movements.DTOs;

namespace GestionFinanciera.Application.Features.Movements.Validators;

/// <summary>Validates a transfer request (whitelist semantics).</summary>
public sealed class TransferValidator : AbstractValidator<TransferDto>
{
    public TransferValidator()
    {
        RuleFor(x => x.ToAccountId)
            .NotEmpty()
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be positive.")
            .LessThanOrEqualTo(1_000_000_000m)
            .WithMessage("Amount is too large.");

        RuleFor(x => x.Description)
            .MaximumLength(200);
    }
}
