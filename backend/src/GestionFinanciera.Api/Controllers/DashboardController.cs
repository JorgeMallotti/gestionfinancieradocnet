using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Features.Dashboard.DTOs;
using GestionFinanciera.Application.Features.Dashboard.Interfaces;
using GestionFinanciera.Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// Read-only aggregation endpoints for the dashboard. Available to every role;
/// scoped to the company from the JWT and optionally filtered by date range.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    /// <summary>Totals: income, expenses, balance and transaction count.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetSummaryAsync(companyId, from, to, ct);
        return this.ToActionResult(result);
    }

    /// <summary>Amount per category for one type (Income or Expense), with percentages.</summary>
    [HttpGet("breakdown")]
    public async Task<ActionResult<IReadOnlyList<CategoryBreakdownDto>>> GetBreakdown(
        [FromQuery] TransactionType type, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetBreakdownAsync(companyId, type, from, to, ct);
        return this.ToActionResult(result);
    }

    /// <summary>Monthly income/expense/balance series for charts.</summary>
    [HttpGet("monthly")]
    public async Task<ActionResult<IReadOnlyList<MonthlyPointDto>>> GetMonthly(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetMonthlyAsync(companyId, from, to, ct);
        return this.ToActionResult(result);
    }
}
