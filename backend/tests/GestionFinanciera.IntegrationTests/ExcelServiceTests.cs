using System.Text;

using GestionFinanciera.Application.Features.Reports.DTOs;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.Infrastructure.Services;

namespace GestionFinanciera.IntegrationTests;

/// <summary>
/// Real ClosedXML export tests — verify the workbook is a valid xlsx
/// (a ZIP archive, hence the "PK" magic bytes).
/// </summary>
public sealed class ExcelServiceTests
{
    [Fact]
    public async Task GenerateAsync_ProducesValidXlsx()
    {
        var data = new ReportDataDto(
            "Acme S.L.",
            [
                new GestionFinanciera.Application.Features.Transactions.DTOs.TransactionDto(
                    Guid.NewGuid(), Guid.NewGuid(), "Sales", TransactionType.Income,
                    1000m, "EUR", new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
                    "Client payment", Guid.NewGuid(), DateTimeOffset.UtcNow),
            ],
            TotalIncome: 1000m,
            TotalExpenses: 0m,
            From: null,
            To: null);

        var service = new ExcelService();

        byte[] bytes = await service.GenerateAsync(data, CancellationToken.None);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, "xlsx should not be empty");
        Assert.Equal("PK", Encoding.ASCII.GetString(bytes, 0, 2));
    }
}
