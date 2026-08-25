using GestionFinanciera.Application.Features.Reports.DTOs;

namespace GestionFinanciera.Application.Features.Reports.Interfaces;

/// <summary>
/// Excel export port. Implemented in Infrastructure with ClosedXML.
/// </summary>
public interface IExcelService
{
    Task<byte[]> GenerateAsync(ReportDataDto data, CancellationToken ct);
}
