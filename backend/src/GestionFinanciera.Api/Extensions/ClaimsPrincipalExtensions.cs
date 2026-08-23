using System.Security.Claims;

namespace GestionFinanciera.Api.Extensions;

/// <summary>
/// Extracts identity data from the JWT claims — the ONLY source of tenant/user
/// identity. Client input is never trusted for this.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Claim name for the tenant id (see JwtService).</summary>
    public const string CompanyIdClaim = "company_id";

    public static Guid GetCompanyId(this ClaimsPrincipal principal)
    {
        string? value = principal.FindFirstValue(CompanyIdClaim);
        return Guid.TryParse(value, out Guid companyId) ? companyId : Guid.Empty;
    }

    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        string? value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        return Guid.TryParse(value, out Guid userId) ? userId : Guid.Empty;
    }

    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email);

    public static bool HasCompanyId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(CompanyIdClaim), out _);
}
