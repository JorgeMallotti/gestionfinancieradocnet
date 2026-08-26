namespace GestionFinanciera.Domain.Entities;

/// <summary>
/// A tenant — every business record in the system belongs to exactly one company.
/// </summary>
public sealed class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Category> Categories { get; set; } = [];

    public ICollection<Transaction> Transactions { get; set; } = [];
}
