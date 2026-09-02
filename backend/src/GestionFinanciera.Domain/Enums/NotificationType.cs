namespace GestionFinanciera.Domain.Enums;

/// <summary>
/// Kind of notification. Every type carries enough context (actor + amount)
/// for the UI to build a localized message and navigate to the related page
/// (movements / loans / claims / dashboard).
/// </summary>
public enum NotificationType
{
    /// <summary>Someone sent money to the recipient's account.</summary>
    TransferReceived = 1,

    /// <summary>The bank approved a loan request.</summary>
    LoanApproved = 2,

    /// <summary>The bank rejected a loan request.</summary>
    LoanRejected = 3,

    /// <summary>A client repaid a loan (the bank is notified).</summary>
    LoanRepaid = 4,

    /// <summary>The Admin proposed a corrective transfer on a claim the user is part of.</summary>
    ClaimProposed = 5,

    /// <summary>The other party consented to a corrective transfer — the user's consent is next.</summary>
    ClaimCounterpartyConsented = 6,

    /// <summary>A corrective transfer was executed (both parties consented).</summary>
    ClaimResolved = 7,

    /// <summary>The bank approved the client's account.</summary>
    ClientApproved = 8,

    /// <summary>The bank suspended the client's account.</summary>
    ClientSuspended = 9,

    /// <summary>A client requested a loan (the bank operator is notified).</summary>
    LoanRequested = 10,
}
