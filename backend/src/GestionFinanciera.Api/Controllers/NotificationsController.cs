using GestionFinanciera.Api.Extensions;
using GestionFinanciera.Application.Features.Notifications.DTOs;
using GestionFinanciera.Application.Features.Notifications.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// The caller's own notifications (the bell). Created whenever a user
/// interacts with another user or the bank acts on their account.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    /// <summary>The caller's notifications (unread first), newest first.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetMine(
        [FromQuery] bool unreadOnly = false, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var companyId = User.GetCompanyId();
        var userId = User.GetUserId();
        var result = await service.GetForUserAsync(companyId, userId, unreadOnly, Math.Clamp(limit, 1, 100), ct);
        return this.ToActionResult(result);
    }

    /// <summary>Unread count for the bell badge.</summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<long>> GetUnreadCount(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.CountUnreadAsync(companyId, User.GetUserId(), ct);
        return this.ToActionResult(result);
    }

    /// <summary>Marks one notification as read.</summary>
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.MarkReadAsync(id, companyId, User.GetUserId(), ct);
        return this.ToActionResult(result);
    }

    /// <summary>Marks all notifications as read.</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var companyId = User.GetCompanyId();
        var result = await service.MarkAllReadAsync(companyId, User.GetUserId(), ct);
        return this.ToActionResult(result);
    }
}
