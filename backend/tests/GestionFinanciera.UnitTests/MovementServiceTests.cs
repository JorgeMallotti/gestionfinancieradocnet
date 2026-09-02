using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Movements;
using GestionFinanciera.Application.Features.Movements.DTOs;
using GestionFinanciera.Application.Features.Movements.Validators;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.UnitTests.Fakes;

namespace GestionFinanciera.UnitTests;

/// <summary>
/// Tests the CORE rule of the ledger: no account can ever go negative.
/// These are the rules a real bank lives and dies by — worth testing first.
/// </summary>
public sealed class MovementServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid PayerUserId = Guid.NewGuid();
    private static readonly Guid PayeeUserId = Guid.NewGuid();

    private readonly InMemoryAccountRepository _accounts = new();
    private readonly InMemoryMovementRepository _movements = new();
    private readonly InMemoryCategoryRepository _categories = new();
    private readonly FakeAuditService _audit = new();
    private readonly FakeNotificationService _notifications = new();

    private MovementService CreateService() => new(
        _movements, _accounts, _categories, _audit, _notifications, new TransferValidator());

    private (Guid Payer, Guid Payee) SeedAccounts(decimal payerBalance, decimal payeeBalance)
    {
        var payer = _accounts.Seed(CompanyId, PayerUserId, "Ana", payerBalance);
        var payee = _accounts.Seed(CompanyId, PayeeUserId, "XYZ SL", payeeBalance);
        return (payer.Id, payee.Id);
    }

    // ── Transfer success ─────────────────────────────────────────────────

    [Fact]
    public async Task Transfer_Success_MovesMoneyAndAudits()
    {
        var (payer, payee) = SeedAccounts(1000m, 500m);
        var service = CreateService();

        var result = await service.TransferAsync(
            new TransferDto(payee, 300m, null, "Web retainer"), CompanyId, PayerUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MovementType.Transfer, result.Value!.Type);
        Assert.Equal(payer, result.Value.FromAccountId);
        Assert.Equal(payee, result.Value.ToAccountId);
        Assert.Equal(300m, result.Value.Amount);
        Assert.Single(_movements.Items);

        // The audit trail records every ledger creation.
        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditAction.Create, entry.Action);
        Assert.Equal(nameof(Movement), entry.Entity);
    }

    [Fact]
    public async Task Transfer_ExactBalance_Succeeds()
    {
        var (payer, payee) = SeedAccounts(300m, 500m);
        var service = CreateService();

        // Spending the very last cent is allowed — going below is not.
        var result = await service.TransferAsync(
            new TransferDto(payee, 300m, null, null), CompanyId, PayerUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    // ── The golden rule: never negative ──────────────────────────────────

    [Fact]
    public async Task Transfer_MoreThanBalance_FailsInsufficientFunds()
    {
        var (payer, payee) = SeedAccounts(100m, 500m);
        var service = CreateService();

        var result = await service.TransferAsync(
            new TransferDto(payee, 101m, null, null), CompanyId, PayerUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Conflict, result.Code);
        Assert.Contains("Insufficient funds", result.Error);
        Assert.Empty(_movements.Items); // nothing was appended
        Assert.Empty(_audit.Entries);   // nothing was audited
    }

    // ── Validation & ownership ───────────────────────────────────────────

    [Fact]
    public async Task Transfer_UnknownPayer_FailsNotFound()
    {
        var (_, payee) = SeedAccounts(0m, 500m);
        var service = CreateService();

        var result = await service.TransferAsync(
            new TransferDto(payee, 10m, null, null), CompanyId, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Code);
    }

    [Fact]
    public async Task Transfer_ToOwnAccount_Fails()
    {
        var (payer, _) = SeedAccounts(1000m, 500m);
        var service = CreateService();

        var result = await service.TransferAsync(
            new TransferDto(payer, 10m, null, null), CompanyId, PayerUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("own account", result.Error);
    }

    [Fact]
    public async Task Transfer_UnknownPayee_FailsNotFound()
    {
        SeedAccounts(1000m, 500m);
        var service = CreateService();

        var result = await service.TransferAsync(
            new TransferDto(Guid.NewGuid(), 10m, null, null), CompanyId, PayerUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Code);
    }

    [Fact]
    public async Task Transfer_TreasuryCannotTransferDirectly_Fails()
    {
        var treasury = _accounts.Seed(CompanyId, Guid.NewGuid(), "Treasury", 1_000_000m, isTreasury: true);
        var payee = _accounts.Seed(CompanyId, PayeeUserId, "XYZ SL", 500m);
        var service = CreateService();

        var result = await service.TransferAsync(
            new TransferDto(payee.Id, 10m, null, null), CompanyId, treasury.OwnerUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Forbidden, result.Code);
    }

    [Fact]
    public async Task Transfer_PendingAccount_FailsForbidden()
    {
        var pending = _accounts.Seed(CompanyId, PayerUserId, "New Client", 0m,
            status: AccountStatus.Pending);
        var payee = _accounts.Seed(CompanyId, PayeeUserId, "XYZ SL", 500m);
        var service = CreateService();

        var result = await service.TransferAsync(
            new TransferDto(payee.Id, 10m, null, null), CompanyId, pending.OwnerUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Forbidden, result.Code);
    }
}
