using GestionFinanciera.Application.Features.Auth.DTOs;
using GestionFinanciera.Application.Features.Auth.Interfaces;
using GestionFinanciera.Infrastructure.Identity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace GestionFinanciera.Api.Controllers;

/// <summary>
/// Public authentication endpoints. The refresh token travels ONLY in an
/// httpOnly + Secure + SameSite=Strict cookie — JavaScript never reads it.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    public const string RefreshCookieName = "refresh_token";

    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        RegisterDto dto, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(dto, ct);
        if (result.IsFailure)
            return Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);

        SetRefreshTokenCookie(result.Value.RefreshToken);
        return CreatedAtAction(nameof(Login), new { }, result.Value.Auth);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var result = await authService.LoginAsync(dto, ct);
        if (result.IsFailure)
            return Problem(result.Error, statusCode: StatusCodes.Status401Unauthorized);

        SetRefreshTokenCookie(result.Value.RefreshToken);
        return Ok(result.Value.Auth);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(CancellationToken ct)
    {
        if (!IsSameOriginRequest())
            return Problem("Invalid request origin.", statusCode: StatusCodes.Status403Forbidden);

        string? cookieToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrWhiteSpace(cookieToken))
            return Problem("Missing refresh token.", statusCode: StatusCodes.Status401Unauthorized);

        var result = await authService.RefreshAsync(cookieToken, ct);
        if (result.IsFailure)
        {
            DeleteRefreshTokenCookie();
            return Problem(result.Error, statusCode: StatusCodes.Status401Unauthorized);
        }

        SetRefreshTokenCookie(result.Value.RefreshToken);
        return Ok(result.Value.Auth);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        string? cookieToken = Request.Cookies[RefreshCookieName];
        await authService.LogoutAsync(cookieToken ?? string.Empty, ct);
        DeleteRefreshTokenCookie();
        return NoContent();
    }

    // ── Cookie helpers ─────────────────────────────────────────────────────

    private void SetRefreshTokenCookie(string token)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,            // JavaScript cannot read it (XSS-safe)
            Secure = Request.IsHttps,   // HTTPS only (localhost dev stays http)
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",         // Sent only to auth endpoints
            Expires = DateTimeOffset.UtcNow.Add(_jwtOptions.RefreshTokenLifetime),
        };

        Response.Cookies.Append(RefreshCookieName, token, options);
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
        });
    }

    /// <summary>CSRF defence: the Origin (if present) must match this host.</summary>
    private bool IsSameOriginRequest()
    {
        string? origin = Request.Headers.Origin;
        if (string.IsNullOrEmpty(origin))
            return true; // non-browser clients (curl) have no Origin header

        var uri = new Uri(origin, UriKind.Absolute);
        return uri.Host == Request.Host.Host && uri.Port == Request.Host.Port;
    }
}
