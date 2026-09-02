using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Loans.Interfaces;

/// <summary>Data access contract for loans.</summary>
public interface ILoanRepository
{
    Task<Loan?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);

    Task<IReadOnlyList<Loan>> GetByClientAsync(
        Guid companyId, Guid clientAccountId, CancellationToken ct);

    Task<IReadOnlyList<Loan>> GetByCompanyAsync(
        Guid companyId, LoanStatus? status, CancellationToken ct);

    Task AddAsync(Loan loan, CancellationToken ct);

    Task UpdateAsync(Loan loan, CancellationToken ct);
}
