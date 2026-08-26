using GestionFinanciera.Application.Common.Audit;
using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.UnitTests.Fakes;

/// <summary>Records audit calls in memory so tests can assert the audit trail.</summary>
internal sealed class FakeAuditService : IAuditService
{
    public List<RecordedAudit> Entries { get; } = [];

    public Task RecordAsync(
        Guid companyId, Guid userId, AuditAction action, string entity, Guid entityId,
        string? beforeJson, string? afterJson, string? ipAddress, CancellationToken ct)
    {
        Entries.Add(new RecordedAudit(
            companyId, userId, action, entity, entityId, beforeJson, afterJson, ipAddress));
        return Task.CompletedTask;
    }

    public Task<PagedResult<AuditLogDto>> GetByCompanyAsync(
        Guid companyId, PaginationQuery query, CancellationToken ct)
    {
        var filtered = Entries.Where(e => e.CompanyId == companyId).ToList();
        var page = filtered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new AuditLogDto(
                Guid.NewGuid(), e.UserId, e.Action, e.Entity, e.EntityId,
                e.BeforeJson, e.AfterJson, e.IpAddress, DateTimeOffset.UtcNow))
            .ToList();

        return Task.FromResult(new PagedResult<AuditLogDto>(page, filtered.Count, query.Page, query.PageSize));
    }
}

internal sealed record RecordedAudit(
    Guid CompanyId,
    Guid UserId,
    AuditAction Action,
    string Entity,
    Guid EntityId,
    string? BeforeJson,
    string? AfterJson,
    string? IpAddress);
