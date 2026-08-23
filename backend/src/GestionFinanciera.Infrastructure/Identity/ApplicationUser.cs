using GestionFinanciera.Domain.Entities;

using Microsoft.AspNetCore.Identity;

namespace GestionFinanciera.Infrastructure.Identity;

/// <summary>
/// Identity user. Belongs to exactly one company (tenant).
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
