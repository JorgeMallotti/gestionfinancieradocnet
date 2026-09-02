using ClosedXML.Excel;

using GestionFinanciera.Application.Features.Reports.DTOs;
using GestionFinanciera.Application.Features.Reports.Interfaces;

namespace GestionFinanciera.Infrastructure.Services;

/// <summary>
/// Excel exporter built with ClosedXML (MIT license). Produces a workbook
/// with one "Transactions" sheet plus a small summary block.
/// </summary>
public sealed class ExcelService : IExcelService
{
    public Task<byte[]> GenerateAsync(ReportDataDto data, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Movements");

        // Header row
        string[] headers = ["Date", "Type", "From", "To", "Amount", "Currency", "Description"];
        for (int c = 0; c < headers.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = headers[c];
        }

        var headerRow = sheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;
        sheet.SheetView.FreezeRows(1);

        // Data rows
        int row = 2;
        foreach (var m in data.Movements)
        {
            sheet.Cell(row, 1).Value = m.OccurredAt.ToString("yyyy-MM-dd");
            sheet.Cell(row, 2).Value = m.Type.ToString();
            sheet.Cell(row, 3).Value = m.FromDisplayName;
            sheet.Cell(row, 4).Value = m.ToDisplayName;
            sheet.Cell(row, 5).Value = m.Amount;
            sheet.Cell(row, 6).Value = m.Currency;
            sheet.Cell(row, 7).Value = m.Description ?? string.Empty;
            row++;
        }

        // Summary block (two blank rows after the data)
        row += 2;
        sheet.Cell(row, 1).Value = "Summary";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row + 1, 1).Value = "Account";
        sheet.Cell(row + 1, 2).Value = data.AccountDisplayName;
        sheet.Cell(row + 2, 1).Value = "Total incoming";
        sheet.Cell(row + 2, 2).Value = data.TotalIncoming;
        sheet.Cell(row + 3, 1).Value = "Total outgoing";
        sheet.Cell(row + 3, 2).Value = data.TotalOutgoing;
        sheet.Cell(row + 4, 1).Value = "Balance";
        sheet.Cell(row + 4, 2).Value = data.Balance;
        sheet.Cell(row + 4, 2).Style.Font.Bold = true;

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }
}
