using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Common.Audit;

/// <summary>
/// Audit trail contract. Services call <c>RecordAsync</c> after every sensitive
/// mutation; the Admin audit viewer reads via <c>GetByCompanyAsync</c>.
/// </summary>
public interface IAuditService
{
    Task RecordAsync(
        Guid companyId,
        Guid userId,
        AuditAction action,
        string entity,
        Guid entityId,
        string? beforeJson,
        string? afterJson,
        string? ipAddress,
        CancellationToken ct);

    Task<PagedResult<AuditLogDto>> GetByCompanyAsync(
        Guid companyId, PaginationQuery query, CancellationToken ct);
}
