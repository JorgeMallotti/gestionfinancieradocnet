using GestionFinanciera.Application.Features.Accounts.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>In-memory client-account repository for unit tests (no DB, no Moq).</summary>
internal sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly List<ClientAccount> _items = [];

    public List<ClientAccount> Items => _items;

    public Task<ClientAccount?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct) =>
        Task.FromResult(_items.FirstOrDefault(a => a.Id == id && a.CompanyId == companyId));

    public Task<ClientAccount?> GetByOwnerUserIdAsync(
        Guid companyId, Guid ownerUserId, CancellationToken ct) =>
        Task.FromResult(_items.FirstOrDefault(a =>
            a.CompanyId == companyId && a.OwnerUserId == ownerUserId));

    public Task<ClientAccount?> GetTreasuryAsync(Guid companyId, CancellationToken ct) =>
        Task.FromResult(_items.FirstOrDefault(a => a.CompanyId == companyId && a.IsTreasury));

    public Task<IReadOnlyList<ClientAccount>> GetClientsAsync(
        Guid companyId, AccountStatus? status, CancellationToken ct)
    {
        IEnumerable<ClientAccount> query = _items
            .Where(a => a.CompanyId == companyId && !a.IsTreasury);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return Task.FromResult<IReadOnlyList<ClientAccount>>(
            query.OrderBy(a => a.DisplayName).ToList());
    }

    public Task<IReadOnlyList<ClientAccount>> GetActiveCounterpartiesAsync(
        Guid companyId, Guid excludeAccountId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ClientAccount>>(_items
            .Where(a => a.CompanyId == companyId
                && a.Id != excludeAccountId
                && a.Status == AccountStatus.Active)
            .OrderBy(a => a.DisplayName)
            .ToList());

    public Task AddAsync(ClientAccount account, CancellationToken ct)
    {
        _items.Add(account);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ClientAccount account, CancellationToken ct)
    {
        account.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }

    /// <summary>Test helper: adds an account and returns it.</summary>
    public ClientAccount Seed(Guid companyId, Guid ownerUserId, string displayName,
        decimal balance, bool isTreasury = false, AccountStatus status = AccountStatus.Active)
    {
        var account = new ClientAccount
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            OwnerUserId = ownerUserId,
            DisplayName = displayName,
            Kind = isTreasury ? ClientKind.Company : ClientKind.Person,
            Status = status,
            Balance = balance,
            Currency = "EUR",
            IsTreasury = isTreasury,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _items.Add(account);
        return account;
    }
}
