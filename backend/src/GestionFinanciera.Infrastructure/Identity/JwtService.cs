using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GestionFinanciera.Infrastructure.Identity;

/// <summary>JWT settings bound from configuration.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access token lifetime (e.g. "00:20:00").</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>Refresh token lifetime (e.g. "07.00:00:00").</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
}

/// <summary>Creates and validates signed JWT access tokens.</summary>
public sealed class JwtService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public string GenerateAccessToken(
        Guid userId, string fullName, string email, string role, Guid companyId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, fullName),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("role", role),
            new Claim("company_id", companyId.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(_options.AccessTokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Returns a cryptographically random refresh token value.</summary>
    public static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
