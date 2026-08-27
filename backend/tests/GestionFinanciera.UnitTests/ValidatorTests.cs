using FluentValidation;

using GestionFinanciera.Application.Features.Auth.DTOs;
using GestionFinanciera.Application.Features.Auth.Validators;
using GestionFinanciera.Application.Features.Categories.DTOs;
using GestionFinanciera.Application.Features.Categories.Validators;
using GestionFinanciera.Application.Features.Transactions.DTOs;
using GestionFinanciera.Application.Features.Transactions.Validators;
using GestionFinanciera.Domain.Enums;

namespace GestionFinanciera.UnitTests;

public sealed class DemoLoginValidatorTests
{
    private readonly DemoLoginValidator _validator = new();

    [Fact]
    public async Task DemoLogin_ValidAccount_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(new DemoLoginDto("admin"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task DemoLogin_EmptyAccount_HasError()
    {
        var result = await _validator.ValidateAsync(new DemoLoginDto(" "));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Account");
    }

    [Fact]
    public async Task DemoLogin_TooLongAccount_HasError()
    {
        var result = await _validator.ValidateAsync(new DemoLoginDto(new string('a', 33)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Account");
    }
}

public sealed class CategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public async Task CreateCategory_Valid_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(new CreateCategoryDto("Travel", "Trips"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateCategory_EmptyName_HasError()
    {
        var result = await _validator.ValidateAsync(new CreateCategoryDto(" ", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateCategory_TooLongName_HasError()
    {
        var result = await _validator.ValidateAsync(new CreateCategoryDto(new string('a', 101), null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task CreateCategory_TooLongDescription_HasError()
    {
        var result = await _validator.ValidateAsync(new CreateCategoryDto("Travel", new string('a', 501)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }
}

public sealed class TransactionValidatorTests
{
    private readonly CreateTransactionValidator _validator = new();

    private static CreateTransactionDto ValidDto() =>
        new(Guid.NewGuid(), TransactionType.Income, 100, "EUR", DateTimeOffset.UtcNow, "Payment");

    [Fact]
    public async Task CreateTransaction_Valid_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(ValidDto());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CreateTransaction_ZeroAmount_HasError()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Amount = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task CreateTransaction_LowercaseCurrency_HasError()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Currency = "eur" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }

    [Fact]
    public async Task CreateTransaction_EmptyCategory_HasError()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { CategoryId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public async Task CreateTransaction_TooLongDescription_HasError()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Description = new string('a', 501) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }
}

public sealed class UpdateTransactionValidatorTests
{
    private readonly UpdateTransactionValidator _validator = new();

    [Fact]
    public async Task UpdateTransaction_Valid_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(new UpdateTransactionDto(
            Guid.NewGuid(), TransactionType.Expense, 50, "USD", DateTimeOffset.UtcNow, null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateTransaction_NegativeAmount_HasError()
    {
        var result = await _validator.ValidateAsync(new UpdateTransactionDto(
            Guid.NewGuid(), TransactionType.Expense, -5, "EUR", DateTimeOffset.UtcNow, null));

        Assert.False(result.IsValid);
    }
}
