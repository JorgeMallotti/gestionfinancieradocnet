namespace GestionFinanciera.Application.Features.Loans.DTOs;

/// <summary>Admin's decision on a pending loan (approve/reject + optional note).</summary>
public sealed record DecideLoanDto(
    bool Approve,
    string? Note);
