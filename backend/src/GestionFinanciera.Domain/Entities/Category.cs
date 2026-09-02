namespace GestionFinanciera.Domain.Entities;

/// <summary>
/// A movement tag from the Admin-managed catalog (e.g. Nómina, Compra).
/// Clients may optionally tag a movement with one category for their own
/// organization. Visible to every client of the bank.
/// </summary>
public sealed class Category : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<Movement> Movements { get; set; } = [];

    /// <summary>
    /// Scalar-only projection for the audit trail. Serializing the entity itself
    /// would hit navigation properties and fail on reference cycles (EF fixup).
    /// </summary>
    public object ToAuditSnapshot() => new
    {
        Id,
        Name,
        Description,
        CreatedAt,
        UpdatedAt,
    };
}
