using GestionFinanciera.Application.Features.Companies.Interfaces;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace GestionFinanciera.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of the minimal company repository.</summary>
public sealed class CompanyRepository(ApplicationDbContext dbContext) : ICompanyRepository
{
    public Task<string?> GetNameAsync(Guid companyId, CancellationToken ct) =>
        dbContext.Companies
            .Where(c => c.Id == companyId)
            .Select(c => c.Name)
            .SingleOrDefaultAsync(ct);
}
