using GestionFinanciera.Infrastructure.Identity;
using GestionFinanciera.Infrastructure.Services;

namespace GestionFinanciera.UnitTests;

public sealed class DemoResetServiceTests
{
    [Fact]
    public void GetInterval_DefaultOptions_Is24Hours()
    {
        var options = new DemoOptions();

        Assert.Equal(TimeSpan.FromHours(24), DemoResetService.GetInterval(options));
    }

    [Fact]
    public void GetInterval_ConfiguredValue_IsUsed()
    {
        var options = new DemoOptions { ResetIntervalHours = 6 };

        Assert.Equal(TimeSpan.FromHours(6), DemoResetService.GetInterval(options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void GetInterval_ClampsNonPositive_ToOneHour(int configured)
    {
        var options = new DemoOptions { ResetIntervalHours = configured };

        Assert.Equal(TimeSpan.FromHours(1), DemoResetService.GetInterval(options));
    }
}
