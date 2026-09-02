using GestionFinanciera.Infrastructure.Services;

using MimeKit;

namespace GestionFinanciera.IntegrationTests;

/// <summary>
/// EmailService envelope tests — BuildMessage is exercised without an SMTP
/// server. The actual send path is validated in the smoke test / dev SMTP.
/// </summary>
public sealed class EmailServiceTests
{
    private static SmtpOptions Options() => new()
    {
        Host = "smtp.test.local",
        Port = 587,
        From = "reports@acme.local",
        FromName = "GestionFinanciera",
    };

    [Fact]
    public void BuildMessage_SetsEnvelopeAndAttachment()
    {
        byte[] pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x74, 0x65, 0x73, 0x74];

        MimeMessage message = EmailService.BuildMessage(
            Options(), "jorge@test.local", "Acme S.L.",
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero),
            pdf);

        Assert.Equal("reports@acme.local", message.From.Mailboxes.Single().Address);
        Assert.Equal("jorge@test.local", message.To.Mailboxes.Single().Address);
        Assert.Contains("Acme S.L.", message.Subject);
        Assert.Contains("2026-08-01", message.Subject);

        var attachment = Assert.Single(message.Attachments.OfType<MimePart>());
        Assert.Equal("financial-report.pdf", attachment.FileName);
        Assert.Equal("application/pdf", attachment.ContentType.MimeType);

        // With an attachment the body is mixed → alternative (text+html) → text.
        var mixed = Assert.IsType<Multipart>(message.Body);
        var alternative = Assert.IsType<MultipartAlternative>(mixed[0]);
        var textPart = Assert.IsType<TextPart>(alternative[0]);
        Assert.Contains("Acme S.L.", textPart.Text);
    }

    [Fact]
    public void BuildMessage_NoRange_UsesAllTime()
    {
        MimeMessage message = EmailService.BuildMessage(
            Options(), "jorge@test.local", "Acme S.L.", null, null, [0x25, 0x50, 0x44, 0x46]);

        Assert.Contains("all time", message.Subject);
    }
}
