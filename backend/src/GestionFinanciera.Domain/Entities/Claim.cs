using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Domain.Entities;

/// <summary>
/// A claim (reclamación) opened by an involved client when something is wrong
/// with a movement. The Admin acts as MEDIATOR: proposes a corrective transfer
/// (e.g. refund the difference) → the parties CONSENT → a corrective movement
/// is executed and stacked on the ledger. Nothing is ever overwritten.
/// </summary>
public sealed class Claim : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    /// <summary>The movement this claim is about.</summary>
    public Guid MovementId { get; set; }

    public Movement Movement { get; set; } = null!;

    /// <summary>The client that opened the claim (payer or payee of the movement).</summary>
    public Guid ClaimantAccountId { get; set; }

    public ClientAccount ClaimantAccount { get; set; } = null!;

    /// <summary>Why the client believes the movement is wrong (free text).</summary>
    public string Reason { get; set; } = string.Empty;

    public ClaimStatus Status { get; set; } = ClaimStatus.Open;

    /// <summary>Amount the Admin proposes to move back (corrective transfer). Null until proposed.</summary>
    public decimal? ProposedAmount { get; set; }

    /// <summary>Account that should send the corrective transfer (Admin's proposal).</summary>
    public Guid? CorrectiveFromAccountId { get; set; }

    /// <summary>Account that should receive the corrective transfer (Admin's proposal).</summary>
    public Guid? CorrectiveToAccountId { get; set; }

    /// <summary>True once the payer of the corrective transfer consents.</summary>
    public bool PayerConsented { get; set; }

    /// <summary>True once the payee of the corrective transfer consents.</summary>
    public bool PayeeConsented { get; set; }

    /// <summary>Admin's resolution note (why resolved/rejected).</summary>
    public string? ResolutionNote { get; set; }

    /// <summary>The corrective movement executed when this claim was resolved. Null if none.</summary>
    public Guid? ResolutionMovementId { get; set; }

    /// <summary>
    /// Scalar-only projection for the audit trail. Serializing the entity
    /// itself would hit navigation properties and fail on reference cycles.
    /// </summary>
    public object ToAuditSnapshot() => new
    {
        Id,
        MovementId,
        ClaimantAccountId,
        Reason,
        Status,
        ProposedAmount,
        CorrectiveFromAccountId,
        CorrectiveToAccountId,
        PayerConsented,
        PayeeConsented,
        ResolutionNote,
        ResolutionMovementId,
        CreatedAt,
        UpdatedAt,
    };
}
