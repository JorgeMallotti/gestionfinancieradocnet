using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Application.Common.Audit;

/// <summary>Audit entry response — read-only, visible only to company Admins.</summary>
public sealed record AuditLogDto(
    Guid Id,
    Guid UserId,
    AuditAction Action,
    string Entity,
    Guid EntityId,
    string? BeforeJson,
    string? AfterJson,
    string? IpAddress,
    DateTimeOffset CreatedAt)
{
    public static AuditLogDto FromEntity(AuditLog auditLog) =>
        new(
            auditLog.Id,
            auditLog.UserId,
            auditLog.Action,
            auditLog.Entity,
            auditLog.EntityId,
            auditLog.BeforeJson,
            auditLog.AfterJson,
            auditLog.IpAddress,
            auditLog.CreatedAt);
}
