using GestionFinanciera.Domain.Entities;

namespace GestionFinanciera.Application.Features.Notifications.Interfaces;

/// <summary>Data access contract for user notifications.</summary>
public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> GetForUserAsync(
        Guid companyId, Guid userId, bool unreadOnly, int limit, CancellationToken ct);

    Task<long> CountUnreadAsync(Guid companyId, Guid userId, CancellationToken ct);

    Task AddAsync(Notification notification, CancellationToken ct);

    /// <summary>Marks one notification read. Returns false when it is not the user's.</summary>
    Task<bool> MarkReadAsync(Guid id, Guid companyId, Guid userId, CancellationToken ct);

    Task MarkAllReadAsync(Guid companyId, Guid userId, CancellationToken ct);

    /// <summary>
    /// Deletes the user's notifications that fall outside the keepLatest most
    /// recent ones — a retention cap so the bell never grows unbounded.
    /// </summary>
    Task PruneAsync(Guid companyId, Guid userId, int keepLatest, CancellationToken ct);
}
