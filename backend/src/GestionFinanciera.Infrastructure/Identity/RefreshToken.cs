using GestionFinanciera.Domain.Entities;

namespace GestionFinanciera.Infrastructure.Identity;

/// <summary>
/// Refresh token record. Only the SHA-256 hash is stored — never the raw token.
/// Rotation: each refresh invalidates the previous token.
/// </summary>
public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }
}
