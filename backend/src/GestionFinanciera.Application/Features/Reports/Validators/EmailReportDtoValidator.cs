using FluentValidation;

using GestionFinanciera.Application.Features.Reports.DTOs;

namespace GestionFinanciera.Application.Features.Reports.Validators;

/// <summary>Validates the send-report-by-email contract.</summary>
public sealed class EmailReportDtoValidator : AbstractValidator<EmailReportDto>
{
    public EmailReportDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x)
            .Must(x => !x.From.HasValue || !x.To.HasValue || x.From.Value <= x.To.Value)
            .WithMessage("'From' cannot be after 'To'.")
            .WithName(nameof(EmailReportDto.From));
    }
}
