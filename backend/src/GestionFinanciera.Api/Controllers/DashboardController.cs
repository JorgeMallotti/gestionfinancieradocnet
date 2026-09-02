using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Features.Dashboard.DTOs;
using GestionFinanciera.Application.Features.Dashboard.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// Read-only aggregations for the caller's OWN account (balance, incoming/
/// outgoing totals, monthly series). Every role sees their own dashboard.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    /// <summary>Balance + incoming/outgoing totals + movement count of my account.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetMySummaryAsync(companyId, User.GetUserId(), from, to, ct);
        return this.ToActionResult(result);
    }

    /// <summary>Monthly incoming/outgoing series for charts.</summary>
    [HttpGet("monthly")]
    public async Task<ActionResult<IReadOnlyList<MonthlyPointDto>>> GetMonthly(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetMyMonthlyAsync(companyId, User.GetUserId(), from, to, ct);
        return this.ToActionResult(result);
    }
}
