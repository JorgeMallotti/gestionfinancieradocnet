namespace GestionFinanciera.Domain.Enums;

/// <summary>Lifecycle of a client account. A freshly registered client is
/// <see cref="Pending"/> until the bank Admin approves the account.</summary>
public enum AccountStatus
{
    /// <summary>Registered but not yet approved by the Admin — cannot operate.</summary>
    Pending = 1,

    /// <summary>Approved — the client can transfer, request loans and open claims.</summary>
    Active = 2,

    /// <summary>Disabled by the Admin — the client can no longer operate.</summary>
    Suspended = 3,
}
