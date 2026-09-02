using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Reports.DTOs;

namespace GestionFinanciera.Application.Features.Reports.Interfaces;

/// <summary>
/// Report orchestration contract: builds the report data scoped to the CALLER'S
/// OWN account (any role can export their own movements) and delegates the
/// generation/sending to the PDF/Excel/email ports.
/// </summary>
public interface IReportService
{
    Task<Result<byte[]>> GeneratePdfAsync(
        Guid companyId, Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<Result<byte[]>> GenerateExcelAsync(
        Guid companyId, Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<Result> SendByEmailAsync(
        EmailReportDto dto, Guid companyId, Guid userId, CancellationToken ct);
}
