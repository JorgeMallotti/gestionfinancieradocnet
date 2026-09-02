using GestionFinanciera.Application.Features.Claims.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace GestionFinanciera.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of the claim repository.</summary>
public sealed class ClaimRepository(ApplicationDbContext dbContext) : IClaimRepository
{
    public async Task<Claim?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct) =>
        await dbContext.Claims
            .Include(c => c.ClaimantAccount)
            .Include(c => c.Movement)
            .Where(c => c.Id == id && c.CompanyId == companyId)
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Claim>> GetByAccountAsync(
        Guid companyId, Guid accountId, CancellationToken ct) =>
        await dbContext.Claims
            .Include(c => c.ClaimantAccount)
            .Include(c => c.Movement)
            .Where(c => c.CompanyId == companyId
                && (c.ClaimantAccountId == accountId
                    || c.CorrectiveFromAccountId == accountId
                    || c.CorrectiveToAccountId == accountId))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Claim>> GetByCompanyAsync(
        Guid companyId, ClaimStatus? status, CancellationToken ct)
    {
        IQueryable<Claim> query = dbContext.Claims
            .Include(c => c.ClaimantAccount)
            .Include(c => c.Movement)
            .Where(c => c.CompanyId == companyId);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Claim claim, CancellationToken ct)
    {
        await dbContext.Claims.AddAsync(claim, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Claim claim, CancellationToken ct)
    {
        claim.UpdatedAt = DateTimeOffset.UtcNow;
        dbContext.Claims.Update(claim);
        await dbContext.SaveChangesAsync(ct);
    }
}
