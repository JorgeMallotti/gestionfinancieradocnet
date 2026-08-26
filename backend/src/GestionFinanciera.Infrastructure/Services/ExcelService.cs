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
        var sheet = workbook.AddWorksheet("Transactions");

        // Header row
        string[] headers = ["Date", "Category", "Type", "Amount", "Currency", "Description"];
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
        foreach (var t in data.Transactions)
        {
            sheet.Cell(row, 1).Value = t.Date.ToString("yyyy-MM-dd");
            sheet.Cell(row, 2).Value = t.CategoryName;
            sheet.Cell(row, 3).Value = t.Type.ToString();
            sheet.Cell(row, 4).Value = t.Amount;
            sheet.Cell(row, 5).Value = t.Currency;
            sheet.Cell(row, 6).Value = t.Description ?? string.Empty;
            row++;
        }

        // Summary block (two blank rows after the data)
        row += 2;
        sheet.Cell(row, 1).Value = "Summary";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row + 1, 1).Value = "Total income";
        sheet.Cell(row + 1, 2).Value = data.TotalIncome;
        sheet.Cell(row + 2, 1).Value = "Total expenses";
        sheet.Cell(row + 2, 2).Value = data.TotalExpenses;
        sheet.Cell(row + 3, 1).Value = "Balance";
        sheet.Cell(row + 3, 2).Value = data.Balance;
        sheet.Cell(row + 3, 2).Style.Font.Bold = true;

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }
}
