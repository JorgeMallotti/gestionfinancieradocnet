using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Transactions.DTOs;

/// <summary>Update-transaction contract.</summary>
public sealed record UpdateTransactionDto(
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    string Currency,
    DateTimeOffset Date,
    string? Description);
