using FluentValidation;

using GestionFinanciera.Application.Features.Loans.DTOs;

namespace GestionFinanciera.Application.Features.Loans.Validators;

/// <summary>Validates the Admin's decision on a loan.</summary>
public sealed class DecideLoanValidator : AbstractValidator<DecideLoanDto>
{
    public DecideLoanValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(500);
    }
}
