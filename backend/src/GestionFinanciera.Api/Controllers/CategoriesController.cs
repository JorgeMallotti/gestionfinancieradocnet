using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Features.Categories.DTOs;
using GestionFinanciera.Application.Features.Categories.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// Category endpoints. Mutations are restricted to Admin/Finance both here (HTTP)
/// and in the service layer (business rule). Identity always comes from the JWT.
/// </summary>
[ApiController]
[Route("api/categories")]
[Authorize]
public sealed class CategoriesController(ICategoryService service) : ControllerBase
{
    /// <summary>Paginated list (tables).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<CategoryDto>>> GetAll(
        [FromQuery] PaginationQuery query, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetByCompanyAsync(companyId, query, ct);
        return Ok(result.Value);
    }

    /// <summary>Full unpaginated list (dropdowns, selects).</summary>
    [HttpGet("all")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAllUnpaginated(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetAllByCompanyAsync(companyId, ct);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetByIdAsync(id, companyId, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.CreateAsync(
            dto, companyId, userId, User.GetRole() ?? string.Empty, HttpContext.GetClientIpAddress(), ct);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Detail = result.Error });

        var created = result.Value!;
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.UpdateAsync(
            id, dto, companyId, userId, User.GetRole() ?? string.Empty, HttpContext.GetClientIpAddress(), ct);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Detail = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.DeleteAsync(
            id, companyId, userId, User.GetRole() ?? string.Empty, HttpContext.GetClientIpAddress(), ct);

        if (result.IsFailure)
            return BadRequest(new ProblemDetails { Detail = result.Error });

        return NoContent();
    }
}
