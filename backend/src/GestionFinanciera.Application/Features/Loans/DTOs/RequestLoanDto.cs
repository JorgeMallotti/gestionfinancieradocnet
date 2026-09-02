namespace GestionFinanciera.Application.Features.Loans.DTOs;

/// <summary>A client requests a loan from the bank (amount + reason).</summary>
public sealed record RequestLoanDto(
    decimal Amount,
    string Reason);
