namespace GestionFinanciera.Domain.Enums;

/// <summary>Lifecycle of a claim (reclamación) opened by a client over a movement.</summary>
public enum ClaimStatus
{
    /// <summary>Just opened by the involved client — nobody has acted yet.</summary>
    Open = 1,

    /// <summary>The Admin (mediator) proposed a corrective transfer; consent pending.</summary>
    UnderReview = 2,

    /// <summary>The corrective transfer was executed (or no correction was needed).</summary>
    Resolved = 3,

    /// <summary>The claim was rejected/closed without a correction.</summary>
    Rejected = 4,
}
