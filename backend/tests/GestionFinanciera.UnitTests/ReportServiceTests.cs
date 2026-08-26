using GestionFinanciera.Application.Features.Reports;
using GestionFinanciera.Application.Features.Reports.DTOs;
using GestionFinanciera.Application.Features.Reports.Validators;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.UnitTests.Fakes;

namespace GestionFinanciera.UnitTests;

public sealed class ReportServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid SalesCategoryId = Guid.NewGuid();
    private static readonly Guid TravelCategoryId = Guid.NewGuid();

    private readonly InMemoryTransactionRepository _transactions = new();
    private readonly InMemoryCompanyRepository _companies = new();
    private readonly FakePdfService _pdf = new();
    private readonly FakeExcelService _excel = new();
    private readonly FakeEmailService _email = new();

    private ReportService CreateService() => new(
        _transactions, _companies, _pdf, _excel, _email, new EmailReportDtoValidator());

    private void SeedData()
    {
        _companies.Name = "Acme S.L.";
        _transactions.CategoryNames[SalesCategoryId] = "Sales";
        _transactions.CategoryNames[TravelCategoryId] = "Travel";

        _transactions.Items.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            CategoryId = SalesCategoryId,
            Type = TransactionType.Income,
            Amount = 1000,
            Currency = "EUR",
            Date = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero),
        });
        _transactions.Items.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            CategoryId = TravelCategoryId,
            Type = TransactionType.Expense,
            Amount = 400,
            Currency = "EUR",
            Date = new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero),
        });
    }

    // ── PDF ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdf_Success_BuildsDataWithCompanyAndTotals()
    {
        SeedData();
        var service = CreateService();

        var result = await service.GeneratePdfAsync(CompanyId, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("%PDF-test", System.Text.Encoding.ASCII.GetString(result.Value!));
        Assert.Equal("Acme S.L.", _pdf.LastData!.CompanyName);
        Assert.Equal(1000, _pdf.LastData.TotalIncome);
        Assert.Equal(400, _pdf.LastData.TotalExpenses);
        Assert.Equal(600, _pdf.LastData.Balance);
        Assert.Equal(2, _pdf.LastData.Transactions.Count);
    }

    [Fact]
    public async Task GeneratePdf_InvalidRange_Fails()
    {
        SeedData();
        var service = CreateService();
        var from = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await service.GeneratePdfAsync(CompanyId, from, to, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("after", result.Error);
    }

    // ── Excel ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateExcel_Success_DelegatesToExcelService()
    {
        SeedData();
        var service = CreateService();

        var result = await service.GenerateExcelAsync(CompanyId, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("PK-test", System.Text.Encoding.ASCII.GetString(result.Value!));
        Assert.Equal("Acme S.L.", _excel.LastData!.CompanyName);
        Assert.Equal(2, _excel.LastData.Transactions.Count);
    }

    // ── Email ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendByEmail_Valid_SendsPdfAttachment()
    {
        SeedData();
        var service = CreateService();
        var dto = new EmailReportDto("jorge@test.local", null, null);

        var result = await service.SendByEmailAsync(dto, CompanyId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var sent = Assert.Single(_email.Sent);
        Assert.Equal("jorge@test.local", sent.ToEmail);
        Assert.Equal("Acme S.L.", sent.CompanyName);
        Assert.Equal("%PDF-test", System.Text.Encoding.ASCII.GetString(sent.PdfBytes));
    }

    [Fact]
    public async Task SendByEmail_InvalidEmail_Fails()
    {
        SeedData();
        var service = CreateService();
        var dto = new EmailReportDto("not-an-email", null, null);

        var result = await service.SendByEmailAsync(dto, CompanyId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_email.Sent);
    }

    [Fact]
    public async Task SendByEmail_InvalidRange_Fails()
    {
        SeedData();
        var service = CreateService();
        var dto = new EmailReportDto(
            "jorge@test.local",
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var result = await service.SendByEmailAsync(dto, CompanyId, CancellationToken.None);

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

        var result = await service.SendByEmailAsync(dto, CompanyId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("SMTP", result.Error);
    }

    // ── Filtros por rango ────────────────────────────────────────────────

    [Fact]
    public async Task GeneratePdf_RespectsDateRange()
    {
        SeedData();
        _transactions.Items.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            CategoryId = SalesCategoryId,
            Type = TransactionType.Income,
            Amount = 500,
            Currency = "EUR",
            Date = new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero),
        });
        var service = CreateService();
        var from = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero);

        var result = await service.GeneratePdfAsync(CompanyId, from, to, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, _pdf.LastData!.Transactions.Count);
        Assert.Equal(1000, _pdf.LastData.TotalIncome);
        Assert.Equal(400, _pdf.LastData.TotalExpenses);
    }

    [Fact]
    public async Task GeneratePdf_EmptyPeriod_StillReturnsPdf()
    {
        var service = CreateService();

        var result = await service.GeneratePdfAsync(CompanyId, null, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_pdf.LastData!.Transactions);
        Assert.Equal(0, _pdf.LastData.TotalIncome);
        Assert.Equal(0, _pdf.LastData.TotalExpenses);
    }
}

public sealed class EmailReportDtoValidatorTests
{
    private readonly EmailReportDtoValidator _validator = new();

    [Fact]
    public async Task Valid_Email_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(new EmailReportDto("jorge@test.local", null, null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Invalid_Email_HasError()
    {
        var result = await _validator.ValidateAsync(new EmailReportDto("nope", null, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Empty_Email_HasError()
    {
        var result = await _validator.ValidateAsync(new EmailReportDto("", null, null));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task From_After_To_HasError()
    {
        var result = await _validator.ValidateAsync(new EmailReportDto(
            "jorge@test.local",
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "From");
    }
}
