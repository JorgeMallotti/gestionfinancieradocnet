using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Loans.DTOs;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Loans.Interfaces;

/// <summary>
/// Loan business logic contract (simple MVP loans: no interest, no deadlines).
/// A client requests → the Admin approves/rejects → on approval the treasury
/// transfers the amount to the client → the client repays anytime (full/partial).
/// </summary>
public interface ILoanService
{
    Task<Result<LoanDto>> RequestAsync(
        RequestLoanDto dto, Guid companyId, Guid clientUserId, CancellationToken ct);

    Task<Result<IReadOnlyList<LoanDto>>> GetMyLoansAsync(
        Guid companyId, Guid clientUserId, CancellationToken ct);

    Task<Result<IReadOnlyList<LoanDto>>> GetAllAsync(
        Guid companyId, string role, LoanStatus? status, CancellationToken ct);

    Task<Result<LoanDto>> DecideAsync(
        Guid id, DecideLoanDto dto, Guid companyId, Guid adminUserId, string role, CancellationToken ct);

    Task<Result<LoanDto>> RepayAsync(
        Guid id, RepayLoanDto dto, Guid companyId, Guid clientUserId, CancellationToken ct);
}
