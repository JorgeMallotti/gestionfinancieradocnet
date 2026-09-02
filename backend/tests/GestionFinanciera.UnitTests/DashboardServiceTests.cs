using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Dashboard;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.UnitTests.Fakes;

namespace GestionFinanciera.UnitTests;

public sealed class DashboardServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OtherUserId = Guid.NewGuid();

    private readonly InMemoryAccountRepository _accounts = new();
    private readonly InMemoryMovementRepository _movements = new();

    private DashboardService CreateService() => new(_accounts, _movements);

    // Seeds the caller's account plus a counterpart and movements both ways.
    private (Guid Mine, Guid Other) SeedAccounts(decimal myBalance = 5000m)
    {
        var mine = _accounts.Seed(CompanyId, UserId, "Ana", myBalance);
        var other = _accounts.Seed(CompanyId, OtherUserId, "Bob", 10_000m);
        return (mine.Id, other.Id);
    }

    // ── Summary ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMySummary_ComputesIncomingOutgoingAndBalance()
    {
        var (mine, other) = SeedAccounts(myBalance: 5600m);
        _movements.Seed(CompanyId, other, mine, MovementType.Transfer, 1000m);
        _movements.Seed(CompanyId, mine, other, MovementType.Transfer, 400m);

        var result = await CreateService().GetMySummaryAsync(CompanyId, UserId, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5600m, result.Value!.Balance);
        Assert.Equal(1000m, result.Value.TotalIncoming);
        Assert.Equal(400m, result.Value.TotalOutgoing);
        Assert.Equal(2, result.Value.MovementCount);
    }

    [Fact]
    public async Task GetMySummary_UnknownUser_ReturnsNotFound()
    {
        var result = await CreateService().GetMySummaryAsync(
            CompanyId, Guid.NewGuid(), null, null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Code);
    }

    [Fact]
    public async Task GetMySummary_RespectsDateRange()
    {
        var (mine, other) = SeedAccounts();
        _movements.Seed(CompanyId, other, mine, MovementType.Transfer, 100m,
            new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        _movements.Seed(CompanyId, other, mine, MovementType.Transfer, 250m,
            new DateTimeOffset(2026, 2, 10, 0, 0, 0, TimeSpan.Zero));

        var from = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var result = await CreateService().GetMySummaryAsync(CompanyId, UserId, from, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(250m, result.Value!.TotalIncoming);
        Assert.Equal(1, result.Value.MovementCount);
    }

    [Fact]
    public async Task GetMySummary_InvalidRange_Fails()
    {
        SeedAccounts();
        var from = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await CreateService().GetMySummaryAsync(CompanyId, UserId, from, to, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("after", result.Error);
    }

    // ── Monthly ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyMonthly_ComputesNetPerMonth()
    {
        var (mine, other) = SeedAccounts();
        _movements.Seed(CompanyId, other, mine, MovementType.Transfer, 1000m,
            new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero));
        _movements.Seed(CompanyId, mine, other, MovementType.Transfer, 400m,
            new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero));
        _movements.Seed(CompanyId, other, mine, MovementType.Transfer, 2000m,
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));

        var result = await CreateService().GetMyMonthlyAsync(CompanyId, UserId, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);

        Assert.Equal(2026, result.Value[0].Year);
        Assert.Equal(1, result.Value[0].Month);
        Assert.Equal(1000m, result.Value[0].Incoming);
        Assert.Equal(400m, result.Value[0].Outgoing);
        Assert.Equal(600m, result.Value[0].Net);

        Assert.Equal(2, result.Value[1].Month);
        Assert.Equal(2000m, result.Value[1].Incoming);
        Assert.Equal(2000m, result.Value[1].Net);
    }
}
