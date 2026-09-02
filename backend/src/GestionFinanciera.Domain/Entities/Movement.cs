using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Domain.Entities;

/// <summary>
/// A single entry of the immutable ledger (append-only, git-style).
///
/// A movement is created ONCE and NEVER edited or deleted: there are no
/// PUT/DELETE endpoints for movements. Corrections are STACKED as new
/// movements (e.g. a corrective transfer that links back via
/// <see cref="CorrectsMovementId"/>), so every correction is traceable.
/// </summary>
public sealed class Movement : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    /// <summary>Account that loses money (payer). The treasury is also an account.</summary>
    public Guid FromAccountId { get; set; }

    public ClientAccount FromAccount { get; set; } = null!;

    /// <summary>Account that gains money (payee). The treasury is also an account.</summary>
    public Guid ToAccountId { get; set; }

    public ClientAccount ToAccount { get; set; } = null!;

    public MovementType Type { get; set; }

    /// <summary>Always positive. Direction is encoded by From/To, never by sign.</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EUR";

    /// <summary>Optional category tag from the Admin-managed catalog.</summary>
    public Guid? CategoryId { get; set; }

    public Category? Category { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// For <see cref="MovementType.CorrectiveTransfer"/>: the original
    /// movement this correction fixes. Null for regular movements.
    /// </summary>
    public Guid? CorrectsMovementId { get; set; }

    public Movement? CorrectsMovement { get; set; }

    /// <summary>When the money actually moved.</summary>
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Scalar-only projection for the audit trail. Serializing the entity
    /// itself would hit navigation properties and fail on reference cycles.
    /// </summary>
    public object ToAuditSnapshot() => new
    {
        Id,
        FromAccountId,
        ToAccountId,
        Type,
        Amount,
        Currency,
        CategoryId,
        Description,
        CorrectsMovementId,
        OccurredAt,
        CreatedAt,
    };
}
