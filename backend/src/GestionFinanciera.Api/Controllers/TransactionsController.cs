using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Features.Transactions.DTOs;
using GestionFinanciera.Application.Features.Transactions.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// Transaction endpoints. Mutations are restricted to Admin/Finance both here (HTTP)
/// and in the service layer. The tenant always comes from the JWT — never from
/// the query string or body.
/// </summary>
[ApiController]
[Route("api/transactions")]
[Authorize]
public sealed class TransactionsController(ITransactionService service) : ControllerBase
{
    /// <summary>Paginated + filtered list. Optional filters: type, category, date range.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionDto>>> GetAll(
        [FromQuery] TransactionQueryDto query, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetByCompanyAsync(companyId, query, ct);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> GetById(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetByIdAsync(id, companyId, ct);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult<TransactionDto>> Create(CreateTransactionDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.CreateAsync(
            dto, companyId, userId, User.GetRole() ?? string.Empty, HttpContext.GetClientIpAddress(), ct);

        if (result.IsFailure)
            return this.ToActionResult(result);

        var created = result.Value!;
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult<TransactionDto>> Update(Guid id, UpdateTransactionDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.UpdateAsync(
            id, dto, companyId, userId, User.GetRole() ?? string.Empty, HttpContext.GetClientIpAddress(), ct);

        return this.ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.DeleteAsync(
            id, companyId, userId, User.GetRole() ?? string.Empty, HttpContext.GetClientIpAddress(), ct);

        return this.ToActionResult(result);
    }
}
