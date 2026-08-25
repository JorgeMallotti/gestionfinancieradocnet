using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Reports.Interfaces;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>Records email send attempts without touching an SMTP server.</summary>
internal sealed class FakeEmailService : IEmailService
{
    public List<(string ToEmail, string CompanyName, byte[] PdfBytes)> Sent { get; } = [];

    public bool ShouldFail { get; set; }

    public Task<Result> SendReportAsync(
        string toEmail, string companyName, DateTimeOffset? from, DateTimeOffset? to,
        byte[] pdfBytes, CancellationToken ct)
    {
        if (ShouldFail)
            return Task.FromResult(Result.Failure("SMTP is not configured. Set the Smtp settings first."));

        Sent.Add((toEmail, companyName, pdfBytes));
        return Task.FromResult(Result.Success());
    }
}
