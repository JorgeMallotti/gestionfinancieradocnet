using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Reports;
using GestionFinanciera.Application.Features.Reports.DTOs;
using GestionFinanciera.Application.Features.Reports.Validators;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.UnitTests.Fakes;

namespace GestionFinanciera.UnitTests;

public sealed class ReportServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CounterpartyUserId = Guid.NewGuid();

    private readonly InMemoryAccountRepository _accounts = new();
    private readonly InMemoryMovementRepository _movements = new();
    private readonly InMemoryCompanyRepository _companies = new();
    private readonly FakePdfService _pdf = new();
    private readonly FakeExcelService _excel = new();
    private readonly FakeEmailService _email = new();

    private ReportService CreateService() => new(
        _accounts, _movements, _companies, _pdf, _excel, _email, new EmailReportDtoValidator());

    private void SeedData()
    {
        _companies.Name = "Acme Demo Bank";
        var mine = _accounts.Seed(CompanyId, UserId, "Ana García", 10_000m);
        var other = _accounts.Seed(CompanyId, CounterpartyUserId, "XYZ Solutions SL", 20_000m);

        _movements.Seed(CompanyId, other.Id, mine.Id, MovementType.Transfer, 1000m,
            new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero));
        _movements.Seed(CompanyId, mine.Id, other.Id, MovementType.Transfer, 400m,
            new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero));
    }

    // ── PDF ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdf_Success_BuildsDataForOwnAccount()
    {
        SeedData();
        var service = CreateService();

        var result = await service.GeneratePdfAsync(CompanyId, UserId, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("%PDF-test", System.Text.Encoding.ASCII.GetString(result.Value!));
        Assert.Equal("Acme Demo Bank", _pdf.LastData!.CompanyName);
        Assert.Equal("Ana García", _pdf.LastData.AccountDisplayName);
        Assert.Equal(1000m, _pdf.LastData.TotalIncoming);
        Assert.Equal(400m, _pdf.LastData.TotalOutgoing);
        Assert.Equal(600m, _pdf.LastData.Balance);
        Assert.Equal(2, _pdf.LastData.Movements.Count);
    }

    [Fact]
    public async Task GeneratePdf_InvalidRange_Fails()
    {
        SeedData();
        var service = CreateService();
        var from = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await service.GeneratePdfAsync(CompanyId, UserId, from, to, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("after", result.Error);
    }

    [Fact]
    public async Task GeneratePdf_UnknownUser_Fails()
    {
        var service = CreateService();

        var result = await service.GeneratePdfAsync(CompanyId, Guid.NewGuid(), null, null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Code);
    }

    [Fact]
    public async Task GeneratePdf_RespectsDateRange()
    {
        SeedData();
        var mine = _accounts.Items.First(a => a.OwnerUserId == UserId);
        var other = _accounts.Items.First(a => a.OwnerUserId == CounterpartyUserId);
        _movements.Seed(CompanyId, other.Id, mine.Id, MovementType.Transfer, 500m,
            new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero));

        var service = CreateService();
        var from = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await service.GeneratePdfAsync(CompanyId, UserId, from, to, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _pdf.LastData!.Movements.Count);
        Assert.Equal(1000m, _pdf.LastData.TotalIncoming);
        Assert.Equal(400m, _pdf.LastData.TotalOutgoing);
    }

    [Fact]
    public async Task GeneratePdf_EmptyPeriod_StillReturnsPdf()
    {
        var service = CreateService();
        _accounts.Seed(CompanyId, UserId, "Ana García", 100m);

        var result = await service.GeneratePdfAsync(CompanyId, UserId, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_pdf.LastData!.Movements);
        Assert.Equal(0m, _pdf.LastData.TotalIncoming);
        Assert.Equal(0m, _pdf.LastData.TotalOutgoing);
    }

    // ── Excel ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateExcel_Success_DelegatesToExcelService()
    {
        SeedData();
        var service = CreateService();

        var result = await service.GenerateExcelAsync(CompanyId, UserId, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("PK-test", System.Text.Encoding.ASCII.GetString(result.Value!));
        Assert.Equal("Ana García", _excel.LastData!.AccountDisplayName);
        Assert.Equal(2, _excel.LastData.Movements.Count);
    }

    // ── Email ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendByEmail_Valid_SendsPdfAttachment()
    {
        SeedData();
        var service = CreateService();
        var dto = new EmailReportDto("jorge@test.local", null, null);

        var result = await service.SendByEmailAsync(dto, CompanyId, UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var sent = Assert.Single(_email.Sent);
        Assert.Equal("jorge@test.local", sent.ToEmail);
        Assert.Equal("Acme Demo Bank", sent.CompanyName);
        Assert.Equal("%PDF-test", System.Text.Encoding.ASCII.GetString(sent.PdfBytes));
    }

    [Fact]
    public async Task SendByEmail_InvalidEmail_Fails()
    {
        SeedData();
        var service = CreateService();
        var dto = new EmailReportDto("not-an-email", null, null);

        var result = await service.SendByEmailAsync(dto, CompanyId, UserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task SendByEmail_SmtpNotConfigured_PassesFailureThrough()
    {
        SeedData();
        _email.ShouldFail = true;
        var service = CreateService();
        var dto = new EmailReportDto("jorge@test.local", null, null);

        var result = await service.SendByEmailAsync(dto, CompanyId, UserId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("SMTP", result.Error);
    }
}
