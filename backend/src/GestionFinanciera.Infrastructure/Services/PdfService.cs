using GestionFinanciera.Application.Features.Reports.DTOs;
using GestionFinanciera.Application.Features.Reports.Interfaces;

using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionFinanciera.Infrastructure.Services;

/// <summary>
/// PDF generator built with QuestPDF. This project is under the Community
/// license (free while company revenue &lt; $1M USD/year — the MVP fits).
/// </summary>
public sealed class PdfService : IPdfService
{
    static PdfService()
    {
        // Community license: free for companies with less than $1M USD
        // annual revenue. Set once per process.
        Settings.License = LicenseType.Community;
    }

    public Task<byte[]> GenerateAsync(ReportDataDto data, CancellationToken ct)
    {
        // QuestPDF generation is synchronous; the token is still surfaced
        // so callers can abort between documents.
        ct.ThrowIfCancellationRequested();

        string period = data.From.HasValue || data.To.HasValue
            ? $"{FormatDate(data.From)} → {FormatDate(data.To)}"
            : "All time";

        byte[] bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken3));

                page.Header().Column(header =>
                {
                    header.Item().Text("Account Statement")
                        .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    header.Item().Text(data.CompanyName).FontSize(13);
                    header.Item().Text($"Account: {data.AccountDisplayName}").FontSize(11);
                    header.Item().Text($"Period: {period}").FontSize(10).FontColor(Colors.Grey.Darken2);
                    header.Item().PaddingBottom(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Summary").FontSize(14).Bold();
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Total incoming").FontColor(Colors.Grey.Darken2);
                        row.RelativeItem().AlignRight().Text(data.TotalIncoming.ToString("N2")).Bold();
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Total outgoing").FontColor(Colors.Grey.Darken2);
                        row.RelativeItem().AlignRight().Text(data.TotalOutgoing.ToString("N2")).Bold();
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Balance").FontColor(Colors.Grey.Darken2);
                        row.RelativeItem().AlignRight().Text(data.Balance.ToString("N2"))
                            .Bold().FontColor(data.Balance >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                    });

                    column.Item().PaddingTop(4).Text("Movements").FontSize(14).Bold();

                    if (data.Movements.Count == 0)
                    {
                        column.Item().Text("No movements in the selected period.")
                            .FontColor(Colors.Grey.Medium).Italic();
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.1f); // Date
                                columns.RelativeColumn(1.0f); // Type
                                columns.RelativeColumn(1.5f); // From
                                columns.RelativeColumn(1.5f); // To
                                columns.RelativeColumn(1.0f); // Amount
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Date").Bold();
                                header.Cell().Text("Type").Bold();
                                header.Cell().Text("From").Bold();
                                header.Cell().Text("To").Bold();
                                header.Cell().AlignRight().Text("Amount").Bold();
                            });

                            foreach (var m in data.Movements)
                            {
                                table.Cell().Text(m.OccurredAt.ToString("yyyy-MM-dd"));
                                table.Cell().Text(m.Type.ToString());
                                table.Cell().Text(m.FromDisplayName);
                                table.Cell().Text(m.ToDisplayName);
                                table.Cell().AlignRight().Text(m.Amount.ToString("N2"));
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken2));
                    text.Span($"Generated {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC • Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    private static string FormatDate(DateTimeOffset? date) =>
        date?.ToString("yyyy-MM-dd") ?? "…";
}
