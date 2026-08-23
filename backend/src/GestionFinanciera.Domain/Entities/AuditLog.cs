using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.Domain.Entities;

/// <summary>
/// Immutable record of a sensitive mutation (create/update/delete).
/// Visible only to company Admins via the API.
/// </summary>
public sealed class AuditLog : BaseEntity
{
    public Guid CompanyId { get; set; }

    public Guid UserId { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>Entity name, e.g. "Transaction".</summary>
    public string Entity { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public string? IpAddress { get; set; }
}
