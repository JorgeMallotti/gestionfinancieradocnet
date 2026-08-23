namespace GestionFinanciera.Domain.Exceptions;

/// <summary>
/// Raised when a business rule is violated (not for expected validation errors —
/// those are represented with the Result pattern).
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
