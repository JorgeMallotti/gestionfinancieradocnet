using FluentValidation;

using GestionFinanciera.Application.Features.Loans.DTOs;

namespace GestionFinanciera.Application.Features.Loans.Validators;

/// <summary>Validates a loan repayment (must be positive).</summary>
public sealed class RepayLoanValidator : AbstractValidator<RepayLoanDto>
{
    public RepayLoanValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be positive.")
            .LessThanOrEqualTo(1_000_000_000m)
            .WithMessage("Amount is too large.");
    }
}
