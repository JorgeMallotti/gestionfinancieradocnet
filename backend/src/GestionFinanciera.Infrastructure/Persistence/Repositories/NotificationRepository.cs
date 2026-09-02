using GestionFinanciera.Application.Features.Notifications.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace GestionFinanciera.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of the notification repository.</summary>
public sealed class NotificationRepository(ApplicationDbContext dbContext) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> GetForUserAsync(
        Guid companyId, Guid userId, bool unreadOnly, int limit, CancellationToken ct)
    {
        IQueryable<Notification> query = dbContext.Notifications
            .Where(n => n.CompanyId == companyId && n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<long> CountUnreadAsync(Guid companyId, Guid userId, CancellationToken ct) =>
        await dbContext.Notifications
            .LongCountAsync(n => n.CompanyId == companyId && n.UserId == userId && !n.IsRead, ct);

    public async Task AddAsync(Notification notification, CancellationToken ct)
    {
        await dbContext.Notifications.AddAsync(notification, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> MarkReadAsync(Guid id, Guid companyId, Guid userId, CancellationToken ct)
    {
        Notification? notification = await dbContext.Notifications
            .SingleOrDefaultAsync(n => n.Id == id && n.CompanyId == companyId && n.UserId == userId, ct);

        if (notification is null || notification.IsRead)
            return notification is not null;

        notification.IsRead = true;
        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task MarkAllReadAsync(Guid companyId, Guid userId, CancellationToken ct)
    {
        await dbContext.Notifications
            .Where(n => n.CompanyId == companyId && n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
