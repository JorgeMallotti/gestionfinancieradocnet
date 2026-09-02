using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Claims.DTOs;

/// <summary>Claim response — shows the mediation state and the consent of both parties.</summary>
public sealed record ClaimDto(
    Guid Id,
    Guid MovementId,
    Guid ClaimantAccountId,
    string ClaimantDisplayName,
    string Reason,
    ClaimStatus Status,
    decimal? ProposedAmount,
    Guid? CorrectiveFromAccountId,
    Guid? CorrectiveToAccountId,
    bool PayerConsented,
    bool PayeeConsented,
    string? ResolutionNote,
    Guid? ResolutionMovementId,
    DateTimeOffset CreatedAt)
{
    public static ClaimDto FromEntity(Claim c) => new(
        c.Id,
        c.MovementId,
        c.ClaimantAccountId,
        c.ClaimantAccount?.DisplayName ?? string.Empty,
        c.Reason,
        c.Status,
        c.ProposedAmount,
        c.CorrectiveFromAccountId,
        c.CorrectiveToAccountId,
        c.PayerConsented,
        c.PayeeConsented,
        c.ResolutionNote,
        c.ResolutionMovementId,
        c.CreatedAt);
}
