using FluentValidation;

using GestionFinanciera.Application.Features.Auth.DTOs;
using GestionFinanciera.Application.Features.Auth.Validators;
using GestionFinanciera.Application.Features.Categories.DTOs;
using GestionFinanciera.Application.Features.Categories.Validators;
using GestionFinanciera.Application.Features.Claims.DTOs;
using GestionFinanciera.Application.Features.Claims.Validators;
using GestionFinanciera.Application.Features.Loans.DTOs;
using GestionFinanciera.Application.Features.Loans.Validators;
using GestionFinanciera.Application.Features.Movements.DTOs;
using GestionFinanciera.Application.Features.Movements.Validators;

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

public sealed class TransferValidatorTests
{
    private readonly TransferValidator _validator = new();

    private static TransferDto ValidDto() =>
        new(Guid.NewGuid(), 100m, Guid.NewGuid(), "Payment");

    [Fact]
    public async Task Transfer_Valid_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(ValidDto());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Transfer_ZeroAmount_HasError()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Amount = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task Transfer_NegativeAmount_HasError()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Amount = -5 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task Transfer_EmptyReceiver_HasError()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { ToAccountId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ToAccountId");
    }

    [Fact]
    public async Task Transfer_TooLongDescription_HasError()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Description = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }
}

public sealed class RequestLoanValidatorTests
{
    private readonly RequestLoanValidator _validator = new();

    [Fact]
    public async Task RequestLoan_Valid_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(new RequestLoanDto(5000m, "Working capital"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RequestLoan_ZeroAmount_HasError()
    {
        var result = await _validator.ValidateAsync(new RequestLoanDto(0m, "Working capital"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task RequestLoan_EmptyReason_HasError()
    {
        var result = await _validator.ValidateAsync(new RequestLoanDto(100m, " "));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Reason");
    }
}

public sealed class RepayLoanValidatorTests
{
    private readonly RepayLoanValidator _validator = new();

    [Fact]
    public async Task RepayLoan_Valid_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(new RepayLoanDto(100m));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RepayLoan_NegativeAmount_HasError()
    {
        var result = await _validator.ValidateAsync(new RepayLoanDto(-5m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }
}

public sealed class OpenClaimValidatorTests
{
    private readonly OpenClaimValidator _validator = new();

    [Fact]
    public async Task OpenClaim_Valid_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(new OpenClaimDto(Guid.NewGuid(), "Wrong amount"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task OpenClaim_EmptyReason_HasError()
    {
        var result = await _validator.ValidateAsync(new OpenClaimDto(Guid.NewGuid(), " "));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Reason");
    }
}

public sealed class ProposeCorrectionValidatorTests
{
    private readonly ProposeCorrectionValidator _validator = new();

    private static ProposeCorrectionDto ValidDto() =>
        new(200m, Guid.NewGuid(), Guid.NewGuid(), "Refund");

    [Fact]
    public async Task Propose_Valid_HasNoErrors()
    {
        var result = await _validator.ValidateAsync(ValidDto());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Propose_ZeroAmount_HasError()
    {
        var result = await _validator.ValidateAsync(ValidDto() with { Amount = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task Propose_SameAccounts_HasError()
    {
        var account = Guid.NewGuid();
        var result = await _validator.ValidateAsync(new ProposeCorrectionDto(100m, account, account, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ToAccountId");
    }
}
