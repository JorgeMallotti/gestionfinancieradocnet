using GestionFinanciera.Application.Features.Reports.DTOs;

namespace GestionFinanciera.Application.Features.Reports.Interfaces;

/// <summary>
/// PDF generation port. Implemented in Infrastructure with QuestPDF.
/// The report data is prepared by the report service — generators stay
/// presentation-only (no EF Core, no business rules).
/// </summary>
public interface IPdfService
{
    Task<byte[]> GenerateAsync(ReportDataDto data, CancellationToken ct);
}
