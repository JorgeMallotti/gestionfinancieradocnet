using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Features.Movements.DTOs;
using GestionFinanciera.Application.Features.Movements.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>
/// In-memory immutable-ledger repository for unit tests. Mirrors the EF Core
/// behaviour: AddAsync performs the double-entry on the account repository,
/// but here balances live on the ClientAccount objects in InMemoryAccountRepository.
/// </summary>
internal sealed class InMemoryMovementRepository : IMovementRepository
{
    private readonly List<Movement> _items = [];

    public List<Movement> Items => _items;

    /// <summary>Optional hook to mutate balances (simulates the double-entry).</summary>
    public Action<Movement>? OnAdd { get; set; }

    public Task<Movement?> GetByIdAsync(Guid id, Guid companyId, CancellationToken ct) =>
        Task.FromResult(_items.FirstOrDefault(m => m.Id == id && m.CompanyId == companyId));

    public Task<PagedResult<Movement>> GetByAccountAsync(
        Guid companyId, Guid accountId, MovementQueryDto query, CancellationToken ct)
    {
        IEnumerable<Movement> source = _items.Where(m =>
            m.CompanyId == companyId && (m.FromAccountId == accountId || m.ToAccountId == accountId));

        if (query.Type.HasValue)
            source = source.Where(m => m.Type == query.Type.Value);

        if (query.From.HasValue)
            source = source.Where(m => m.OccurredAt >= query.From.Value);

        if (query.To.HasValue)
            source = source.Where(m => m.OccurredAt <= query.To.Value);

        var ordered = source.OrderByDescending(m => m.OccurredAt).ToList();
        var page = ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();

        return Task.FromResult(new PagedResult<Movement>(page, ordered.Count, query.Page, query.PageSize));
    }

    public Task<IReadOnlyList<Movement>> GetByAccountInRangeAsync(
        Guid companyId, Guid accountId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        IEnumerable<Movement> source = _items.Where(m =>
            m.CompanyId == companyId && (m.FromAccountId == accountId || m.ToAccountId == accountId));

        if (from.HasValue)
            source = source.Where(m => m.OccurredAt >= from.Value);
        if (to.HasValue)
            source = source.Where(m => m.OccurredAt <= to.Value);

        return Task.FromResult<IReadOnlyList<Movement>>(
            source.OrderBy(m => m.OccurredAt).ToList());
    }

    public Task<long> CountByCategoryAsync(Guid companyId, Guid categoryId, CancellationToken ct) =>
        Task.FromResult(_items.LongCount(m => m.CompanyId == companyId && m.CategoryId == categoryId));

    public Task AddAsync(Movement movement, CancellationToken ct)
    {
        _items.Add(movement);
        OnAdd?.Invoke(movement);
        return Task.CompletedTask;
    }

    /// <summary>Test helper: adds a movement and returns it.</summary>
    public Movement Seed(Guid companyId, Guid fromAccountId, Guid toAccountId,
        MovementType type, decimal amount, DateTimeOffset? occurredAt = null)
    {
        var movement = new Movement
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId,
            Type = type,
            Amount = amount,
            Currency = "EUR",
            OccurredAt = occurredAt ?? DateTimeOffset.UtcNow,
        };

        _items.Add(movement);
        return movement;
    }
}
