namespace GestionFinanciera.Domain.Enums;

/// <summary>Application roles, stored as Identity roles. Bank demo model:
/// Admin = bank operator/mediator, User = client (person or company).</summary>
public enum UserRole
{
    Admin = 1,
    User = 2,
}
