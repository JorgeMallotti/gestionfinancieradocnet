using GestionFinanciera.Application.Features.Notifications.Interfaces;
using GestionFinanciera.Domain.Entities;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>
/// In-memory notification repository. PruneAsync mirrors the EF retention
/// rule (keep the newest keepLatest by OccurredAt, then Id) so service tests
/// can assert the cap end-to-end without a database.
/// </summary>
internal sealed class FakeNotificationRepository : INotificationRepository
{
    public List<Notification> Items { get; } = [];

    public Task<IReadOnlyList<Notification>> GetForUserAsync(
        Guid companyId, Guid userId, bool unreadOnly, int limit, CancellationToken ct)
    {
        IEnumerable<Notification> query = Items
            .Where(n => n.CompanyId == companyId && n.UserId == userId)
            .OrderByDescending(n => n.OccurredAt)
            .ThenByDescending(n => n.Id);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return Task.FromResult<IReadOnlyList<Notification>>(query.Take(limit).ToList());
    }

    public Task<long> CountUnreadAsync(Guid companyId, Guid userId, CancellationToken ct) =>
        Task.FromResult(Items.LongCount(n =>
            n.CompanyId == companyId && n.UserId == userId && !n.IsRead));

    public Task AddAsync(Notification notification, CancellationToken ct)
    {
        Items.Add(notification);
        return Task.CompletedTask;
    }

    public Task<bool> MarkReadAsync(Guid id, Guid companyId, Guid userId, CancellationToken ct)
    {
        Notification? item = Items.SingleOrDefault(n =>
            n.Id == id && n.CompanyId == companyId && n.UserId == userId);

        if (item is null || item.IsRead)
            return Task.FromResult(item is not null);

        item.IsRead = true;
        return Task.FromResult(true);
    }

    public Task MarkAllReadAsync(Guid companyId, Guid userId, CancellationToken ct)
    {
        foreach (Notification n in Items.Where(n =>
                     n.CompanyId == companyId && n.UserId == userId && !n.IsRead))
        {
            n.IsRead = true;
        }

        return Task.CompletedTask;
    }

    public Task PruneAsync(Guid companyId, Guid userId, int keepLatest, CancellationToken ct)
    {
        var keepIds = Items
            .Where(n => n.CompanyId == companyId && n.UserId == userId)
            .OrderByDescending(n => n.OccurredAt)
            .ThenByDescending(n => n.Id)
            .Select(n => n.Id)
            .Take(keepLatest)
            .ToHashSet();

        Items.RemoveAll(n =>
            n.CompanyId == companyId && n.UserId == userId && !keepIds.Contains(n.Id));
        return Task.CompletedTask;
    }

    /// <summary>Seeds one notification for a user with the given age in minutes.</summary>
    public Notification Seed(Guid companyId, Guid userId, int ageMinutes, bool isRead = false)
    {
        var item = new Notification
        {
            CompanyId = companyId,
            UserId = userId,
            Type = Domain.Enums.NotificationType.TransferReceived,
            IsRead = isRead,
            OccurredAt = DateTimeOffset.UtcNow.AddMinutes(-ageMinutes),
        };
        Items.Add(item);
        return item;
    }
}
