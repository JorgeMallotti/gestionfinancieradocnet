namespace GestionFinanciera.Domain.Entities;

/// <summary>
/// A transaction category (e.g. Marketing, Sales, Operations). Scoped to a company.
/// </summary>
public sealed class Category : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// True for the seed categories created automatically when a company signs up
    /// (Marketing, Sales, Operations).
    /// </summary>
    public bool IsDefault { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = [];
}
