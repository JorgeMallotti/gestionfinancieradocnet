using GestionFinanciera.Application.Common.Results;

namespace GestionFinanciera.Application.Features.Reports.Interfaces;

/// <summary>
/// Email port. Implemented in Infrastructure with MailKit (SMTP).
/// The refresh token/auth data never travel through email.
/// </summary>
public interface IEmailService
{
    /// <summary>Sends a PDF financial report as an attachment.</summary>
    Task<Result> SendReportAsync(
        string toEmail,
        string companyName,
        DateTimeOffset? from,
        DateTimeOffset? to,
        byte[] pdfBytes,
        CancellationToken ct);
}
