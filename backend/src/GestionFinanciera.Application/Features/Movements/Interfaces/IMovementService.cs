using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Movements.DTOs;

namespace GestionFinanciera.Application.Features.Movements.Interfaces;

/// <summary>
/// Movement business logic contract (ledger). The caller's account is resolved
/// from the JWT — never from the body. Every mutation returns the full resource.
/// </summary>
public interface IMovementService
{
    /// <summary>Send money from the caller's own account to another account.</summary>
    Task<Result<MovementDto>> TransferAsync(
        TransferDto dto, Guid companyId, Guid fromUserId, CancellationToken ct);

    /// <summary>Paginated ledger of the caller's own account (incoming + outgoing).</summary>
    Task<Result<PagedResult<MovementDto>>> GetMyMovementsAsync(
        Guid companyId, Guid userId, MovementQueryDto query, CancellationToken ct);

    /// <summary>One movement — must involve the caller's account (Admin sees any).</summary>
    Task<Result<MovementDto>> GetByIdAsync(
        Guid id, Guid companyId, Guid userId, string role, CancellationToken ct);
}
