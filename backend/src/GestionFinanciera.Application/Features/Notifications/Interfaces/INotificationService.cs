using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Notifications.DTOs;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Notifications.Interfaces;

/// <summary>
/// Notification contract. Services that move money or mediate interactions
/// call <c>NotifyAsync</c>; the UI reads them via the controller.
/// </summary>
public interface INotificationService
{
    /// <summary>Creates a notification for a recipient user.</summary>
    Task NotifyAsync(
        Guid companyId,
        Guid toUserId,
        NotificationType type,
        Guid? relatedId,
        string? actorName,
        decimal? amount,
        CancellationToken ct);

    /// <summary>The caller's notifications (read + unread, newest first).</summary>
    Task<Result<IReadOnlyList<NotificationDto>>> GetForUserAsync(
        Guid companyId, Guid userId, bool unreadOnly, int limit, CancellationToken ct);

    /// <summary>Unread count for the bell badge.</summary>
    Task<Result<long>> CountUnreadAsync(Guid companyId, Guid userId, CancellationToken ct);

    /// <summary>Marks one of the caller's notifications as read.</summary>
    Task<Result> MarkReadAsync(Guid id, Guid companyId, Guid userId, CancellationToken ct);

    /// <summary>Marks all of the caller's notifications as read.</summary>
    Task<Result> MarkAllReadAsync(Guid companyId, Guid userId, CancellationToken ct);
}
