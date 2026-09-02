using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Common.Audit;
using GestionFinanciera.Application.Common.Pagination;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// Read-only audit trail viewer. Admin only (both at HTTP level and enforced in
/// the service layer). Scoped to the company from the JWT.
/// </summary>
[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Admin")]
public sealed class AuditController(IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAll(
        [FromQuery] PaginationQuery query, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await auditService.GetByCompanyAsync(companyId, query, ct);
        return Ok(result);
    }
}
