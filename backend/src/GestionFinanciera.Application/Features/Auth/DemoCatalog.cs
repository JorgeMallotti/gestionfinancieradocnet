using GestionFinanciera.Application.Features.Auth.DTOs;

namespace GestionFinanciera.Application.Features.Auth;

/// <summary>
/// Well-known demo accounts for the one-click quick access (MVP demo).
/// Bank model: ONE seeded bank (Acme Demo Bank) whose Admin is the operator,
/// plus two demo clients (a person and a company) so visitors can explore
/// P2P transfers, loans and claims with the demo buttons.
///
/// Security model (AGENTS.md §13): the credentials live ONLY in the backend
/// (Infrastructure seeder + Demo:Password config). The public API exposes the
/// catalog metadata — never passwords.
/// </summary>
public static class DemoCatalog
{
    public const string CompanyName = "Acme Demo Bank";

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
            "Bank operator: approves clients, decides loans, mediates claims."),
        new("ana",
            "User",
            "demo.ana@gestfin.local",
            "Client (person): transfer money and open claims."),
        new("xyz",
            "User",
            "demo.xyz@gestfin.local",
            "Client (company): transfer money and open claims."),
    ];

    /// <summary>Resolves a demo account by key (case-insensitive).</summary>
    public static DemoAccount? Find(string key) =>
        Accounts.FirstOrDefault(a =>
            string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>Public metadata of every demo account (no credentials).</summary>
    public static IReadOnlyList<DemoAccountDto> ToDtos() =>
        Accounts.Select(a => a.ToDto()).ToList();
}
