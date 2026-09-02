using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Features.Accounts.DTOs;
using GestionFinanciera.Application.Features.Accounts.Interfaces;
using GestionFinanciera.Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// Bank Admin panel: list clients (pending/active/suspended), approve pending
/// registrations and suspend clients. The Admin's own identity comes from the JWT.
/// </summary>
[ApiController]
[Route("api/admin/clients")]
[Authorize(Roles = "Admin")]
public sealed class AdminClientsController(IAccountService service) : ControllerBase
{
    /// <summary>Client accounts, optionally filtered by status (Pending/Active/Suspended).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> ListClients(
        [FromQuery] AccountStatus? status, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.ListClientsAsync(companyId, User.GetRole() ?? string.Empty, status, ct);
        return this.ToActionResult(result);
    }

    /// <summary>Approve a pending client so it can operate.</summary>
    [HttpPost("{accountId:guid}/approve")]
    public async Task<ActionResult<AccountDto>> Approve(Guid accountId, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.ApproveClientAsync(
            accountId, companyId, User.GetUserId(), User.GetRole() ?? string.Empty,
            HttpContext.GetClientIpAddress(), ct);
        return this.ToActionResult(result);
    }

    /// <summary>Suspend an active client (no operations until re-approved).</summary>
    [HttpPost("{accountId:guid}/suspend")]
    public async Task<ActionResult<AccountDto>> Suspend(Guid accountId, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.SuspendClientAsync(
            accountId, companyId, User.GetUserId(), User.GetRole() ?? string.Empty,
            HttpContext.GetClientIpAddress(), ct);
        return this.ToActionResult(result);
    }
}
