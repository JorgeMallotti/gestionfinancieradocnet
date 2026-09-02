using GestionFinanciera.Application.Features.Accounts.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace GestionFinanciera.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of the client-account repository (incl. treasury).</summary>
public sealed class AccountRepository(ApplicationDbContext dbContext) : IAccountRepository
{
    public async Task<ClientAccount?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct) =>
        await dbContext.ClientAccounts
            .Where(a => a.Id == id && a.CompanyId == companyId)
            .SingleOrDefaultAsync(ct);

    public async Task<ClientAccount?> GetByOwnerUserIdAsync(
        Guid companyId, Guid ownerUserId, CancellationToken ct) =>
        await dbContext.ClientAccounts
            .Where(a => a.CompanyId == companyId && a.OwnerUserId == ownerUserId)
            .SingleOrDefaultAsync(ct);

    public async Task<ClientAccount?> GetTreasuryAsync(Guid companyId, CancellationToken ct) =>
        await dbContext.ClientAccounts
            .Where(a => a.CompanyId == companyId && a.IsTreasury)
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<ClientAccount>> GetClientsAsync(
        Guid companyId, AccountStatus? status, CancellationToken ct)
    {
        IQueryable<ClientAccount> query = dbContext.ClientAccounts
            .Where(a => a.CompanyId == companyId && !a.IsTreasury);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query
            .OrderBy(a => a.DisplayName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ClientAccount>> GetActiveCounterpartiesAsync(
        Guid companyId, Guid excludeAccountId, CancellationToken ct) =>
        await dbContext.ClientAccounts
            .Where(a => a.CompanyId == companyId
                && a.Id != excludeAccountId
                && a.Status == AccountStatus.Active)
            .OrderBy(a => a.DisplayName)
            .ToListAsync(ct);

    public async Task AddAsync(ClientAccount account, CancellationToken ct)
    {
        await dbContext.ClientAccounts.AddAsync(account, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ClientAccount account, CancellationToken ct)
    {
        dbContext.ClientAccounts.Update(account);
        await dbContext.SaveChangesAsync(ct);
    }
}
