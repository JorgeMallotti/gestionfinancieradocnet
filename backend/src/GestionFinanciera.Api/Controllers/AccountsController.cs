using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Features.Accounts.DTOs;
using GestionFinanciera.Application.Features.Accounts.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// The caller's own account (clients see theirs; the Admin owns the treasury)
/// plus the active accounts available as transfer counterparties.
/// </summary>
[ApiController]
[Route("api/accounts")]
[Authorize]
public sealed class AccountsController(IAccountService service) : ControllerBase
{
    /// <summary>The caller's own account — balance and status.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<AccountDto>> GetMyAccount(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.GetMyAccountAsync(companyId, userId, ct);
        return this.ToActionResult(result);
    }

    /// <summary>Active accounts the caller may send money to (never itself).</summary>
    [HttpGet("counterparties")]
    public async Task<ActionResult<IReadOnlyList<AccountRefDto>>> GetCounterparties(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.GetCounterpartiesAsync(companyId, userId, ct);
        return this.ToActionResult(result);
    }
}
