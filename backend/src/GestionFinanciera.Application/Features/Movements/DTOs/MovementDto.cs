using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Movements.DTOs;

/// <summary>
/// A ledger movement as seen by a client: who paid whom, when, for how much,
/// with an optional category tag. The ledger is immutable — there are no
/// update/delete endpoints for movements (AGENTS.md §2.1).
/// </summary>
public sealed record MovementDto(
    Guid Id,
    MovementType Type,
    Guid FromAccountId,
    string FromDisplayName,
    Guid ToAccountId,
    string ToDisplayName,
    decimal Amount,
    string Currency,
    Guid? CategoryId,
    string? CategoryName,
    string? Description,
    Guid? CorrectsMovementId,
    DateTimeOffset OccurredAt)
{
    public static MovementDto FromEntity(Movement m) => new(
        m.Id,
        m.Type,
        m.FromAccountId,
        m.FromAccount?.DisplayName ?? string.Empty,
        m.ToAccountId,
        m.ToAccount?.DisplayName ?? string.Empty,
        m.Amount,
        m.Currency,
        m.CategoryId,
        m.Category?.Name,
        m.Description,
        m.CorrectsMovementId,
        m.OccurredAt);

    /// <summary>True when this movement credited the given account (money in).</summary>
    public bool IsIncomingFor(Guid accountId) => ToAccountId == accountId;

    /// <summary>True when this movement debited the given account (money out).</summary>
    public bool IsOutgoingFor(Guid accountId) => FromAccountId == accountId;
}
