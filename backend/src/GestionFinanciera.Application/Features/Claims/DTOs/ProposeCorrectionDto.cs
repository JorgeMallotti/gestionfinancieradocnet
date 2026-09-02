namespace GestionFinanciera.Application.Features.Claims.DTOs;

/// <summary>Admin proposes a corrective transfer (who returns what to whom).</summary>
public sealed record ProposeCorrectionDto(
    decimal Amount,
    Guid FromAccountId,
    Guid ToAccountId,
    string? Note);
