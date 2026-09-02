namespace GestionFinanciera.Application.Features.Movements.DTOs;

/// <summary>Request to send money from the caller's account to another account
/// (P2P transfer). The payer is always the caller — never taken from the body.</summary>
public sealed record TransferDto(
    Guid ToAccountId,
    decimal Amount,
    Guid? CategoryId,
    string? Description);
