using GestionFinanciera.Application.Common.Audit;
using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace GestionFinanciera.Infrastructure.Services;

/// <summary>
/// Audit trail implementation. Every sensitive mutation writes an AuditLog row
/// with Before/After JSON snapshots; the Admin viewer reads them paginated.
/// The client IP is captured by the API controller and passed in — the service
/// never touches HttpContext.
/// </summary>
public sealed class AuditService(ApplicationDbContext dbContext) : IAuditService
{
    public async Task RecordAsync(
        Guid companyId,
        Guid userId,
        AuditAction action,
        string entity,
        Guid entityId,
        string? beforeJson,
        string? afterJson,
        string? ipAddress,
        CancellationToken ct)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            CompanyId = companyId,
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            IpAddress = ipAddress,
        });

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AuditLogDto>> GetByCompanyAsync(
        Guid companyId, PaginationQuery query, CancellationToken ct)
    {
        IQueryable<AuditLog> source = dbContext.AuditLogs
            .Where(a => a.CompanyId == companyId)
            .OrderByDescending(a => a.CreatedAt);

        int total = await source.CountAsync(ct);

        List<AuditLog> items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>(
            items.Select(AuditLogDto.FromEntity).ToList(),
            total,
            query.Page,
            query.PageSize);
    }
}
