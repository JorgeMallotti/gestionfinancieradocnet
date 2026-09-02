using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Notifications.DTOs;
using GestionFinanciera.Application.Features.Notifications.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Notifications;

/// <summary>Creates and reads user notifications (no business rules beyond scoping).</summary>
public sealed class NotificationService(INotificationRepository repository) : INotificationService
{
    public async Task NotifyAsync(
        Guid companyId,
        Guid toUserId,
        NotificationType type,
        Guid? relatedId,
        string? actorName,
        decimal? amount,
        CancellationToken ct)
    {
        await repository.AddAsync(new Notification
        {
            CompanyId = companyId,
            UserId = toUserId,
            Type = type,
            RelatedId = relatedId,
            ActorName = actorName,
            Amount = amount,
            OccurredAt = DateTimeOffset.UtcNow,
        }, ct);
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> GetForUserAsync(
        Guid companyId, Guid userId, bool unreadOnly, int limit, CancellationToken ct)
    {
        var items = await repository.GetForUserAsync(companyId, userId, unreadOnly, limit, ct);
        return Result<IReadOnlyList<NotificationDto>>.Success(
            items.Select(NotificationDto.FromEntity).ToList());
    }

    public async Task<Result<long>> CountUnreadAsync(Guid companyId, Guid userId, CancellationToken ct) =>
        Result<long>.Success(await repository.CountUnreadAsync(companyId, userId, ct));

    public async Task<Result> MarkReadAsync(Guid id, Guid companyId, Guid userId, CancellationToken ct)
    {
        bool marked = await repository.MarkReadAsync(id, companyId, userId, ct);
        return marked
            ? Result.Success()
            : Result.Failure(ErrorCode.NotFound, "Notification not found.");
    }

    public async Task<Result> MarkAllReadAsync(Guid companyId, Guid userId, CancellationToken ct)
    {
        await repository.MarkAllReadAsync(companyId, userId, ct);
        return Result.Success();
    }
}
