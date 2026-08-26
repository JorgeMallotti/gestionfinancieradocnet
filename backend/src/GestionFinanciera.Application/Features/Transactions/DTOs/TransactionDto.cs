using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Transactions.DTOs;

/// <summary>Transaction response — returned by every mutation so the frontend can
/// update its local state without refetching (AGENTS.md §12).</summary>
public sealed record TransactionDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    TransactionType Type,
    decimal Amount,
    string Currency,
    DateTimeOffset Date,
    string? Description,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt)
{
    public static TransactionDto FromEntity(Transaction transaction) =>
        new(
            transaction.Id,
            transaction.CategoryId,
            transaction.Category?.Name ?? string.Empty,
            transaction.Type,
            transaction.Amount,
            transaction.Currency,
            transaction.Date,
            transaction.Description,
            transaction.CreatedByUserId,
            transaction.CreatedAt);
}
