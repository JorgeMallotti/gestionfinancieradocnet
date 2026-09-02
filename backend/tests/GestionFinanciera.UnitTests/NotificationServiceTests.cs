using GestionFinanciera.Application.Features.Notifications;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.UnitTests.Fakes;

namespace GestionFinanciera.UnitTests;

/// <summary>
/// Notification retention: the bell keeps only the newest 30 notifications
/// per user — the list and the badge never grow unbounded. Read notifications
/// are NOT deleted on their own; they are dropped only when newer ones push
/// them outside the retention window.
/// </summary>
public sealed class NotificationServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly FakeNotificationRepository _repository = new();

    private NotificationService CreateService() => new(_repository);

    [Fact]
    public async Task NotifyAsync_WhenUnderCap_KeepsEverything()
    {
        _repository.Seed(CompanyId, UserId, ageMinutes: 60, isRead: true);
        _repository.Seed(CompanyId, UserId, ageMinutes: 30, isRead: true);
        var service = CreateService();

        await service.NotifyAsync(
            CompanyId, UserId, NotificationType.LoanRequested, null, "Ana", 100m, CancellationToken.None);

        Assert.Equal(3, _repository.Items.Count);
        var added = _repository.Items.Single(n => n.Type == NotificationType.LoanRequested);
        Assert.Equal(UserId, added.UserId);
        Assert.Equal("Ana", added.ActorName);
        Assert.Equal(100m, added.Amount);
    }

    [Fact]
    public async Task NotifyAsync_AboveCap_DropsTheOldestOnly()
    {
        // 30 existing notifications, oldest first — the retention window is full.
        var oldest = _repository.Seed(CompanyId, UserId, ageMinutes: 30 + 30);
        for (int i = 0; i < 29; i++)
        {
            _repository.Seed(CompanyId, UserId, ageMinutes: 30 - i);
        }

        var service = CreateService();

        // A new one arrives: the window slides and the oldest is dropped.
        await service.NotifyAsync(
            CompanyId, UserId, NotificationType.LoanApproved, null, null, 200m, CancellationToken.None);

        Assert.Equal(30, _repository.Items.Count);
        Assert.DoesNotContain(_repository.Items, n => n.Id == oldest.Id);
        Assert.Contains(_repository.Items, n => n.Type == NotificationType.LoanApproved);
    }

    [Fact]
    public async Task NotifyAsync_ReadNotificationsSurviveInsideTheWindow()
    {
        // A read notification near the top of the window…
        var read = _repository.Seed(CompanyId, UserId, ageMinutes: 10, isRead: true);
        // …plus 30 older ones fill the window and overflow it.
        for (int i = 0; i < 30; i++)
        {
            _repository.Seed(CompanyId, UserId, ageMinutes: 60 + i, isRead: true);
        }

        var service = CreateService();

        await service.NotifyAsync(
            CompanyId, UserId, NotificationType.ClientApproved, null, null, null, CancellationToken.None);

        // The read one sits inside the newest 30 → it is NOT removed on its
        // own; only the overflow (the oldest outside the window) is dropped.
        Assert.Contains(_repository.Items, n => n.Id == read.Id);
        Assert.Equal(30, _repository.Items.Count);
    }
}
