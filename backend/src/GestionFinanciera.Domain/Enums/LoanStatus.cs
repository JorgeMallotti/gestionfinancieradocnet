namespace GestionFinanciera.Domain.Enums;

/// <summary>Lifecycle of a loan request. Simple MVP model: no interest, no deadlines.</summary>
public enum LoanStatus
{
    /// <summary>Requested by the client, awaiting the Admin's decision.</summary>
    Pending = 1,

    /// <summary>Approved — the treasury transferred the amount to the client.</summary>
    Approved = 2,

    /// <summary>Rejected by the Admin.</summary>
    Rejected = 3,

    /// <summary>Fully repaid by the client.</summary>
    Repaid = 4,
}
