using GestionFinanciera.Application.Features.Claims.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>In-memory claim repository for unit tests.</summary>
internal sealed class InMemoryClaimRepository : IClaimRepository
{
    private readonly List<Claim> _items = [];

    public List<Claim> Items => _items;

    public Task<Claim?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct) =>
        Task.FromResult(_items.FirstOrDefault(c => c.Id == id && c.CompanyId == companyId));

    public Task<IReadOnlyList<Claim>> GetByAccountAsync(
        Guid companyId, Guid accountId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Claim>>(_items
            .Where(c => c.CompanyId == companyId
                && (c.ClaimantAccountId == accountId
                    || c.CorrectiveFromAccountId == accountId
                    || c.CorrectiveToAccountId == accountId))
            .OrderByDescending(c => c.CreatedAt)
            .ToList());

    public Task<IReadOnlyList<Claim>> GetByCompanyAsync(
        Guid companyId, ClaimStatus? status, CancellationToken ct)
    {
        IEnumerable<Claim> query = _items.Where(c => c.CompanyId == companyId);
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return Task.FromResult<IReadOnlyList<Claim>>(
            query.OrderByDescending(c => c.CreatedAt).ToList());
    }

    public Task AddAsync(Claim claim, CancellationToken ct)
    {
        _items.Add(claim);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Claim claim, CancellationToken ct)
    {
        claim.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    /// <summary>Test helper: seeds a claim and returns it.</summary>
    public Claim Seed(Guid companyId, Guid movementId, Guid claimantAccountId,
        ClaimStatus status = ClaimStatus.Open)
    {
        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            MovementId = movementId,
            ClaimantAccountId = claimantAccountId,
            Reason = "The amount is wrong.",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _items.Add(claim);
        return claim;
    }
}
