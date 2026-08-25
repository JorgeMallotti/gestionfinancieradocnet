using GestionFinanciera.Application.Features.Dashboard;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.UnitTests.Fakes;

namespace GestionFinanciera.UnitTests;

public sealed class DashboardServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid OtherCompanyId = Guid.NewGuid();

    private readonly InMemoryTransactionRepository _repository = new();

    private DashboardService CreateService() => new(_repository);

    private void SeedTransaction(
        decimal amount, TransactionType type, DateTimeOffset date, Guid? categoryId = null, Guid? companyId = null)
    {
        Guid category = categoryId ?? Guid.NewGuid();
        _repository.CategoryNames[category] = "Sales";
        _repository.Items.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId ?? CompanyId,
            CategoryId = category,
            Type = type,
            Amount = amount,
            Currency = "EUR",
            Date = date,
        });
    }

    // ── Summary ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummary_ComputesTotalsAndBalance()
    {
        var now = DateTimeOffset.UtcNow;
        SeedTransaction(1000, TransactionType.Income, now);
        SeedTransaction(400, TransactionType.Expense, now);
        SeedTransaction(200, TransactionType.Income, now.AddMonths(-1));

        var result = await CreateService().GetSummaryAsync(CompanyId, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1200, result.Value!.TotalIncome);
        Assert.Equal(400, result.Value.TotalExpenses);
        Assert.Equal(800, result.Value.Balance);
        Assert.Equal(3, result.Value.TransactionCount);
    }

    [Fact]
    public async Task GetSummary_IgnoresOtherCompanies()
    {
        var now = DateTimeOffset.UtcNow;
        SeedTransaction(1000, TransactionType.Income, now);
        SeedTransaction(99999, TransactionType.Income, now, companyId: OtherCompanyId);

        var result = await CreateService().GetSummaryAsync(CompanyId, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1000, result.Value!.TotalIncome);
        Assert.Equal(1, result.Value.TransactionCount);
    }

    [Fact]
    public async Task GetSummary_RespectsDateRange()
    {
        SeedTransaction(100, TransactionType.Income, new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        SeedTransaction(250, TransactionType.Income, new DateTimeOffset(2026, 2, 10, 0, 0, 0, TimeSpan.Zero));
        var from = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await CreateService().GetSummaryAsync(CompanyId, from, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(250, result.Value!.TotalIncome);
        Assert.Equal(1, result.Value.TransactionCount);
    }

    [Fact]
    public async Task GetSummary_InvalidRange_Fails()
    {
        var from = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await CreateService().GetSummaryAsync(CompanyId, from, to, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("after", result.Error);
    }

    // ── Breakdown ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBreakdown_ComputesPercentages()
    {
        var sales = Guid.NewGuid();
        var travel = Guid.NewGuid();
        _repository.CategoryNames[sales] = "Sales";
        _repository.CategoryNames[travel] = "Travel";

        _repository.Items.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            CategoryId = sales,
            Type = TransactionType.Expense,
            Amount = 300,
            Date = DateTimeOffset.UtcNow,
        });
        _repository.Items.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            CategoryId = travel,
            Type = TransactionType.Expense,
            Amount = 100,
            Date = DateTimeOffset.UtcNow,
        });
        // Income must not pollute the expense breakdown.
        _repository.Items.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            CategoryId = sales,
            Type = TransactionType.Income,
            Amount = 999,
            Date = DateTimeOffset.UtcNow,
        });

        var result = await CreateService().GetBreakdownAsync(
            CompanyId, TransactionType.Expense, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal("Sales", result.Value[0].CategoryName);
        Assert.Equal(75, result.Value[0].Percentage);
        Assert.Equal("Travel", result.Value[1].CategoryName);
        Assert.Equal(25, result.Value[1].Percentage);
    }

    // ── Monthly ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMonthly_ComputesBalancePerMonth()
    {
        SeedTransaction(1000, TransactionType.Income, new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero));
        SeedTransaction(400, TransactionType.Expense, new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero));
        SeedTransaction(2000, TransactionType.Income, new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

        var result = await CreateService().GetMonthlyAsync(CompanyId, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        Assert.Equal(2026, result.Value[0].Year);
        Assert.Equal(1, result.Value[0].Month);
        Assert.Equal(1000, result.Value[0].Income);
        Assert.Equal(400, result.Value[0].Expenses);
        Assert.Equal(600, result.Value[0].Balance);

        Assert.Equal(2, result.Value[1].Month);
        Assert.Equal(2000, result.Value[1].Income);
        Assert.Equal(2000, result.Value[1].Balance);
    }
}
