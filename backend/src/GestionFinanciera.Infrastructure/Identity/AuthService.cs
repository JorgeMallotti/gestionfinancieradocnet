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
/// Register opens a CLIENT account (Pending) under the single seeded bank.
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

        // The bank (Company) is a single seeded row. If it does not exist yet
        // (e.g. fresh DB, demo disabled), create it — the MVP has ONE bank and
        // registration NEVER creates another one.
        Company company = await GetOrCreateBankAsync(ct);

        // 1. Client user (role User — never Admin) bound to the bank.
        var user = new ApplicationUser
        {
            FullName = dto.DisplayName.Trim(),
            Email = dto.Email,
            UserName = dto.Email,
            CompanyId = company.Id,
        };

        var createResult = await userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return Result<(AuthResponseDto, string)>.Failure(createResult.Errors.First().Description);

        await userManager.AddToRoleAsync(user, nameof(UserRole.User));

        // 2. Client account with status Pending — the bank Admin must approve it
        //    before the client can operate (no balance yet).
        dbContext.ClientAccounts.Add(new ClientAccount
        {
            CompanyId = company.Id,
            OwnerUserId = user.Id,
            DisplayName = dto.DisplayName.Trim(),
            Kind = dto.Kind,
            Status = AccountStatus.Pending,
            Balance = 0m,
            Currency = "EUR",
            IsTreasury = false,
        });

        // 3. Tokens.
        var refreshTokenValue = JwtService.GenerateRefreshToken();
        dbContext.RefreshTokens.Add(CreateRefreshTokenEntity(user.Id, refreshTokenValue));
        await dbContext.SaveChangesAsync(ct);

        string accessToken = jwtService.GenerateAccessToken(
            user.Id, user.FullName, user.Email!, nameof(UserRole.User), company.Id);

        logger.LogInformation("New client account requested: {UserId} under bank {CompanyId} (Pending approval).",
            user.Id, company.Id);

        var auth = BuildResponse(accessToken, user, nameof(UserRole.User), company);
        return Result<(AuthResponseDto, string)>.Success((auth, refreshTokenValue));
    }

    /// <summary>Finds the single bank or creates it once (idempotent by name).</summary>
    private async Task<Company> GetOrCreateBankAsync(CancellationToken ct)
    {
        Company? bank = await dbContext.Companies
            .SingleOrDefaultAsync(c => c.Name == DemoCatalog.CompanyName, ct);

        if (bank is not null)
            return bank;

        bank = new Company { Name = DemoCatalog.CompanyName };
        dbContext.Companies.Add(bank);
        await dbContext.SaveChangesAsync(ct);
        return bank;
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

        // Demo identities are exempt from lockout: their password is public (the
        // whole point of the one-click demo), so there is nothing to brute-force
        // and the lockout only served an attacker — five wrong passwords against
        // a published email disabled the demo buttons for every visitor. Skipping
        // the check also makes a lockout left by an earlier attack harmless.
        if (!IsDemoAccount(user.Email) && await userManager.IsLockedOutAsync(user))
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

    /// <summary>
    /// Demo accounts are exempt from lockout, but ONLY while demo mode is on:
    /// with Demo:Enabled=false those identities are ordinary users and the
    /// usual lockout policy applies again.
    /// </summary>
    private bool IsDemoAccount(string? email) =>
        _demoOptions.Enabled && DemoCatalog.IsDemoAccountEmail(email);

    /// <summary>
    /// The lifetime of the stored row MUST equal the lifetime of the cookie
    /// issued by the controller — both now come from Jwt:RefreshTokenLifetime.
    /// It used to be hardcoded to 7 days: shortening the configured lifetime
    /// left orphan rows behind, and lengthening it made refresh fail early
    /// (the row expired before the cookie did).
    /// </summary>
    private RefreshToken CreateRefreshTokenEntity(Guid userId, string tokenValue) =>
        new()
        {
            UserId = userId,
            TokenHash = HashToken(tokenValue),
            ExpiresAt = DateTimeOffset.UtcNow.Add(_jwtOptions.RefreshTokenLifetime),
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
