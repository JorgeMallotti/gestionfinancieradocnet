using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Domain.Entities;

/// <summary>
/// A client's account (wallet). Every client of the bank has exactly one
/// account in the MVP. The bank itself also has ONE treasury account
/// (<see cref="IsTreasury"/>) with a very high starting balance.
///
/// The <see cref="Balance"/> is the CURRENT state (a snapshot). The full
/// history lives in <see cref="Movement"/> rows (append-only). The balance
/// is derived from movements and re-computed on every mutation.
/// </summary>
public sealed class ClientAccount : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    /// <summary>The ApplicationUser (Identity) that owns this account.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Display name shown to counterparties (person name or company name).</summary>
    public string DisplayName { get; set; } = string.Empty;

    public ClientKind Kind { get; set; }

    public AccountStatus Status { get; set; } = AccountStatus.Pending;

    /// <summary>Current balance. Hard rule: never goes below zero for anyone.</summary>
    public decimal Balance { get; set; }

    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// True for the single treasury account owned by the bank (the Admin
    /// operator). Not a different entity — same table, same balance rule.
    /// </summary>
    public bool IsTreasury { get; set; }

    public ICollection<Movement> OutgoingMovements { get; set; } = [];

    public ICollection<Movement> IncomingMovements { get; set; } = [];

    /// <summary>
    /// Scalar-only projection for the audit trail. Serializing the entity
    /// itself would hit navigation properties and fail on reference cycles.
    /// </summary>
    public object ToAuditSnapshot() => new
    {
        Id,
        OwnerUserId,
        DisplayName,
        Kind,
        Status,
        Balance,
        Currency,
        IsTreasury,
        CreatedAt,
        UpdatedAt,
    };
}
