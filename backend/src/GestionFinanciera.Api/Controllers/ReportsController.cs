using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Features.Reports.DTOs;
using GestionFinanciera.Application.Features.Reports.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// Report endpoints: PDF/Excel download of the CALLER'S OWN movements (any
/// role) and report-by-email (Admin only — a paid SMTP operation).
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController(IReportService service) : ControllerBase
{
    private const string PdfContentType = "application/pdf";
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("pdf")]
    public async Task<ActionResult<byte[]>> GetPdf(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GeneratePdfAsync(companyId, User.GetUserId(), from, to, ct);
        if (result.IsFailure)
            return this.ToActionResult(result);

        return File(result.Value!, PdfContentType, "account-statement.pdf");
    }

    [HttpGet("excel")]
    public async Task<ActionResult<byte[]>> GetExcel(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GenerateExcelAsync(companyId, User.GetUserId(), from, to, ct);
        if (result.IsFailure)
            return this.ToActionResult(result);

        return File(result.Value!, ExcelContentType, "movements.xlsx");
    }

    [HttpPost("email")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendByEmail(EmailReportDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.SendByEmailAsync(dto, companyId, User.GetUserId(), ct);
        if (result.IsFailure)
            return this.ToActionResult(result);

        return Accepted(new { message = "Report email queued." });
    }
}
