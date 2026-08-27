using System.Security.Cryptography;
using System.Text;

using FluentValidation;

using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Auth;
using GestionFinanciera.Application.Features.Auth.DTOs;
using GestionFinanciera.Application.Features.Auth.Interfaces;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GestionFinanciera.Infrastructure.Identity;

/// <summary>
/// Auth implementation using ASP.NET Core Identity + JWT.
/// Register creates a company with its Admin and the default categories.
/// </summary>
public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ApplicationDbContext dbContext,
    JwtService jwtService,
    IOptions<JwtOptions> jwtOptions,
    IValidator<RegisterDto> registerValidator,
    IValidator<LoginDto> loginValidator,
    IValidator<DemoLoginDto> demoLoginValidator,
    IOptions<DemoOptions> demoOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly DemoOptions _demoOptions = demoOptions.Value;

    private static readonly (string Name, string? Description)[] DefaultCategories =
    [
        ("Marketing", "Marketing and advertising expenses"),
        ("Sales", "Revenue from sales"),
        ("Operations", "Operational costs"),
    ];

    public async Task<Result<(AuthResponseDto Auth, string RefreshToken)>> RegisterAsync(
        RegisterDto dto, CancellationToken ct)
    {
        var validation = await registerValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<(AuthResponseDto, string)>.Failure(validation.Errors.First().ErrorMessage);

        var existing = await userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
            return Result<(AuthResponseDto, string)>.Failure("An account with this email already exists.");

        await RoleSeeder.EnsureRolesAsync(roleManager, ct);

        // 1. Company (tenant) — created ONLY here, from the signup payload.
        var company = new Company { Name = dto.CompanyName.Trim() };
        dbContext.Companies.Add(company);

        // 2. Admin user bound to the company
        var user = new ApplicationUser
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email,
            UserName = dto.Email,
            CompanyId = company.Id,
        };

        var createResult = await userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return Result<(AuthResponseDto, string)>.Failure(createResult.Errors.First().Description);

        await userManager.AddToRoleAsync(user, nameof(UserRole.Admin));

        // 3. Default categories for the new company
        var categories = DefaultCategories
            .Select(c => new Category
            {
                CompanyId = company.Id,
                Name = c.Name,
                Description = c.Description,
                IsDefault = true,
            })
            .ToList();

        dbContext.Categories.AddRange(categories);

        // 4. Tokens
        var refreshTokenValue = JwtService.GenerateRefreshToken();
        dbContext.RefreshTokens.Add(CreateRefreshTokenEntity(user.Id, refreshTokenValue));
        await dbContext.SaveChangesAsync(ct);

        string accessToken = jwtService.GenerateAccessToken(
            user.Id, user.FullName, user.Email!, nameof(UserRole.Admin), company.Id);

        logger.LogInformation("New company registered: {CompanyId} by {UserId}", company.Id, user.Id);

        var auth = BuildResponse(accessToken, user, nameof(UserRole.Admin), company);
        return Result<(AuthResponseDto, string)>.Success((auth, refreshTokenValue));
    }

    public async Task<Result<(AuthResponseDto Auth, string RefreshToken)>> LoginAsync(
        LoginDto dto, CancellationToken ct)
    {
        var validation = await loginValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<(AuthResponseDto, string)>.Failure(validation.Errors.First().ErrorMessage);

        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user is null)
        {
            // Same message for unknown email and wrong password — no user enumeration.
            return Result<(AuthResponseDto, string)>.Failure("Invalid email or password.");
        }

        if (await userManager.IsLockedOutAsync(user))
            return Result<(AuthResponseDto, string)>.Failure("Account is temporarily locked. Try again later.");

        bool valid = await userManager.CheckPasswordAsync(user, dto.Password);
        if (!valid)
        {
            await userManager.AccessFailedAsync(user);
            return Result<(AuthResponseDto, string)>.Failure("Invalid email or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var company = await dbContext.Companies
            .SingleAsync(c => c.Id == user.CompanyId, ct);

        var role = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";

        var refreshTokenValue = JwtService.GenerateRefreshToken();
        dbContext.RefreshTokens.Add(CreateRefreshTokenEntity(user.Id, refreshTokenValue));
        await dbContext.SaveChangesAsync(ct);

        string accessToken = jwtService.GenerateAccessToken(
            user.Id, user.FullName, user.Email!, role, company.Id);

        var auth = BuildResponse(accessToken, user, role, company);
        return Result<(AuthResponseDto, string)>.Success((auth, refreshTokenValue));
    }

    public async Task<Result<(AuthResponseDto Auth, string RefreshToken)>> RefreshAsync(
        string refreshToken, CancellationToken ct)
    {
        string tokenHash = HashToken(refreshToken);

        var stored = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTimeOffset.UtcNow)
            return Result<(AuthResponseDto, string)>.Failure("Invalid or expired refresh token.");

        // Rotation: revoke the old token, issue a new one.
        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null)
            return Result<(AuthResponseDto, string)>.Failure("User no longer exists.");

        var company = await dbContext.Companies
            .SingleAsync(c => c.Id == user.CompanyId, ct);

        var role = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";

        var newRefreshToken = JwtService.GenerateRefreshToken();

        stored.IsRevoked = true;
        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.ReplacedByTokenHash = HashToken(newRefreshToken);

        dbContext.RefreshTokens.Add(CreateRefreshTokenEntity(user.Id, newRefreshToken));
        await dbContext.SaveChangesAsync(ct);

        string accessToken = jwtService.GenerateAccessToken(
            user.Id, user.FullName, user.Email!, role, company.Id);

        var auth = BuildResponse(accessToken, user, role, company);
        return Result<(AuthResponseDto, string)>.Success((auth, newRefreshToken));
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Success();

        string tokenHash = HashToken(refreshToken);
        var stored = await dbContext.RefreshTokens
            .SingleOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

        if (stored is not null && !stored.IsRevoked)
        {
            stored.IsRevoked = true;
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    public async Task<IReadOnlyList<DemoAccountDto>> GetDemoAccountsAsync(CancellationToken ct)
    {
        if (!_demoOptions.Enabled)
            return [];

        return DemoCatalog.ToDtos();
    }

    public async Task<Result<(AuthResponseDto Auth, string RefreshToken)>> DemoLoginAsync(
        DemoLoginDto dto, CancellationToken ct)
    {
        var validation = await demoLoginValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return Result<(AuthResponseDto, string)>.Failure(validation.Errors.First().ErrorMessage);

        if (!_demoOptions.Enabled)
            return Result<(AuthResponseDto, string)>.Failure("Demo access is disabled.");

        DemoCatalog.DemoAccount? account = DemoCatalog.Find(dto.Account);
        if (account is null)
            return Result<(AuthResponseDto, string)>.Failure("Unknown demo account.");

        // Reuse the normal login flow with the seeded demo credentials — the
        // password never leaves the backend.
        return await LoginAsync(new LoginDto(account.Email, _demoOptions.Password), ct);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static RefreshToken CreateRefreshTokenEntity(Guid userId, string tokenValue) =>
        new()
        {
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        };

    /// <summary>SHA-256 hash of the token — the raw value is never persisted.</summary>
    private static string HashToken(string token)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static AuthResponseDto BuildResponse(
        string accessToken, ApplicationUser user, string role, Company company) =>
        new(
            accessToken,
            user.Id,
            user.FullName,
            user.Email!,
            role,
            company.Id,
            company.Name);
}
