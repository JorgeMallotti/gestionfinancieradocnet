using System.Text;

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
            "Acme S.L.",
            Enumerable.Range(1, count)
                .Select(i => new GestionFinanciera.Application.Features.Transactions.DTOs.TransactionDto(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    i % 2 == 0 ? "Sales" : "Travel",
                    i % 2 == 0 ? TransactionType.Income : TransactionType.Expense,
                    100m * i,
                    "EUR",
                    new DateTimeOffset(2026, 8, i, 10, 0, 0, TimeSpan.Zero),
                    $"Description {i}",
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow))
                .ToList(),
            TotalIncome: 200m,
            TotalExpenses: 100m,
            From: null,
            To: null);

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
    public async Task GenerateAsync_EmptyTransactions_StillProducesValidPdf()
    {
        var service = new PdfService();

        byte[] bytes = await service.GenerateAsync(SampleData(count: 0), CancellationToken.None);

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
