using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Domain.Entities;

/// <summary>
/// A financial movement (income or expense). Scoped to a company.
/// </summary>
public sealed class Transaction : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    /// <summary>The ApplicationUser (Identity) that created this transaction.</summary>
    public Guid CreatedByUserId { get; set; }

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EUR";

    public DateTimeOffset Date { get; set; }

    public string? Description { get; set; }
}
