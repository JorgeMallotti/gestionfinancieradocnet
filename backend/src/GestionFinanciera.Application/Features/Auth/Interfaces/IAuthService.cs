using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Auth.DTOs;

namespace GestionFinanciera.Application.Features.Auth.Interfaces;

/// <summary>
/// Authentication contract. The controller resolves identity from the JWT/cookie
/// and passes the rest here; services never see HttpContext.
/// </summary>
public interface IAuthService
{
    /// <summary>Creates a company + its first Admin user, and signs them in.
    /// Returns the auth payload and the raw refresh token (for the httpOnly cookie).</summary>
    Task<Result<(AuthResponseDto Auth, string RefreshToken)>> RegisterAsync(
        RegisterDto dto, CancellationToken ct);

    /// <summary>Signs an existing user in.</summary>
    Task<Result<(AuthResponseDto Auth, string RefreshToken)>> LoginAsync(
        LoginDto dto, CancellationToken ct);

    /// <summary>
    /// Rotates the refresh token and issues a new access token.
    /// Returns the new refresh token value so the caller can set the httpOnly cookie.
    /// </summary>
    Task<Result<(AuthResponseDto Auth, string RefreshToken)>> RefreshAsync(
        string refreshToken, CancellationToken ct);

    /// <summary>Invalidates the refresh token on logout.</summary>
    Task<Result> LogoutAsync(string refreshToken, CancellationToken ct);
}
