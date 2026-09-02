using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Notifications.DTOs;

/// <summary>Notification response. The frontend builds localized messages from
/// the type + actor + amount and navigates to RelatedId's page on click.</summary>
public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    Guid? RelatedId,
    string? ActorName,
    decimal? Amount,
    bool IsRead,
    DateTimeOffset OccurredAt)
{
    public static NotificationDto FromEntity(Domain.Entities.Notification n) => new(
        n.Id,
        n.Type,
        n.RelatedId,
        n.ActorName,
        n.Amount,
        n.IsRead,
        n.OccurredAt);
}
