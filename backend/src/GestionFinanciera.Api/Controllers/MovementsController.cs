using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Features.Movements.DTOs;
using GestionFinanciera.Application.Features.Movements.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// The caller's own immutable ledger (movements involving their account).
/// Movements are append-only: GET only — there is NO update/delete (AGENTS.md §2.1).
/// </summary>
[ApiController]
[Route("api/movements")]
[Authorize]
public sealed class MovementsController(IMovementService service) : ControllerBase
{
    /// <summary>Paginated ledger of the caller's account. Optional filters: type, date range.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<MovementDto>>> GetAll(
        [FromQuery] MovementQueryDto query, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.GetMyMovementsAsync(companyId, userId, query, ct);
        return this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MovementDto>> GetById(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetByIdAsync(
            id, companyId, User.GetUserId(), User.GetRole() ?? string.Empty, ct);
        return this.ToActionResult(result);
    }
}
