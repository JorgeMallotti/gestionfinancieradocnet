using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Claims.Interfaces;

/// <summary>Data access contract for claims.</summary>
public interface IClaimRepository
{
    Task<Claim?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct);

    Task<IReadOnlyList<Claim>> GetByAccountAsync(
        Guid companyId, Guid accountId, CancellationToken ct);

    Task<IReadOnlyList<Claim>> GetByCompanyAsync(
        Guid companyId, ClaimStatus? status, CancellationToken ct);

    Task AddAsync(Claim claim, CancellationToken ct);

    Task UpdateAsync(Claim claim, CancellationToken ct);
}
