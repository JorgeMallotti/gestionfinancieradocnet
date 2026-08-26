using FluentValidation;

using GestionFinanciera.Application.Features.Transactions.DTOs;

namespace GestionFinanciera.Application.Features.Transactions.Validators;

/// <summary>Validates the create-transaction contract.</summary>
public sealed class CreateTransactionValidator : AbstractValidator<CreateTransactionDto>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a 3-letter ISO code (e.g. EUR).");

        RuleFor(x => x.Date)
            .NotEmpty();

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
