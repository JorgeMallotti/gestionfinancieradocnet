using GestionFinanciera.Application.Features.Auth;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GestionFinanciera.Infrastructure.Persistence;

/// <summary>
/// Seeds the single demo BANK (Acme Demo Bank) with its operator Admin and two
/// demo clients — Ana (person) and XYZ Solutions SL (company) — so the MVP demo
/// works with one click: P2P transfers, loans and claims are all explorable.
///
/// Idempotent: safe to run on every startup. No-op when Demo:Enabled=false.
/// Runs with NO tenant (no request context), so the multi-tenant query filters
/// are neutral (they only apply when a CompanyId is present).
/// </summary>
public sealed class DemoSeeder(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ApplicationDbContext dbContext,
    IOptions<DemoOptions> demoOptions,
    ILogger<DemoSeeder> logger)
{
    private readonly DemoOptions _options = demoOptions.Value;

    /// <summary>Seed values per demo account key: display name + starting balance.</summary>
    private static readonly Dictionary<string, (string DisplayName, ClientKind Kind, decimal StartBalance, bool IsTreasury)> AccountSeeds =
        new()
        {
            // The bank operator owns the treasury (one account per user, uniform).
            ["admin"] = ("Acme Demo Bank Treasury", ClientKind.Company, 1_000_000m, true),
            ["ana"] = ("Ana García", ClientKind.Person, 5_000m, false),
            ["xyz"] = ("XYZ Solutions SL", ClientKind.Company, 12_000m, false),
        };

    /// <summary>Bank-managed category catalog (visible to every client, optional tag).</summary>
    private static readonly (string Name, string Description)[] BankCategories =
    [
        ("Salary", "Income from work or contracts"),
        ("Shopping", "Purchases of goods"),
        ("Services", "Paid services and subscriptions"),
        ("Food", "Restaurants and groceries"),
        ("Utilities", "Bills and basic services"),
    ];

    /// <summary>P2P transfers between the two demo clients (ledger examples).</summary>
    private static readonly (string FromKey, string ToKey, decimal Amount, string Category, string Description)[] SampleTransfers =
    [
        ("ana", "xyz", 1_200m, "Services", "Web design retainer — Q3"),
        ("xyz", "ana", 2_400m, "Salary", "Monthly salary — Ana"),
        ("ana", "xyz", 350m, "Shopping", "Office supplies from Ana's shop"),
        ("xyz", "ana", 800m, "Services", "Consulting — tax advisory"),
        ("ana", "xyz", 150m, "Food", "Team lunch reimbursement"),
        ("xyz", "ana", 2_400m, "Salary", "Monthly salary — Ana"),
    ];

    public async Task SeedAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Demo seeding skipped (Demo:Enabled=false).");
            return;
        }

        logger.LogInformation("Seeding demo accounts ({Count} roles)...", DemoCatalog.Accounts.Count);
        await RoleSeeder.EnsureRolesAsync(roleManager, ct);

        Company company = await GetOrCreateCompanyAsync(ct);

        foreach (DemoCatalog.DemoAccount account in DemoCatalog.Accounts)
            await EnsureDemoUserAsync(company.Id, account, ct);

        await SeedSampleDataAsync(company.Id, ct);

        logger.LogInformation("Demo seeding completed for company {CompanyId}", company.Id);
    }

    /// <summary>
    /// Wipes the demo bank's mutable demo data (audit trail, movements, loans,
    /// claims) and re-seeds the sample dataset. Demo users, their accounts and
    /// the starting balances are restored so the one-click logins keep working.
    /// Scoped to the demo company only — real banks/clients are never touched.
    /// </summary>
    public async Task ResetDemoDataAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Demo reset skipped (Demo:Enabled=false).");
            return;
        }

        Company? company = await dbContext.Companies
            .SingleOrDefaultAsync(c => c.Name == DemoCatalog.CompanyName, ct);

        if (company is null)
        {
            logger.LogInformation("Demo reset skipped (demo company not found).");
            return;
        }

        logger.LogInformation("Resetting demo data for company {CompanyId}...", company.Id);

        // Runs outside a request, so the tenant query filters are neutral and the
        // explicit CompanyId filters scope the deletes to the demo company only.
        await dbContext.AuditLogs.Where(a => a.CompanyId == company.Id).ExecuteDeleteAsync(ct);
        await dbContext.Claims.Where(c => c.CompanyId == company.Id).ExecuteDeleteAsync(ct);
        await dbContext.Loans.Where(l => l.CompanyId == company.Id).ExecuteDeleteAsync(ct);
        await dbContext.Movements.Where(m => m.CompanyId == company.Id).ExecuteDeleteAsync(ct);

        // ── Ephemeral visitor accounts ──────────────────────────────────────
        // The public demo is a ~24-hour sandbox: accounts created by visitors
        // through the signup flow are removed on every reset, so the demo world
        // always returns exactly to its seed (the signup explains this). The
        // seeded identities (bank operator, Ana, XYZ) are permanent — never
        // touched. Order matters (FKs are Restrict):
        //   audit/claims/loans/movements deleted above → notifications (no FK,
        //   explicit) → client accounts → identity users (roles + refresh
        //   tokens are handled by Identity's cascade).
        string[] demoEmails = DemoCatalog.Accounts.Select(a => a.Email).ToArray();

        List<ApplicationUser> visitorUsers = await dbContext.Users
            .Where(u => u.CompanyId == company.Id && !demoEmails.Contains(u.Email!))
            .ToListAsync(ct);

        if (visitorUsers.Count > 0)
        {
            List<Guid> visitorIds = visitorUsers.Select(u => u.Id).ToList();

            await dbContext.Notifications
                .Where(n => visitorIds.Contains(n.UserId))
                .ExecuteDeleteAsync(ct);

            await dbContext.ClientAccounts
                .Where(a => visitorIds.Contains(a.OwnerUserId))
                .ExecuteDeleteAsync(ct);

            foreach (ApplicationUser visitor in visitorUsers)
                await userManager.DeleteAsync(visitor);

            logger.LogInformation(
                "Reset removed {Count} ephemeral visitor account(s).", visitorUsers.Count);
        }

        // Restore the accounts to their starting balances, then re-seed examples.
        foreach (DemoCatalog.DemoAccount account in DemoCatalog.Accounts)
        {
            if (!AccountSeeds.TryGetValue(account.Key, out var seed))
                continue;

            ApplicationUser? user = await userManager.FindByEmailAsync(account.Email);
            if (user is null)
                continue;

            ClientAccount? clientAccount = await dbContext.ClientAccounts
                .SingleOrDefaultAsync(a => a.CompanyId == company.Id && a.OwnerUserId == user.Id, ct);
            if (clientAccount is null)
                continue;

            clientAccount.Balance = seed.StartBalance;
            clientAccount.Status = AccountStatus.Active;
        }

        await dbContext.SaveChangesAsync(ct);

        await SeedSampleDataAsync(company.Id, ct, force: true);

        logger.LogInformation("Demo data reset completed for company {CompanyId}.", company.Id);
    }

    private async Task<Company> GetOrCreateCompanyAsync(CancellationToken ct)
    {
        Company? company = await dbContext.Companies
            .SingleOrDefaultAsync(c => c.Name == DemoCatalog.CompanyName, ct);

        if (company is null)
        {
            company = new Company { Name = DemoCatalog.CompanyName };
            dbContext.Companies.Add(company);
            await dbContext.SaveChangesAsync(ct);
        }

        return company;
    }

    private async Task EnsureDemoUserAsync(
        Guid companyId, DemoCatalog.DemoAccount account, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        ApplicationUser? existing = await userManager.FindByEmailAsync(account.Email);
        if (existing is not null)
            return;

        if (!AccountSeeds.TryGetValue(account.Key, out var seed))
            return;

        var user = new ApplicationUser
        {
            FullName = seed.DisplayName,
            Email = account.Email,
            UserName = account.Email,
            CompanyId = companyId,
        };

        IdentityResult createResult = await userManager.CreateAsync(user, _options.Password);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("Failed to create demo user {Email}: {Errors}",
                account.Email, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, account.Role);

        dbContext.ClientAccounts.Add(new ClientAccount
        {
            CompanyId = companyId,
            OwnerUserId = user.Id,
            DisplayName = seed.DisplayName,
            Kind = seed.Kind,
            Status = AccountStatus.Active,
            Balance = seed.StartBalance,
            Currency = "EUR",
            IsTreasury = seed.IsTreasury,
        });

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task SeedSampleDataAsync(Guid companyId, CancellationToken ct, bool force = false)
    {
        // Only the first time — keep the dataset stable across restarts
        // (a reset always re-seeds, so it bypasses this check).
        if (!force)
        {
            bool hasData = await dbContext.Movements.AnyAsync(m => m.CompanyId == companyId, ct);
            if (hasData)
                return;
        }

        // ── Category catalog (natural key: CompanyId + Name) ────────────────
        // The catalog is protected by a unique index (IX_Categories_CompanyId_Name),
        // so it must be upserted by name — never inserted blindly. A blind insert
        // breaks the second run: a demo reset wipes movements but not categories,
        // so the empty-movements guard above passes and the INSERT collides,
        // killing the host at startup. Reuse what exists, add only what's missing.
        List<Category> existingCategories = await dbContext.Categories
            .Where(c => c.CompanyId == companyId)
            .ToListAsync(ct);

        Dictionary<string, Category> categoriesByName = existingCategories
            .ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        List<Category> newCategories = BankCategories
            .Where(c => !categoriesByName.ContainsKey(c.Name))
            .Select(c => new Category { CompanyId = companyId, Name = c.Name, Description = c.Description })
            .ToList();

        if (newCategories.Count > 0)
        {
            dbContext.Categories.AddRange(newCategories);
            await dbContext.SaveChangesAsync(ct);

            foreach (Category category in newCategories)
                categoriesByName[category.Name] = category;

            logger.LogInformation("Seeded {Count} missing demo categories.", newCategories.Count);
        }

        // The full catalog in a stable order — existing rows and fresh ones alike.
        var categories = BankCategories.Select(c => categoriesByName[c.Name]).ToList();

        var accounts = await dbContext.ClientAccounts
            .Where(a => a.CompanyId == companyId)
            .ToDictionaryAsync(a => a.DisplayName, ct);

        ClientAccount treasury = accounts["Acme Demo Bank Treasury"];
        ClientAccount ana = accounts["Ana García"];
        ClientAccount xyz = accounts["XYZ Solutions SL"];

        // ── P2P ledger examples ──────────────────────────────────────────────
        DateTimeOffset today = DateTimeOffset.UtcNow.Date;
        var movements = new List<Movement>();

        for (int i = 0; i < SampleTransfers.Length; i++)
        {
            var sample = SampleTransfers[i];
            bool anaPays = sample.FromKey == "ana";
            ClientAccount from = anaPays ? ana : xyz;
            ClientAccount to = anaPays ? xyz : ana;
            Category category = categories.First(c => c.Name == sample.Category);

            // Back-date each transfer across the last ~6 weeks.
            DateTimeOffset date = today.AddDays(-(42 - i * 7));
            date = new DateTimeOffset(date.Year, date.Month, date.Day, 12, 0, 0, TimeSpan.Zero);

            // The ledger must stay consistent: apply the double-entry here too.
            from.Balance -= sample.Amount;
            to.Balance += sample.Amount;

            movements.Add(new Movement
            {
                CompanyId = companyId,
                FromAccountId = from.Id,
                ToAccountId = to.Id,
                Type = MovementType.Transfer,
                Amount = sample.Amount,
                Currency = "EUR",
                CategoryId = category.Id,
                Description = sample.Description,
                OccurredAt = date,
            });
        }

        dbContext.Movements.AddRange(movements);
        await dbContext.SaveChangesAsync(ct);

        // ── One sample loan: XYZ borrowed 5,000, already repaid 2,000 ───────
        var loan = new Loan
        {
            CompanyId = companyId,
            ClientAccountId = xyz.Id,
            Amount = 5_000m,
            RepaidAmount = 2_000m,
            Currency = "EUR",
            Reason = "Working capital for the new office",
            Status = LoanStatus.Approved,
            DecidedAt = today.AddDays(-30),
        };

        dbContext.Loans.Add(loan);
        await dbContext.SaveChangesAsync(ct);

        // The disbursement + repayment are real ledger rows: treasury → XYZ 5,000,
        // then XYZ → treasury 2,000. XYZ's balance above already reflects them.
        var disbursement = new Movement
        {
            CompanyId = companyId,
            FromAccountId = treasury.Id,
            ToAccountId = xyz.Id,
            Type = MovementType.LoanDisbursement,
            Amount = 5_000m,
            Currency = "EUR",
            Description = "Loan disbursement — Working capital for the new office",
            OccurredAt = today.AddDays(-30),
        };

        var repayment = new Movement
        {
            CompanyId = companyId,
            FromAccountId = xyz.Id,
            ToAccountId = treasury.Id,
            Type = MovementType.LoanRepayment,
            Amount = 2_000m,
            Currency = "EUR",
            Description = "Loan repayment (partial)",
            OccurredAt = today.AddDays(-15),
        };

        dbContext.Movements.AddRange(disbursement, repayment);
        await dbContext.SaveChangesAsync(ct);

        // Apply their effect on the balances (the history rows document them).
        treasury.Balance -= 5_000m;
        xyz.Balance += 5_000m;
        xyz.Balance -= 2_000m;
        treasury.Balance += 2_000m;

        // ── One sample claim (resolved via a mediated corrective transfer) ───
        var claimedMovement = movements[0]; // Ana paid XYZ 1,200 — she claims she overpaid.
        var claim = new Claim
        {
            CompanyId = companyId,
            MovementId = claimedMovement.Id,
            ClaimantAccountId = ana.Id,
            Reason = "I paid 1,200 but the agreed amount was 1,000.",
            Status = ClaimStatus.Resolved,
            ProposedAmount = 200m,
            CorrectiveFromAccountId = xyz.Id,
            CorrectiveToAccountId = ana.Id,
            PayerConsented = true,
            PayeeConsented = true,
            ResolutionNote = "Both parties consented — corrective transfer executed.",
        };

        dbContext.Claims.Add(claim);
        await dbContext.SaveChangesAsync(ct);

        var corrective = new Movement
        {
            CompanyId = companyId,
            FromAccountId = xyz.Id,
            ToAccountId = ana.Id,
            Type = MovementType.CorrectiveTransfer,
            Amount = 200m,
            Currency = "EUR",
            CorrectsMovementId = claimedMovement.Id,
            Description = "Corrective transfer after claim — refund of overpayment",
            OccurredAt = today.AddDays(-2),
        };

        dbContext.Movements.Add(corrective);
        await dbContext.SaveChangesAsync(ct);

        xyz.Balance -= 200m;
        ana.Balance += 200m;

        claim.ResolutionMovementId = corrective.Id;
        await dbContext.SaveChangesAsync(ct);
    }
}
