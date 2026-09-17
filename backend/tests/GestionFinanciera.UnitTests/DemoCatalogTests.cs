using GestionFinanciera.Application.Features.Auth;

namespace GestionFinanciera.UnitTests;

public sealed class DemoCatalogTests
{
    [Fact]
    public void Accounts_HasBankOperatorAndTwoClients()
    {
        Assert.Equal(3, DemoCatalog.Accounts.Count);
        Assert.Equal(["admin", "ana", "xyz"], DemoCatalog.Accounts.Select(a => a.Key));
        Assert.Equal(["Admin", "User", "User"], DemoCatalog.Accounts.Select(a => a.Role));
        Assert.All(DemoCatalog.Accounts, a => Assert.Equal("Acme Demo Bank", a.ToDto().CompanyName));
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
        Assert.NotNull(DemoCatalog.Find("Ana"));
        Assert.NotNull(DemoCatalog.Find("XYZ"));
    }

    [Fact]
    public void Find_UnknownKey_ReturnsNull()
    {
        Assert.Null(DemoCatalog.Find("finance")); // the Finance role is gone
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

    [Fact]
    public void IsDemoAccountEmail_EverySeededEmail_ReturnsTrue()
    {
        // Drives the lockout exemption in AuthService: it must recognise every
        // seeded identity, otherwise an attacker can lock the demo buttons again.
        Assert.All(DemoCatalog.Accounts, a => Assert.True(DemoCatalog.IsDemoAccountEmail(a.Email)));
    }

    [Theory]
    [InlineData("DEMO.ADMIN@GESTFIN.LOCAL")]
    [InlineData("  demo.ana@gestfin.local  ")]
    public void IsDemoAccountEmail_IsCaseInsensitiveAndTrimmed(string email)
    {
        Assert.True(DemoCatalog.IsDemoAccountEmail(email));
    }

    [Theory]
    [InlineData("visitor@example.com")]
    [InlineData("demo.admin@gestfin.local.evil.com")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsDemoAccountEmail_VisitorOrEmpty_ReturnsFalse(string? email)
    {
        Assert.False(DemoCatalog.IsDemoAccountEmail(email));
    }
}
