using GestionFinanciera.Application.Features.Reports.DTOs;
using GestionFinanciera.Application.Features.Reports.Interfaces;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>Captures the report data passed to the Excel exporter.</summary>
internal sealed class FakeExcelService : IExcelService
{
    public ReportDataDto? LastData { get; private set; }

    public byte[] Output { get; set; } = "PK-test"u8.ToArray();

    public Task<byte[]> GenerateAsync(ReportDataDto data, CancellationToken ct)
    {
        LastData = data;
        return Task.FromResult(Output);
    }
}
