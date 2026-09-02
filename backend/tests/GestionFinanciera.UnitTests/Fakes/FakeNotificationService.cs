using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Notifications.DTOs;
using GestionFinanciera.Application.Features.Notifications.Interfaces;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>In-memory notification service — records sends, no-ops the reads.</summary>
internal sealed class FakeNotificationService : INotificationService
{
    public List<(Guid ToUserId, NotificationType Type, Guid? RelatedId, string? ActorName, decimal? Amount)> Sent { get; } = [];

    public Task NotifyAsync(
        Guid companyId, Guid toUserId, NotificationType type, Guid? relatedId,
        string? actorName, decimal? amount, CancellationToken ct)
    {
        Sent.Add((toUserId, type, relatedId, actorName, amount));
        return Task.CompletedTask;
    }

    public Task<Result<IReadOnlyList<NotificationDto>>> GetForUserAsync(
        Guid companyId, Guid userId, bool unreadOnly, int limit, CancellationToken ct) =>
        Task.FromResult(Result<IReadOnlyList<NotificationDto>>.Success([]));

    public Task<Result<long>> CountUnreadAsync(Guid companyId, Guid userId, CancellationToken ct) =>
        Task.FromResult(Result<long>.Success(0));

    public Task<Result> MarkReadAsync(Guid id, Guid companyId, Guid userId, CancellationToken ct) =>
        Task.FromResult(Result.Success());

    public Task<Result> MarkAllReadAsync(Guid companyId, Guid userId, CancellationToken ct) =>
        Task.FromResult(Result.Success());
}
