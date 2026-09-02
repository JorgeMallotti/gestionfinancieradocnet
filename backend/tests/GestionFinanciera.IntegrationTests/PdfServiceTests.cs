using System.Text;

using GestionFinanciera.Application.Features.Movements.DTOs;
using GestionFinanciera.Application.Features.Reports.DTOs;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.Infrastructure.Services;

namespace GestionFinanciera.IntegrationTests;

/// <summary>
/// Real QuestPDF generation tests. Run in-memory (no DB) — they validate the
/// QuestPDF API usage and produce real PDF bytes.
/// </summary>
public sealed class PdfServiceTests
{
    private static ReportDataDto SampleData(int count = 2) =>
        new(
            "Acme Demo Bank",
            "Ana García",
            Enumerable.Range(1, count)
                .Select(i => new MovementDto(
                    Guid.NewGuid(),
                    MovementType.Transfer,
                    Guid.NewGuid(),
                    i % 2 == 0 ? "XYZ Solutions SL" : "Acme Demo Bank",
                    Guid.NewGuid(),
                    "Ana García",
                    100m * i,
                    "EUR",
                    null,
                    null,
                    $"Description {i}",
                    null,
                    new DateTimeOffset(2026, 8, i, 10, 0, 0, TimeSpan.Zero)))
                .ToList(),
            200m,
            100m,
            null,
            null);

    [Fact]
    public async Task GenerateAsync_ProducesValidPdf()
    {
        var service = new PdfService();

        byte[] bytes = await service.GenerateAsync(SampleData(), CancellationToken.None);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, "PDF should not be empty");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task GenerateAsync_EmptyMovements_StillProducesValidPdf()
    {
        var service = new PdfService();

        byte[] bytes = await service.GenerateAsync(SampleData(count: 0), CancellationToken.None);

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
