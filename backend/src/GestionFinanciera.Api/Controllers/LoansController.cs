using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Features.Loans.DTOs;
using GestionFinanciera.Application.Features.Loans.Interfaces;
using GestionFinanciera.Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// Loans (simple MVP: no interest, no deadlines). Clients request and repay;
/// the bank Admin decides. On approval the treasury funds the client and a
/// LoanDisbursement movement is appended to the ledger.
/// </summary>
[ApiController]
[Route("api/loans")]
[Authorize]
public sealed class LoansController(ILoanService service) : ControllerBase
{
    /// <summary>Request a loan from the bank (client).</summary>
    [HttpPost]
    public async Task<ActionResult<LoanDto>> Create(RequestLoanDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.RequestAsync(dto, companyId, userId, ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LoanDto>> GetById(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var role = User.GetRole() ?? string.Empty;

        // Clients only see their own loans; the Admin sees any.
        if (role != nameof(UserRole.Admin))
        {
            var mine = await service.GetMyLoansAsync(companyId, User.GetUserId(), ct);
            var loan = mine.Value?.FirstOrDefault(l => l.Id == id);
            return loan is null
                ? NotFound(new ProblemDetails { Detail = "Loan not found." })
                : Ok(loan);
        }

        var all = await service.GetAllAsync(companyId, role, status: null, ct);
        var adminLoan = all.Value?.FirstOrDefault(l => l.Id == id);
        return adminLoan is null
            ? NotFound(new ProblemDetails { Detail = "Loan not found." })
            : Ok(adminLoan);
    }

    /// <summary>The caller's own loans (client).</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<LoanDto>>> GetMine(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetMyLoansAsync(companyId, User.GetUserId(), ct);
        return this.ToActionResult(result);
    }

    /// <summary>All loans, optionally by status (Admin).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<LoanDto>>> GetAll(
        [FromQuery] LoanStatus? status, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.GetAllAsync(companyId, User.GetRole() ?? string.Empty, status, ct);
        return this.ToActionResult(result);
    }

    /// <summary>Approve or reject a pending loan (Admin).</summary>
    [HttpPost("{id:guid}/decide")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LoanDto>> Decide(Guid id, DecideLoanDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.DecideAsync(
            id, dto, companyId, User.GetUserId(), User.GetRole() ?? string.Empty, ct);
        return this.ToActionResult(result);
    }

    /// <summary>Repay a loan, full or partial (client).</summary>
    [HttpPost("{id:guid}/repay")]
    public async Task<ActionResult<LoanDto>> Repay(Guid id, RepayLoanDto dto, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.RepayAsync(id, dto, companyId, User.GetUserId(), ct);
        return this.ToActionResult(result);
    }
}
