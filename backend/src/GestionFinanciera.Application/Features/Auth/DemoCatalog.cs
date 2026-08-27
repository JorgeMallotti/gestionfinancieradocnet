using GestionFinanciera.Application.Features.Auth.DTOs;

namespace GestionFinanciera.Application.Features.Auth;

/// <summary>
/// Well-known demo accounts for the one-click quick access (MVP demo).
///
/// Security model (AGENTS.md §13): the credentials live ONLY in the backend
/// (Infrastructure seeder + Demo:Password config). The public API exposes the
/// catalog metadata — never passwords.
/// </summary>
public static class DemoCatalog
{
    public const string CompanyName = "Acme Demo SL";

    public sealed record DemoAccount(
        string Key,
        string Role,
        string Email,
        string Description)
    {
        public DemoAccountDto ToDto() => new(Key, Role, CompanyName, Description);
    }

    public static readonly IReadOnlyList<DemoAccount> Accounts =
    [
        new("admin",
            "Admin",
            "demo.admin@gestfin.local",
            "Full access: users, categories, transactions and the audit log."),
        new("finance",
            "Finance",
            "demo.finance@gestfin.local",
            "Reports: PDF/Excel exports and email delivery."),
        new("user",
            "User",
            "demo.user@gestfin.local",
            "Day-to-day operations on categories and transactions."),
    ];

    /// <summary>Resolves a demo account by key (case-insensitive).</summary>
    public static DemoAccount? Find(string key) =>
        Accounts.FirstOrDefault(a =>
            string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Public metadata of every demo account (no credentials).</summary>
    public static IReadOnlyList<DemoAccountDto> ToDtos() =>
        Accounts.Select(a => a.ToDto()).ToList();
}
