using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Claims.DTOs;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Features.Claims.Interfaces;

/// <summary>
/// Claim business logic contract (Admin = mediator).
/// Flow: a client opens a claim on a movement → the Admin proposes a corrective
/// transfer → BOTH parties consent → the corrective movement is executed and
/// stacked on the ledger. Nothing is ever overwritten.
/// </summary>
public interface IClaimService
{
    Task<Result<ClaimDto>> OpenAsync(
        OpenClaimDto dto, Guid companyId, Guid claimantUserId, CancellationToken ct);

    Task<Result<IReadOnlyList<ClaimDto>>> GetMyClaimsAsync(
        Guid companyId, Guid clientUserId, CancellationToken ct);

    Task<Result<IReadOnlyList<ClaimDto>>> GetAllAsync(
        Guid companyId, string role, ClaimStatus? status, CancellationToken ct);

    /// <summary>Admin proposes a corrective transfer between the two parties.</summary>
    Task<Result<ClaimDto>> ProposeAsync(
        Guid id, ProposeCorrectionDto dto, Guid companyId, Guid adminUserId, string role, CancellationToken ct);

    /// <summary>The given client consents (or refuses) the proposed correction.</summary>
    Task<Result<ClaimDto>> ConsentAsync(
        Guid id, bool approve, Guid companyId, Guid clientUserId, CancellationToken ct);
}
