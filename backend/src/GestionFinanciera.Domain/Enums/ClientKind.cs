namespace GestionFinanciera.Domain.Enums;

/// <summary>Nature of a bank client. In the MVP a client is either a person
/// or a company, always with exactly ONE login.</summary>
public enum ClientKind
{
    Person = 1,
    Company = 2,
}
