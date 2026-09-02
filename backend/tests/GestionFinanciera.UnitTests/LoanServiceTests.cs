using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Loans;
using GestionFinanciera.Application.Features.Loans.DTOs;
using GestionFinanciera.Application.Features.Loans.Validators;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.UnitTests.Fakes;

namespace GestionFinanciera.UnitTests;

/// <summary>
/// Loan business rules: the bank treasury can never lend more than it holds
/// (it is also bound by the no-negative rule), disbursements are ledger
/// movements, and repayment is allowed full or partial.
/// </summary>
public sealed class LoanServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ClientUserId = Guid.NewGuid();
    private static readonly Guid AdminUserId = Guid.NewGuid();

    private readonly InMemoryLoanRepository _loans = new();
    private readonly InMemoryAccountRepository _accounts = new();
    private readonly InMemoryMovementRepository _movements = new();
    private readonly FakeAuditService _audit = new();
    private readonly FakeNotificationService _notifications = new();

    private LoanService CreateService() => new(
        _loans, _accounts, _movements, _audit, _notifications,
        new RequestLoanValidator(), new DecideLoanValidator(), new RepayLoanValidator());

    private (Guid Treasury, Guid Client) SeedAccounts(decimal treasuryBalance = 100_000m, decimal clientBalance = 1000m)
    {
        var treasury = _accounts.Seed(CompanyId, AdminUserId, "Treasury", treasuryBalance, isTreasury: true);
        var client = _accounts.Seed(CompanyId, ClientUserId, "XYZ SL", clientBalance);
        return (treasury.Id, client.Id);
    }

    // ── Request ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Request_Success_ReturnsPendingLoanAndAudits()
    {
        var (_, client) = SeedAccounts();
        var service = CreateService();

        var result = await service.RequestAsync(
            new RequestLoanDto(5000m, "Working capital"), CompanyId, ClientUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(LoanStatus.Pending, result.Value!.Status);
        Assert.Equal(client, result.Value.ClientAccountId);
        Assert.Single(_loans.Items);
        Assert.Contains(_audit.Entries, e => e.Entity == nameof(Loan) && e.Action == AuditAction.Create);
    }

    // ── Decide (approve) ─────────────────────────────────────────────────

    [Fact]
    public async Task Decide_Approve_DisbursesTreasuryToClient()
    {
        var (treasury, client) = SeedAccounts();
        var loan = _loans.Seed(CompanyId, client, 5000m);
        var service = CreateService();

        var result = await service.DecideAsync(
            loan.Id, new DecideLoanDto(true, "OK"), CompanyId, AdminUserId, "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(LoanStatus.Approved, result.Value!.Status);

        // A LoanDisbursement ledger row treasury → client was appended.
        var movement = Assert.Single(_movements.Items);
        Assert.Equal(MovementType.LoanDisbursement, movement.Type);
        Assert.Equal(treasury, movement.FromAccountId);
        Assert.Equal(client, movement.ToAccountId);
        Assert.Equal(5000m, movement.Amount);
    }

    [Fact]
    public async Task Decide_Reject_NoMovementCreated()
    {
        var (_, client) = SeedAccounts();
        var loan = _loans.Seed(CompanyId, client, 5000m);
        var service = CreateService();

        var result = await service.DecideAsync(
            loan.Id, new DecideLoanDto(false, "Not enough revenue"), CompanyId, AdminUserId, "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(LoanStatus.Rejected, result.Value!.Status);
        Assert.Empty(_movements.Items);
        Assert.DoesNotContain(_audit.Entries, e => e.Entity == nameof(Movement));
    }

    // ── The treasury limit ───────────────────────────────────────────────

    [Fact]
    public async Task Decide_ApproveBeyondTreasury_Fails()
    {
        var (_, client) = SeedAccounts(treasuryBalance: 1000m);
        var loan = _loans.Seed(CompanyId, client, 5000m);
        var service = CreateService();

        var result = await service.DecideAsync(
            loan.Id, new DecideLoanDto(true, null), CompanyId, AdminUserId, "Admin", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Conflict, result.Code);
        Assert.Contains("treasury", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_movements.Items); // the bank never lent what it does not have
        Assert.Equal(LoanStatus.Pending, loan.Status);
    }

    [Fact]
    public async Task Decide_ClientRole_Forbidden()
    {
        var (_, client) = SeedAccounts();
        var loan = _loans.Seed(CompanyId, client, 5000m);
        var service = CreateService();

        var result = await service.DecideAsync(
            loan.Id, new DecideLoanDto(true, null), CompanyId, ClientUserId, "User", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Forbidden, result.Code);
    }

    // ── Repay ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Repay_Partial_MovesClientToTreasuryAndKeepsApproved()
    {
        var (treasury, client) = SeedAccounts(treasuryBalance: 100_000m, clientBalance: 10_000m);
        var loan = _loans.Seed(CompanyId, client, 5000m, status: LoanStatus.Approved, repaid: 2000m);
        var service = CreateService();

        var result = await service.RepayAsync(
            loan.Id, new RepayLoanDto(1000m), CompanyId, ClientUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3000m, result.Value!.RepaidAmount);
        Assert.Equal(2000m, result.Value.OutstandingAmount);
        Assert.Equal(LoanStatus.Approved, result.Value.Status);

        var movement = Assert.Single(_movements.Items);
        Assert.Equal(MovementType.LoanRepayment, movement.Type);
        Assert.Equal(client, movement.FromAccountId);
        Assert.Equal(treasury, movement.ToAccountId);
    }

    [Fact]
    public async Task Repay_Full_MarksLoanRepaid()
    {
        var (_, client) = SeedAccounts(treasuryBalance: 100_000m, clientBalance: 10_000m);
        var loan = _loans.Seed(CompanyId, client, 5000m, status: LoanStatus.Approved, repaid: 2000m);
        var service = CreateService();

        var result = await service.RepayAsync(
            loan.Id, new RepayLoanDto(3000m), CompanyId, ClientUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5000m, result.Value!.RepaidAmount);
        Assert.Equal(0m, result.Value.OutstandingAmount);
        Assert.Equal(LoanStatus.Repaid, result.Value.Status);
    }

    [Fact]
    public async Task Repay_MoreThanOutstanding_Fails()
    {
        var (_, client) = SeedAccounts(treasuryBalance: 100_000m, clientBalance: 10_000m);
        var loan = _loans.Seed(CompanyId, client, 5000m, status: LoanStatus.Approved, repaid: 2000m);
        var service = CreateService();

        var result = await service.RepayAsync(
            loan.Id, new RepayLoanDto(4000m), CompanyId, ClientUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("outstanding", result.Error);
        Assert.Empty(_movements.Items);
    }
}
