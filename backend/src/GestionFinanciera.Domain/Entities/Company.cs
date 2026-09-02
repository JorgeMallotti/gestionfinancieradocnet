namespace GestionFinanciera.Domain.Entities;

/// <summary>
/// A tenant. In the bank demo model the single seeded Company IS the bank
/// (one row in the MVP); every account/movement belongs to it via CompanyId
/// so the schema is ready for multiple banks later without migrations.
/// </summary>
public sealed class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<Category> Categories { get; set; } = [];

    public ICollection<ClientAccount> ClientAccounts { get; set; } = [];

    public ICollection<Movement> Movements { get; set; } = [];
}
