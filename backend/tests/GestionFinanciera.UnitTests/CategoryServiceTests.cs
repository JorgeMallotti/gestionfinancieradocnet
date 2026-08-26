using GestionFinanciera.Application.Common.Pagination;
using GestionFinanciera.Application.Common.Results;
using GestionFinanciera.Application.Features.Categories;
using GestionFinanciera.Application.Features.Categories.DTOs;
using GestionFinanciera.Application.Features.Categories.Validators;
using GestionFinanciera.Domain.Entities;
using GestionFinanciera.Domain.Enums;
using GestionFinanciera.UnitTests.Fakes;

namespace GestionFinanciera.UnitTests;

public sealed class CategoryServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly InMemoryCategoryRepository _repository = new();
    private readonly InMemoryTransactionRepository _transactions = new();
    private readonly FakeAuditService _audit = new();

    private CategoryService CreateService() => new(
        _repository,
        _transactions,
        _audit,
        new CreateCategoryValidator(),
        new UpdateCategoryValidator());

    private static Category SeedCategory(
        InMemoryCategoryRepository repository, string name, bool isDefault = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            Name = name,
            IsDefault = isDefault,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    // ── Create ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Success_ReturnsDtoAndAudits()
    {
        var service = CreateService();
        var dto = new CreateCategoryDto("Travel", "Business trips");

        var result = await service.CreateAsync(dto, CompanyId, UserId, "Admin", "127.0.0.1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Travel", result.Value!.Name);
        Assert.Single(_audit.Entries);
        Assert.Equal(AuditAction.Create, _audit.Entries[0].Action);
        Assert.Equal(nameof(Category), _audit.Entries[0].Entity);
        Assert.Equal("127.0.0.1", _audit.Entries[0].IpAddress);
        Assert.NotNull(_audit.Entries[0].AfterJson);
    }

    [Fact]
    public async Task Create_DuplicateName_Fails()
    {
        _repository.Items.Add(SeedCategory(_repository, "Marketing"));
        var service = CreateService();

        var result = await service.CreateAsync(
            new CreateCategoryDto("marketing", null), CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("already exists", result.Error);
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Create_UserRole_Forbidden()
    {
        var service = CreateService();

        var result = await service.CreateAsync(
            new CreateCategoryDto("Travel", null), CompanyId, UserId, "User", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("permission", result.Error);
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Create_InvalidName_Fails()
    {
        var service = CreateService();

        var result = await service.CreateAsync(
            new CreateCategoryDto("", null), CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(string.IsNullOrEmpty(result.Error));
    }

    // ── Update ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Renames_SuccessAndAudits()
    {
        var category = SeedCategory(_repository, "Marketing");
        _repository.Items.Add(category);
        var service = CreateService();

        var result = await service.UpdateAsync(
            category.Id, new UpdateCategoryDto("Branding", "Updated"), CompanyId, UserId, "Finance", null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Branding", result.Value!.Name);
        Assert.Equal("Updated", result.Value.Description);

        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditAction.Update, entry.Action);
        Assert.NotNull(entry.BeforeJson);
        Assert.NotNull(entry.AfterJson);
        Assert.Contains("Marketing", entry.BeforeJson);
        Assert.Contains("Branding", entry.AfterJson);
    }

    [Fact]
    public async Task Update_ToExistingName_Fails()
    {
        _repository.Items.Add(SeedCategory(_repository, "Marketing"));
        var sales = SeedCategory(_repository, "Sales");
        _repository.Items.Add(sales);
        var service = CreateService();

        var result = await service.UpdateAsync(
            sales.Id, new UpdateCategoryDto("Marketing", null), CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public async Task Update_NotFound_Fails()
    {
        var service = CreateService();

        var result = await service.UpdateAsync(
            Guid.NewGuid(), new UpdateCategoryDto("X", null), CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error);
        Assert.Equal(ErrorCode.NotFound, result.Code);
    }

    // ── Delete ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_DefaultCategory_Fails()
    {
        var category = SeedCategory(_repository, "Marketing", isDefault: true);
        _repository.Items.Add(category);
        var service = CreateService();

        var result = await service.DeleteAsync(category.Id, CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Default", result.Error);
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Delete_CategoryWithTransactions_Fails()
    {
        var category = SeedCategory(_repository, "Travel");
        _repository.Items.Add(category);
        _transactions.Items.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            CategoryId = category.Id,
            Amount = 100,
            Date = DateTimeOffset.UtcNow,
        });
        var service = CreateService();

        var result = await service.DeleteAsync(category.Id, CompanyId, UserId, "Admin", null, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("transactions", result.Error);
        Assert.Empty(_audit.Entries);
    }

    [Fact]
    public async Task Delete_Success_AuditsBeforeState()
    {
        var category = SeedCategory(_repository, "Travel");
        _repository.Items.Add(category);
        var service = CreateService();

        var result = await service.DeleteAsync(category.Id, CompanyId, UserId, "Admin", "10.0.0.5", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_repository.Items);

        var entry = Assert.Single(_audit.Entries);
        Assert.Equal(AuditAction.Delete, entry.Action);
        Assert.NotNull(entry.BeforeJson);
        Assert.Contains("Travel", entry.BeforeJson);
        Assert.Null(entry.AfterJson);
        Assert.Equal("10.0.0.5", entry.IpAddress);
    }

    // ── Read ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCompany_PaginatesAndScopesByCompany()
    {
        var otherCompany = Guid.NewGuid();
        _repository.Items.Add(SeedCategory(_repository, "Alpha"));
        _repository.Items.Add(SeedCategory(_repository, "Beta"));
        _repository.Items.Add(new Category
        {
            Id = Guid.NewGuid(),
            CompanyId = otherCompany,
            Name = "Other",
        });
        var service = CreateService();

        var result = await service.GetByCompanyAsync(CompanyId, new PaginationQuery(1, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.DoesNotContain(result.Value.Items, c => c.Name == "Other");
    }

    [Fact]
    public async Task GetById_NotFound_Fails()
    {
        var service = CreateService();

        var result = await service.GetByIdAsync(Guid.NewGuid(), CompanyId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error);
        Assert.Equal(ErrorCode.NotFound, result.Code);
    }
}
