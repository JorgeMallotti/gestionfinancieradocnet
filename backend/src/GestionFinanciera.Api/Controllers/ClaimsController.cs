using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Features.Claims.DTOs;
using GestionFinanciera.Application.Features.Claims.Interfaces;
using GestionFinanciera.Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// Claims (reclamaciones): a client disputes a movement it is part of → the
/// Admin (mediator) proposes a corrective transfer → BOTH parties consent →
/// the corrective movement is executed and stacked on the ledger.
/// </summary>
[ApiController]
[Route("api/claims")]
[Authorize]
public sealed class ClaimsController(IClaimService service) : ControllerBase
{
    /// <summary>Open a claim about a movement the caller is part of (client).</summary>
    [HttpPost]
    public async Task<ActionResult<ClaimDto>> Open(OpenClaimDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.OpenAsync(dto, companyId, User.GetUserId(), ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClaimDto>> GetById(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var role = User.GetRole() ?? string.Empty;

        if (role != nameof(UserRole.Admin))
        {
            var mine = await service.GetMyClaimsAsync(companyId, User.GetUserId(), ct);
            var claim = mine.Value?.FirstOrDefault(c => c.Id == id);
            return claim is null
                ? NotFound(new ProblemDetails { Detail = "Claim not found." })
                : Ok(claim);
        }

        var all = await service.GetAllAsync(companyId, role, status: null, ct);
        var adminClaim = all.Value?.FirstOrDefault(c => c.Id == id);
        return adminClaim is null
            ? NotFound(new ProblemDetails { Detail = "Claim not found." })
            : Ok(adminClaim);
    }

    /// <summary>The claims the caller is involved in (claimant or correction party).</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<ClaimDto>>> GetMine(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetMyClaimsAsync(companyId, User.GetUserId(), ct);
        return this.ToActionResult(result);
    }

    /// <summary>All claims, optionally by status (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<ClaimDto>>> GetAll(
        [FromQuery] ClaimStatus? status, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetAllAsync(companyId, User.GetRole() ?? string.Empty, status, ct);
        return this.ToActionResult(result);
    }

    /// <summary>Propose a corrective transfer between the two parties (Admin).</summary>
    [HttpPost("{id:guid}/propose")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ClaimDto>> Propose(Guid id, ProposeCorrectionDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.ProposeAsync(
            id, dto, companyId, User.GetUserId(), User.GetRole() ?? string.Empty, ct);
        return this.ToActionResult(result);
    }

    /// <summary>Consent (or refuse) the proposed correction (client).</summary>
    [HttpPost("{id:guid}/consent")]
    public async Task<ActionResult<ClaimDto>> Consent(Guid id, [FromQuery] bool approve, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.ConsentAsync(id, approve, companyId, User.GetUserId(), ct);
        return this.ToActionResult(result);
    }
}
