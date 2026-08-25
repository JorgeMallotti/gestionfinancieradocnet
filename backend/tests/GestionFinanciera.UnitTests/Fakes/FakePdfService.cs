using GestionFinanciera.Application.Features.Reports.DTOs;
using GestionFinanciera.Application.Features.Reports.Interfaces;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>Captures the report data passed to the PDF generator.</summary>
internal sealed class FakePdfService : IPdfService
{
    public ReportDataDto? LastData { get; private set; }

    public byte[] Output { get; set; } = "%PDF-test"u8.ToArray();

    public Task<byte[]> GenerateAsync(ReportDataDto data, CancellationToken ct)
    {
        LastData = data;
        return Task.FromResult(Output);
    }
}
