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
/// Seeds the demo company, its three role users (Admin/Finance/User) and a
/// realistic sample dataset so the MVP demo works with one click.
///
/// Idempotent: safe to run on every startup. No-op when Demo:Enabled=false.
/// Runs with NO tenant (no request context), so the multi-tenant query
/// filters are neutral (they only apply when a CompanyId is present).
/// </summary>
public sealed class DemoSeeder(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ApplicationDbContext dbContext,
    IOptions<DemoOptions> demoOptions,
    ILogger<DemoSeeder> logger)
{
    private readonly DemoOptions _options = demoOptions.Value;

    private static readonly (string Name, string Description)[] DefaultCategories =
    [
        ("Marketing", "Marketing and advertising expenses"),
        ("Sales", "Revenue from sales"),
        ("Operations", "Operational costs"),
    ];

    /// <summary>12 sample movements spread over the last 3 months (4/month).</summary>
    private static readonly (string Category, TransactionType Type, decimal Amount, string Description)[] SampleTransactions =
    [
        ("Sales", TransactionType.Income, 4800m, "Q3 client onboarding"),
        ("Marketing", TransactionType.Expense, 1250m, "Google Ads campaign"),
        ("Sales", TransactionType.Income, 3150m, "Consulting services"),
        ("Operations", TransactionType.Expense, 780m, "Office supplies"),
        ("Sales", TransactionType.Income, 5200m, "Annual contract renewal"),
        ("Marketing", TransactionType.Expense, 940m, "Social media ads"),
        ("Operations", TransactionType.Expense, 1120m, "Software subscriptions"),
        ("Sales", TransactionType.Income, 2750m, "Training workshop"),
        ("Marketing", TransactionType.Expense, 1580m, "Trade fair booth"),
        ("Operations", TransactionType.Expense, 640m, "Utilities"),
        ("Sales", TransactionType.Income, 6900m, "Enterprise license"),
        ("Operations", TransactionType.Expense, 850m, "Equipment maintenance"),
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

        var user = new ApplicationUser
        {
            FullName = $"Demo {account.Role}",
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
    }

    private async Task SeedSampleDataAsync(Guid companyId, CancellationToken ct)
    {
        // Only the first time — keep the dataset stable across restarts.
        bool hasData = await dbContext.Transactions.AnyAsync(t => t.CompanyId == companyId, ct);
        if (hasData)
            return;

        var categories = DefaultCategories
            .Select(c => new Category
            {
                CompanyId = companyId,
                Name = c.Name,
                Description = c.Description,
                IsDefault = true,
            })
            .ToList();

        dbContext.Categories.AddRange(categories);
        await dbContext.SaveChangesAsync(ct);

        // The Admin demo user owns the sample transactions.
        ApplicationUser? admin = await userManager.FindByEmailAsync(DemoCatalog.Accounts[0].Email);
        Guid adminId = admin?.Id ?? Guid.Empty;

        DateTimeOffset today = DateTimeOffset.UtcNow.Date;
        int[] days = [5, 12, 19, 26];

        var transactions = new List<Transaction>(SampleTransactions.Length);

        for (int i = 0; i < SampleTransactions.Length; i++)
        {
            var sample = SampleTransactions[i];
            var category = categories.First(c => c.Name == sample.Category);

            // 4 movements per month: months 2, 1 and 0 (current) ago.
            DateTimeOffset baseDate = today.AddMonths(-(2 - i / 4));
            int day = Math.Min(days[i % 4], DateTime.DaysInMonth(baseDate.Year, baseDate.Month));

            var date = new DateTimeOffset(baseDate.Year, baseDate.Month, day, 12, 0, 0, TimeSpan.Zero);
            if (date > today)
                date = today; // never seed future-dated rows

            transactions.Add(new Transaction
            {
                CompanyId = companyId,
                CategoryId = category.Id,
                CreatedByUserId = adminId,
                Type = sample.Type,
                Amount = sample.Amount,
                Currency = "EUR",
                Date = date,
                Description = sample.Description,
            });
        }

        dbContext.Transactions.AddRange(transactions);
        await dbContext.SaveChangesAsync(ct);
    }
}
