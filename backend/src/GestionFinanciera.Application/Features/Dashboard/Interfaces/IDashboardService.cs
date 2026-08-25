using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Dashboard.DTOs;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Dashboard.Interfaces;

/// <summary>
/// Read-only aggregation contract for the dashboard. All queries are scoped to
/// the company from the JWT and optionally filtered by date range.
/// </summary>
public interface IDashboardService
{
    Task<Result<DashboardSummaryDto>> GetSummaryAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<Result<IReadOnlyList<CategoryBreakdownDto>>> GetBreakdownAsync(
        Guid companyId, TransactionType type, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);

    Task<Result<IReadOnlyList<MonthlyPointDto>>> GetMonthlyAsync(
        Guid companyId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct);
}
