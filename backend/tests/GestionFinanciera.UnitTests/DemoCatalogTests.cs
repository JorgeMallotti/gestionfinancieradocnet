using GestionFinanciera.Application.Features.Auth;

namespace GestionFinanciera.UnitTests;

public sealed class DemoCatalogTests
{
    [Fact]
    public void Accounts_HasThreeRoles_AdminFinanceUser()
    {
        Assert.Equal(3, DemoCatalog.Accounts.Count);
        Assert.Equal(["admin", "finance", "user"], DemoCatalog.Accounts.Select(a => a.Key));
        Assert.Equal(["Admin", "Finance", "User"], DemoCatalog.Accounts.Select(a => a.Role));
    }

    [Fact]
    public void Accounts_EmailsAndKeys_AreUnique()
    {
        Assert.Equal(DemoCatalog.Accounts.Count, DemoCatalog.Accounts.Select(a => a.Email).Distinct().Count());
        Assert.Equal(DemoCatalog.Accounts.Count, DemoCatalog.Accounts.Select(a => a.Key).Distinct().Count());
    }

    [Fact]
    public void Find_ExistingKey_ReturnsAccount()
    {
        var account = DemoCatalog.Find("admin");

        Assert.NotNull(account);
        Assert.Equal("demo.admin@gestfin.local", account!.Email);
        Assert.Equal("Admin", account.Role);
    }

    [Fact]
    public void Find_IsCaseInsensitive()
    {
        Assert.NotNull(DemoCatalog.Find("ADMIN"));
        Assert.NotNull(DemoCatalog.Find("Finance"));
    }

    [Fact]
    public void Find_UnknownKey_ReturnsNull()
    {
        Assert.Null(DemoCatalog.Find("nope"));
    }

    [Fact]
    public void ToDtos_ExposesOnlyPublicMetadata()
    {
        var dtos = DemoCatalog.ToDtos();

        Assert.Equal(3, dtos.Count);
        Assert.All(dtos, d => Assert.False(string.IsNullOrWhiteSpace(d.Key)));
        Assert.All(dtos, d => Assert.False(string.IsNullOrWhiteSpace(d.Role)));
        Assert.All(dtos, d => Assert.Equal(DemoCatalog.CompanyName, d.CompanyName));
    }
}
