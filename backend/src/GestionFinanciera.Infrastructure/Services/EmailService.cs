using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Reports.Interfaces;

using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MimeKit;

namespace GestionFinanciera.Infrastructure.Services;

/// <summary>
/// Email sender built with MailKit. SMTP credentials are never committed —
/// they come from user-secrets (dev) or Azure App Settings (prod).
/// If SMTP is not configured, it fails gracefully with a clear Result.
/// </summary>
public sealed class EmailService(
    IOptions<SmtpOptions> options,
    ILogger<EmailService> logger) : IEmailService
{
    private readonly SmtpOptions _options = options.Value;

    public async Task<Result> SendReportAsync(
        string toEmail,
        string companyName,
        DateTimeOffset? from,
        DateTimeOffset? to,
        byte[] pdfBytes,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.From)
            || _options.Host.Contains("__SET_VIA_USER_SECRETS__")
            || _options.From.Contains("__SET_VIA_USER_SECRETS__"))
            return Result.Failure("SMTP is not configured. Set the Smtp settings first.");

        try
        {
            var message = BuildMessage(_options, toEmail, companyName, from, to, pdfBytes);

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                _options.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable,
                ct);

            if (!string.IsNullOrWhiteSpace(_options.UserName))
                await client.AuthenticateAsync(_options.UserName, _options.Password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Report email sent to {To}", toEmail);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send report email to {To}", toEmail);
            return Result.Failure("Failed to send the email. Check the SMTP configuration.");
        }
    }

    /// <summary>
    /// Builds the MimeMessage. Public static so unit tests can verify the
    /// envelope without an SMTP server.
    /// </summary>
    public static MimeMessage BuildMessage(
        SmtpOptions options,
        string toEmail,
        string companyName,
        DateTimeOffset? from,
        DateTimeOffset? toDate,
        byte[] pdfBytes)
    {
        string period = from.HasValue || toDate.HasValue
            ? $"{from:yyyy-MM-dd} to {toDate:yyyy-MM-dd}"
            : "all time";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            string.IsNullOrWhiteSpace(options.FromName) ? options.From : options.FromName,
            options.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"Financial Report - {companyName} ({period})";

        var body = new BodyBuilder
        {
            TextBody = $"Please find attached the financial report for {companyName} ({period}).",
            HtmlBody = $"<p>Please find attached the financial report for <strong>{companyName}</strong> ({period}).</p>",
        };
        body.Attachments.Add("financial-report.pdf", pdfBytes, ContentType.Parse("application/pdf"));

        message.Body = body.ToMessageBody();
        return message;
    }
}
