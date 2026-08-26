using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Transactions;
using GestionFinanciera.Application.Features.Transactions.DTOs;
using GestionFinanciera.Application.Features.Transactions.Validators;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.UnitTests.Fakes;

namespace GestionFinanciera.UnitTests;

public sealed class TransactionServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    private readonly InMemoryCategoryRepository _categories = new();
    private readonly InMemoryTransactionRepository _repository = new();
    private readonly FakeAuditService _audit = new();

    private TransactionService CreateService() => new(
        _repository,
        _categories,
        _audit,
        new CreateTransactionValidator(),
        new UpdateTransactionValidator());

    private void SeedCategory(string name = "Sales")
    {
        _repository.CategoryNames[CategoryId] = name;
        _categories.Items.Add(new Category
        {
            Id = CategoryId,
            CompanyId = CompanyId,
            Name = name,
        });
    }

    private static CreateTransactionDto CreateDto(decimal amount = 100, TransactionType type = TransactionType.Income) =>
        new(CategoryId, type, amount, "EUR", new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero), "Client payment");

    private static UpdateTransactionDto UpdateDto(decimal amount = 100, TransactionType type = TransactionType.Income) =>
        new(CategoryId, type, amount, "EUR", new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero), "Client payment");

    // ── Create ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Success_ReturnsFullDtoAndAudits()
    {
        SeedCategory("Sales");
        var service = CreateService();

        var result = await service.CreateAsync(
            CreateDto(), CompanyId, UserId, "Finance", "192.168.1.10", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Sales", result.Value!.CategoryName);
        Assert.Equal(TransactionType.Income, result.Value.Type);
        Assert.Equal(100, result.Value.Amount);
        Assert.Equal(UserId, result.Value.CreatedByUserId);

        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditAction.Create, entry.Action);
        Assert.Equal(nameof(Transaction), entry.Entity);
        Assert.Equal("192.168.1.10", entry.IpAddress);
        Assert.NotNull(entry.AfterJson);
    }

    [Fact]
    public async Task Create_UnknownCategory_Fails()
    {
        var service = CreateService();

        var result = await service.CreateAsync(
            CreateDto(), CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Category", result.Error);
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Create_AmountZero_Fails()
    {
        SeedCategory();
        var service = CreateService();

        var result = await service.CreateAsync(
            CreateDto(amount: 0), CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("greater than zero", result.Error);
    }

    [Fact]
    public async Task Create_InvalidCurrency_Fails()
    {
        SeedCategory();
        var service = CreateService();

        var dto = CreateDto() with { Currency = "euros" };
        var result = await service.CreateAsync(
            dto, CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("ISO", result.Error);
    }

    [Fact]
    public async Task Create_UserRole_Forbidden()
    {
        SeedCategory();
        var service = CreateService();

        var result = await service.CreateAsync(
            CreateDto(), CompanyId, UserId, "User", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("permission", result.Error);
        Assert.Empty(_audit.Entries);
    }

    // ── Update ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Success_AuditsBeforeAndAfter()
    {
        SeedCategory("Sales");
        var service = CreateService();
        var created = await service.CreateAsync(CreateDto(100), CompanyId, UserId, "Admin", null, CancellationToken.None);

        var result = await service.UpdateAsync(
            created.Value!.Id,
            UpdateDto(amount: 250, type: TransactionType.Expense) with { Description = "Refund" },
            CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(250, result.Value!.Amount);
        Assert.Equal(TransactionType.Expense, result.Value.Type);
        Assert.Equal("Refund", result.Value.Description);

        var entry = Assert.Single(_audit.Entries, e => e.Action == AuditAction.Update);
        Assert.NotNull(entry.BeforeJson);
        Assert.NotNull(entry.AfterJson);
        Assert.Contains("\"amount\":100", entry.BeforeJson);
        Assert.Contains("\"amount\":250", entry.AfterJson);
    }

    [Fact]
    public async Task Update_NotFound_Fails()
    {
        SeedCategory();
        var service = CreateService();

        var result = await service.UpdateAsync(
            Guid.NewGuid(), UpdateDto(), CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error);
        Assert.Equal(ErrorCode.NotFound, result.Code);
    }

    [Fact]
    public async Task Update_MovedToUnknownCategory_Fails()
    {
        SeedCategory("Sales");
        var service = CreateService();
        var created = await service.CreateAsync(CreateDto(), CompanyId, UserId, "Admin", null, CancellationToken.None);

        var dto = UpdateDto() with { CategoryId = Guid.NewGuid() };
        var result = await service.UpdateAsync(
            created.Value!.Id, dto, CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Category", result.Error);
    }

    // ── Delete ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Success_Audits()
    {
        SeedCategory("Sales");
        var service = CreateService();
        var created = await service.CreateAsync(CreateDto(), CompanyId, UserId, "Admin", null, CancellationToken.None);

        var result = await service.DeleteAsync(
            created.Value!.Id, CompanyId, UserId, "Admin", "10.1.1.1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_repository.Items);

        var entry = Assert.Single(_audit.Entries, e => e.Action == AuditAction.Delete);
        Assert.NotNull(entry.BeforeJson);
        Assert.Null(entry.AfterJson);
        Assert.Equal("10.1.1.1", entry.IpAddress);
    }

    [Fact]
    public async Task Delete_NotFound_Fails()
    {
        var service = CreateService();

        var result = await service.DeleteAsync(
            Guid.NewGuid(), CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.NotFound, result.Code);
        Assert.Empty(_audit.Entries);
    }

    // ── Read ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCompany_FiltersByTypeAndCategory()
    {
        SeedCategory("Sales");
        var otherCategory = Guid.NewGuid();
        _repository.CategoryNames[otherCategory] = "Travel";
        _categories.Items.Add(new Category
        {
            Id = otherCategory,
            CompanyId = CompanyId,
            Name = "Travel",
        });

        var service = CreateService();
        await service.CreateAsync(CreateDto(100, TransactionType.Income), CompanyId, UserId, "Admin", null, CancellationToken.None);
        await service.CreateAsync(CreateDto(50, TransactionType.Expense), CompanyId, UserId, "Admin", null, CancellationToken.None);
        await service.CreateAsync(
            CreateDto(30, TransactionType.Income) with { CategoryId = otherCategory },
            CompanyId, UserId, "Admin", null, CancellationToken.None);

        var result = await service.GetByCompanyAsync(
            CompanyId, new TransactionQueryDto(Type: TransactionType.Income, CategoryId: CategoryId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.TotalCount);
        Assert.Equal(100, result.Value.Items[0].Amount);
    }
}
