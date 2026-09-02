using FluentValidation;

using GestionFinanciera.Application.Features.Loans.DTOs;

namespace GestionFinanciera.Application.Features.Loans.Validators;

/// <summary>Validates a loan request.</summary>
public sealed class RequestLoanValidator : AbstractValidator<RequestLoanDto>
{
    public RequestLoanValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be positive.")
            .LessThanOrEqualTo(1_000_000_000m)
            .WithMessage("Amount is too large.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}
