using GestionFinanciera.Api.Extensions;
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
    IOptions<JwtOptions> jwtOptions,
    IConfiguration configuration,
    IWebHostEnvironment environment) : ControllerBase
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

    /// <summary>
    /// Public metadata of the demo quick-access accounts (no credentials).
    /// The login page renders the one-click buttons from this list.
    /// </summary>
    [HttpGet("demo-accounts")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<DemoAccountDto>>> GetDemoAccounts(
        CancellationToken ct)
    {
        var accounts = await authService.GetDemoAccountsAsync(ct);
        return Ok(accounts);
    }

    /// <summary>
    /// One-click demo login. The account key is resolved server-side to the
    /// seeded credentials — the password never travels to (or from) the client.
    /// </summary>
    [HttpPost("demo-login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponseDto>> DemoLogin(
        DemoLoginDto dto, CancellationToken ct)
    {
        var result = await authService.DemoLoginAsync(dto, ct);
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
            // Secure is decided by the ENVIRONMENT, not by Request.IsHttps:
            // behind Azure App Service the request only looks like HTTPS when
            // ForwardedHeaders is enabled, so tying the flag to it meant one
            // misconfigured App Setting silently dropped `Secure` from the
            // cookie. In Development http has to keep working.
            Secure = !environment.IsDevelopment(),
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
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
        });
    }

    /// <summary>
    /// CSRF defence: the Origin header (if present) must be one of the CORS
    /// allowlisted origins. We compare against the allowlist — NOT against the
    /// current Host — because in production the frontend and the API live on
    /// different domains, and in dev the proxy changes the Host port.
    /// The header is attacker-controlled and is not always a URI, so parsing and
    /// the fail-closed behaviour live in <see cref="OriginPolicy"/>.
    /// </summary>
    private bool IsSameOriginRequest()
    {
        string[] allowed = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? [];

        return OriginPolicy.IsAllowed(Request.Headers.Origin, allowed);
    }
}
