using GestionFinanciera.Application.Features.Loans.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace GestionFinanciera.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of the loan repository.</summary>
public sealed class LoanRepository(ApplicationDbContext dbContext) : ILoanRepository
{
    public async Task<Loan?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct) =>
        await dbContext.Loans
            .Include(l => l.ClientAccount)
            .Where(l => l.Id == id && l.CompanyId == companyId)
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Loan>> GetByClientAsync(
        Guid companyId, Guid clientAccountId, CancellationToken ct) =>
        await dbContext.Loans
            .Include(l => l.ClientAccount)
            .Where(l => l.CompanyId == companyId && l.ClientAccountId == clientAccountId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Loan>> GetByCompanyAsync(
        Guid companyId, LoanStatus? status, CancellationToken ct)
    {
        IQueryable<Loan> query = dbContext.Loans
            .Include(l => l.ClientAccount)
            .Where(l => l.CompanyId == companyId);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Loan loan, CancellationToken ct)
    {
        await dbContext.Loans.AddAsync(loan, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Loan loan, CancellationToken ct)
    {
        loan.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.Loans.Update(loan);
        await dbContext.SaveChangesAsync(ct);
    }
}
