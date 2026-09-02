using System.Text;

using GestionFinanciera.Application.Features.Movements.DTOs;
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
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();
        var data = new ReportDataDto(
            "Acme Demo Bank",
            "Ana García",
            [
                new MovementDto(
                    Guid.NewGuid(), MovementType.Transfer, from, "Ana García", to, "XYZ Solutions SL",
                    1000m, "EUR", null, null, "Client payment", null,
                    new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero)),
            ],
            1000m,
            0m,
            null,
            null);

        var service = new ExcelService();

        byte[] bytes = await service.GenerateAsync(data, CancellationToken.None);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000, "xlsx should not be empty");
        Assert.Equal("PK", Encoding.ASCII.GetString(bytes, 0, 2));
    }
}
