namespace GestionFinanciera.Application.Features.Loans.DTOs;

/// <summary>A client repays a loan (full or partial).</summary>
public sealed record RepayLoanDto(
    decimal Amount);
