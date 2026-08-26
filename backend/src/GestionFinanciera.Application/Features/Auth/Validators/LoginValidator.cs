using FluentValidation;

using GestionFinanciera.Application.Features.Auth.DTOs;

namespace GestionFinanciera.Application.Features.Auth.Validators;

/// <summary>Validates the login contract.</summary>
public sealed class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}
