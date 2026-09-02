using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Domain.Entities;

/// <summary>
/// A simple loan (MVP): a client requests an amount with a reason → the Admin
/// approves/rejects → on approval the bank treasury transfers the amount to the
/// client (a <see cref="MovementType.LoanDisbursement"/> movement) → the client
/// repays anytime, full or partial, with a transfer back
/// (<see cref="MovementType.LoanRepayment"/>). No interest, no deadlines in the MVP.
/// </summary>
public sealed class Loan : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid ClientAccountId { get; set; }

    public ClientAccount ClientAccount { get; set; } = null!;

    /// <summary>Positive amount lent by the bank.</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EUR";

    /// <summary>Why the client needs the loan (free text, shown to the Admin).</summary>
    public string Reason { get; set; } = string.Empty;

    public LoanStatus Status { get; set; } = LoanStatus.Pending;

    /// <summary>Admin user that approved/rejected this loan.</summary>
    public Guid? DecidedByUserId { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    public string? DecisionNote { get; set; }

    /// <summary>
    /// Scalar-only projection for the audit trail. Serializing the entity
    /// itself would hit navigation properties and fail on reference cycles.
    /// </summary>
    public object ToAuditSnapshot() => new
    {
        Id,
        ClientAccountId,
        Amount,
        Currency,
        Reason,
        Status,
        DecidedByUserId,
        DecidedAt,
        DecisionNote,
        CreatedAt,
        UpdatedAt,
    };
}
