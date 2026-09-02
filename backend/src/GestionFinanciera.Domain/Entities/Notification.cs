using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Domain.Entities;

/// <summary>
/// A user notification ("bell" in the UI). Created whenever a user interacts
/// with another user (transfer received, loan decided/repaid, claim consent)
/// or the bank acts on an account. The frontend polls them and navigates to
/// the related resource when one is clicked.
/// </summary>
public sealed class Notification : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    /// <summary>Recipient user (a client, or the Admin who owns the treasury).</summary>
    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }

    /// <summary>Resource the notification points to (movement/loan/claim/account id).</summary>
    public Guid? RelatedId { get; set; }

    /// <summary>Who triggered it (display name of the counterparty), when relevant.</summary>
    public string? ActorName { get; set; }

    /// <summary>Optional amount shown in the message (transfer/loan/correction).</summary>
    public decimal? Amount { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
