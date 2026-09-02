using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Features.Movements.DTOs;
using GestionFinanciera.Application.Features.Movements.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// P2P transfers. The payer is ALWAYS the caller (from the JWT) — the body only
/// names the receiver, the amount and an optional category/description. A
/// transfer appends an immutable movement to the ledger (never edited/deleted).
/// </summary>
[ApiController]
[Route("api/transfers")]
[Authorize]
public sealed class TransfersController(IMovementService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MovementDto>> Create(TransferDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.TransferAsync(dto, companyId, userId, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(MovementsController.GetById), "Movements",
                new { id = result.Value!.Id }, result.Value)
            : this.ToActionResult(result);
    }
}
