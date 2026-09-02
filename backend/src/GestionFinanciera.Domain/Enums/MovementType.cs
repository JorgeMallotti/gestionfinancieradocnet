namespace GestionFinanciera.Domain.Enums;

/// <summary>
/// Kind of ledger movement. The ledger is append-only: movements are created
/// once and NEVER edited or deleted (git-style). Corrections are stacked as
/// new movements on top of the original ones.
/// </summary>
public enum MovementType
{
    /// <summary>A client pays another client (or the bank) — P2P transfer.</summary>
    Transfer = 1,

    /// <summary>Bank treasury lends money to a client (loan approved).</summary>
    LoanDisbursement = 2,

    /// <summary>A client repays a loan (full or partial) back to the treasury.</summary>
    LoanRepayment = 3,

    /// <summary>
    /// A corrective transfer executed after a claim is resolved with the
    /// consent of both parties (e.g. refund of a wrong amount). Links back
    /// to the original movement via <c>CorrectsMovementId</c>.
    /// </summary>
    CorrectiveTransfer = 4,

    /// <summary>The opening balance of an account (seeded once).</summary>
    InitialBalance = 5,
}
