using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Claims;
using GestionFinanciera.Application.Features.Claims.DTOs;
using GestionFinanciera.Application.Features.Claims.Validators;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.UnitTests.Fakes;

namespace GestionFinanciera.UnitTests;

/// <summary>
/// Claim (mediation) rules: only an involved party can open a claim; the Admin
/// proposes a corrective transfer; the corrective movement is executed ONLY
/// when BOTH parties consented — and it is STACKED (never overwrites).
/// </summary>
public sealed class ClaimServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid AnaUserId = Guid.NewGuid();
    private static readonly Guid XyzUserId = Guid.NewGuid();
    private static readonly Guid AdminUserId = Guid.NewGuid();

    private readonly InMemoryClaimRepository _claims = new();
    private readonly InMemoryMovementRepository _movements = new();
    private readonly InMemoryAccountRepository _accounts = new();
    private readonly FakeAuditService _audit = new();
    private readonly FakeNotificationService _notifications = new();

    private ClaimService CreateService() => new(
        _claims, _movements, _accounts, _audit, _notifications,
        new OpenClaimValidator(), new ProposeCorrectionValidator());

    private (Guid Ana, Guid Xyz, Guid DisputedMovement) SeedDisputedTransfer()
    {
        var ana = _accounts.Seed(CompanyId, AnaUserId, "Ana", 1000m);
        var xyz = _accounts.Seed(CompanyId, XyzUserId, "XYZ SL", 10_000m);
        var treasury = _accounts.Seed(CompanyId, AdminUserId, "Treasury", 1_000_000m, isTreasury: true);

        // Ana paid XYZ 1,200 (the disputed movement).
        var movement = _movements.Seed(CompanyId, ana.Id, xyz.Id, MovementType.Transfer, 1200m);
        return (ana.Id, xyz.Id, movement.Id);
    }

    // ── Open ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Open_Success_ByInvolvedParty_Audits()
    {
        var (ana, _, disputed) = SeedDisputedTransfer();
        var service = CreateService();

        var result = await service.OpenAsync(
            new OpenClaimDto(disputed, "I overpaid by 200"), CompanyId, AnaUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClaimStatus.Open, result.Value!.Status);
        Assert.Equal(ana, result.Value.ClaimantAccountId);
        Assert.Contains(_audit.Entries, e => e.Entity == nameof(Claim) && e.Action == AuditAction.Create);
    }

    [Fact]
    public async Task Open_ByUninvolvedParty_Forbidden()
    {
        var (_, _, disputed) = SeedDisputedTransfer();
        var stranger = _accounts.Seed(CompanyId, Guid.NewGuid(), "Stranger", 500m);
        var service = CreateService();

        var result = await service.OpenAsync(
            new OpenClaimDto(disputed, "This is not mine"), CompanyId, stranger.OwnerUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.Forbidden, result.Code);
    }

    [Fact]
    public async Task Open_UnknownMovement_FailsNotFound()
    {
        SeedDisputedTransfer();
        var service = CreateService();

        var result = await service.OpenAsync(
            new OpenClaimDto(Guid.NewGuid(), "Whatever"), CompanyId, AnaUserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Code);
    }

    // ── Propose (Admin mediates) ─────────────────────────────────────────

    [Fact]
    public async Task Propose_Admin_SetsUnderReviewWithProposal()
    {
        var (ana, xyz, disputed) = SeedDisputedTransfer();
        var claim = _claims.Seed(CompanyId, disputed, ana, ClaimStatus.Open);
        var service = CreateService();

        // XYZ must return 200 to Ana.
        var result = await service.ProposeAsync(
            claim.Id, new ProposeCorrectionDto(200m, xyz, ana, "Refund the overpayment"),
            CompanyId, AdminUserId, "Admin", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClaimStatus.UnderReview, result.Value!.Status);
        Assert.Equal(200m, result.Value.ProposedAmount);
        Assert.False(result.Value.PayerConsented);
        Assert.False(result.Value.PayeeConsented);
    }

    [Fact]
    public async Task Propose_CorrectionOutsideParties_Fails()
    {
        var (ana, _, disputed) = SeedDisputedTransfer();
        var claim = _claims.Seed(CompanyId, disputed, ana, ClaimStatus.Open);
        var stranger = _accounts.Seed(CompanyId, Guid.NewGuid(), "Stranger", 500m);
        var service = CreateService();

        var result = await service.ProposeAsync(
            claim.Id, new ProposeCorrectionDto(50m, stranger.Id, ana, "?"),
            CompanyId, AdminUserId, "Admin", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("original movement", result.Error);
    }

    // ── Consent (both parties must approve) ──────────────────────────────

    [Fact]
    public async Task Consent_FirstPartyOnly_DoesNotExecuteYet()
    {
        var (ana, xyz, disputed) = SeedDisputedTransfer();
        var claim = _claims.Seed(CompanyId, disputed, ana, ClaimStatus.UnderReview);
        claim.ProposedAmount = 200m;
        claim.CorrectiveFromAccountId = xyz; // XYZ returns the money
        claim.CorrectiveToAccountId = ana;
        var service = CreateService();

        var result = await service.ConsentAsync(claim.Id, true, CompanyId, XyzUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.PayerConsented);
        Assert.False(result.Value.PayeeConsented);
        Assert.Equal(ClaimStatus.UnderReview, result.Value.Status);
        Assert.DoesNotContain(_movements.Items, m => m.Type == MovementType.CorrectiveTransfer); // nothing moved yet
    }

    [Fact]
    public async Task Consent_BothParties_ExecutesCorrectiveMovement()
    {
        var (ana, xyz, disputed) = SeedDisputedTransfer();
        var claim = _claims.Seed(CompanyId, disputed, ana, ClaimStatus.UnderReview);
        claim.ProposedAmount = 200m;
        claim.CorrectiveFromAccountId = xyz;
        claim.CorrectiveToAccountId = ana;
        var service = CreateService();

        // 1st consent: XYZ (the payer in the correction).
        var first = await service.ConsentAsync(claim.Id, true, CompanyId, XyzUserId, CancellationToken.None);
        Assert.Equal(ClaimStatus.UnderReview, first.Value!.Status);

        // 2nd consent: Ana (the payee) → executes.
        var second = await service.ConsentAsync(claim.Id, true, CompanyId, AnaUserId, CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal(ClaimStatus.Resolved, second.Value!.Status);

        // A corrective movement was STACKED, referencing the original.
        var corrective = Assert.Single(_movements.Items, m => m.Type == MovementType.CorrectiveTransfer);
        Assert.Equal(xyz, corrective.FromAccountId);
        Assert.Equal(ana, corrective.ToAccountId);
        Assert.Equal(200m, corrective.Amount);
        Assert.Equal(disputed, corrective.CorrectsMovementId); // links back — never overwrites
        Assert.Equal(corrective.Id, second.Value.ResolutionMovementId);
    }

    [Fact]
    public async Task Consent_Refusal_RejectsClaimWithoutMovingMoney()
    {
        var (ana, xyz, disputed) = SeedDisputedTransfer();
        var claim = _claims.Seed(CompanyId, disputed, ana, ClaimStatus.UnderReview);
        claim.ProposedAmount = 200m;
        claim.CorrectiveFromAccountId = xyz;
        claim.CorrectiveToAccountId = ana;
        var service = CreateService();

        var result = await service.ConsentAsync(claim.Id, false, CompanyId, XyzUserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ClaimStatus.Rejected, result.Value!.Status);
        Assert.DoesNotContain(_movements.Items, m => m.Type == MovementType.CorrectiveTransfer);
    }
}
