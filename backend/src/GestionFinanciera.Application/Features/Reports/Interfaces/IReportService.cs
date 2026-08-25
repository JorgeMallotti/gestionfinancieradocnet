using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Reports.DTOs;

namespace GestionFinanciera.Application.Features.Reports.Interfaces;

/// <summary>
/// Report orchestration contract: fetches the report data (scoped to the
/// company from the JWT) and delegates the actual generation/sending to the
/// PDF/Excel/email ports. Every method is read-only except SendByEmailAsync.
/// </summary>
public interface IReportService
{
    Task<Result<byte[]>> GeneratePdfAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<Result<byte[]>> GenerateExcelAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<Result> SendByEmailAsync(
        EmailReportDto dto, Guid companyId, CancellationToken ct);
}
