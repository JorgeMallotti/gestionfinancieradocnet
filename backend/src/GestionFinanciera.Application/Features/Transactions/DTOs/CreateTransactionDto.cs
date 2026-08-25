using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Transactions.DTOs;

/// <summary>Create-transaction contract. The enum is serialized as string (Income/Expense).</summary>
public sealed record CreateTransactionDto(
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    string Currency,
    DateTimeOffset Date,
    string? Description);
