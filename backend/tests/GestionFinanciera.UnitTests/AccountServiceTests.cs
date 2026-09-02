using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Accounts;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.UnitTests.Fakes;

namespace GestionFinanciera.UnitTests;

/// <summary>
/// Account business rules: the caller only ever sees their own account; only
/// the bank Admin can list, approve and suspend clients; the treasury is never
/// a "client".
/// </summary>
public sealed class AccountServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid AdminUserId = Guid.NewGuid();
    private static readonly Guid ClientUserId = Guid.NewGuid();

    private readonly InMemoryAccountRepository _accounts = new();
    private readonly FakeAuditService _audit = new();
    private readonly FakeNotificationService _notifications = new();

    private AccountService CreateService() => new(_accounts, _audit, _notifications);

    // ── Get my account ───────────────────────────────────────────────────

    [Fact]
    public async Task GetMyAccount_Success()
    {
        var mine = _accounts.Seed(CompanyId, ClientUserId, "Ana", 500m);
        var service = CreateService();

        var result = await service.GetMyAccountAsync(CompanyId, ClientUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(mine.Id, result.Value!.Id);
        Assert.Equal("Ana", result.Value.DisplayName);
    }

    [Fact]
    public async Task GetMyAccount_UnknownUser_NotFound()
    {
        var service = CreateService();

        var result = await service.GetMyAccountAsync(CompanyId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Code);
    }

    // ── Counterparties ───────────────────────────────────────────────────

    [Fact]
    public async Task GetCounterparties_ExcludesSelfAndTreasuryIncluded()
    {
        var mine = _accounts.Seed(CompanyId, ClientUserId, "Ana", 500m);
        var treasury = _accounts.Seed(CompanyId, AdminUserId, "Treasury", 1_000_000m, isTreasury: true);
        var other = _accounts.Seed(CompanyId, Guid.NewGuid(), "XYZ SL", 10_000m);
        var pending = _accounts.Seed(CompanyId, Guid.NewGuid(), "Pending Client", 0m,
            status: AccountStatus.Pending);
        var service = CreateService();

        var result = await service.GetCounterpartiesAsync(CompanyId, ClientUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var ids = result.Value!.Select(a => a.Id).ToList();
        Assert.Contains(other.Id, ids);
        Assert.Contains(treasury.Id, ids);      // clients may pay the bank
        Assert.DoesNotContain(mine.Id, ids);    // never yourself
        Assert.DoesNotContain(pending.Id, ids); // only active counterparties
    }

    // ── List clients (Admin) ─────────────────────────────────────────────

    [Fact]
    public async Task ListClients_Admin_ExcludesTreasury()
    {
        _accounts.Seed(CompanyId, AdminUserId, "Treasury", 1_000_000m, isTreasury: true);
        _accounts.Seed(CompanyId, ClientUserId, "Ana", 500m);
        _accounts.Seed(CompanyId, Guid.NewGuid(), "XYZ SL", 10_000m);
        var service = CreateService();

        var result = await service.ListClientsAsync(CompanyId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.DoesNotContain(result.Value, a => a.IsTreasury);
    }

    [Fact]
    public async Task ListClients_FilterPending()
    {
        _accounts.Seed(CompanyId, Guid.NewGuid(), "Pending A", 0m, status: AccountStatus.Pending);
        _accounts.Seed(CompanyId, Guid.NewGuid(), "Active B", 500m);
        var service = CreateService();

        var result = await service.ListClientsAsync(CompanyId, "Admin", AccountStatus.Pending, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var pending = Assert.Single(result.Value!);
        Assert.Equal("Pending A", pending.DisplayName);
    }

    [Fact]
    public async Task ListClients_ClientRole_Forbidden()
    {
        var service = CreateService();

        var result = await service.ListClientsAsync(CompanyId, "User", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Forbidden, result.Code);
    }

    // ── Approve / Suspend (Admin) ────────────────────────────────────────

    [Fact]
    public async Task Approve_PendingClient_SetsActiveAndAudits()
    {
        var client = _accounts.Seed(CompanyId, ClientUserId, "Ana", 0m, status: AccountStatus.Pending);
        var service = CreateService();

        var result = await service.ApproveClientAsync(
            client.Id, CompanyId, AdminUserId, "Admin", "127.0.0.1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Active, result.Value!.Status);
        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditAction.Update, entry.Action);
        Assert.Equal(nameof(ClientAccount), entry.Entity);
        Assert.Equal("127.0.0.1", entry.IpAddress);
    }

    [Fact]
    public async Task Approve_ActiveClient_FailsConflict()
    {
        var client = _accounts.Seed(CompanyId, ClientUserId, "Ana", 500m, status: AccountStatus.Active);
        var service = CreateService();

        var result = await service.ApproveClientAsync(
            client.Id, CompanyId, AdminUserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Conflict, result.Code);
    }

    [Fact]
    public async Task Approve_Treasury_NotFound()
    {
        var treasury = _accounts.Seed(CompanyId, AdminUserId, "Treasury", 1_000_000m, isTreasury: true);
        var service = CreateService();

        var result = await service.ApproveClientAsync(
            treasury.Id, CompanyId, AdminUserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Code); // never approve the bank itself
    }

    [Fact]
    public async Task Approve_ClientRole_Forbidden()
    {
        var client = _accounts.Seed(CompanyId, ClientUserId, "Ana", 0m, status: AccountStatus.Pending);
        var service = CreateService();

        var result = await service.ApproveClientAsync(
            client.Id, CompanyId, ClientUserId, "User", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Forbidden, result.Code);
    }

    [Fact]
    public async Task Suspend_ActiveClient_SetsSuspended()
    {
        var client = _accounts.Seed(CompanyId, ClientUserId, "Ana", 500m, status: AccountStatus.Active);
        var service = CreateService();

        var result = await service.SuspendClientAsync(
            client.Id, CompanyId, AdminUserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Suspended, result.Value!.Status);
    }
}
