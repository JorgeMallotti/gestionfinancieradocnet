using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Dashboard.DTOs;

namespace GestionFinanciera.Application.Features.Dashboard.Interfaces;

/// <summary>
/// Read-only aggregation contract for the dashboard. Scoped to the caller's own
/// account (resolved from the JWT user), optionally filtered by date range.
/// </summary>
public interface IDashboardService
{
    Task<Result<DashboardSummaryDto>> GetMySummaryAsync(
        Guid companyId, Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<Result<IReadOnlyList<MonthlyPointDto>>> GetMyMonthlyAsync(
        Guid companyId, Guid userId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
}
