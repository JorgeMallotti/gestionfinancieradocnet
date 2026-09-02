namespace GestionFinanciera.Application.Features.Claims.DTOs;

/// <summary>A client opens a claim about a movement it is involved in.</summary>
public sealed record OpenClaimDto(
    Guid MovementId,
    string Reason);
