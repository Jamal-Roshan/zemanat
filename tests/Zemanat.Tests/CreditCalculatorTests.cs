using Zemanat.Core.Finance;

namespace Zemanat.Tests;

public class CreditCalculatorTests
{
    [Fact]
    public void CalculateCap_UsesPercentOfNetPay_RoundedDown()
    {
        // 302,415,004 x 70% = 211,690,502.8
        Assert.Equal(211_690_502L, CreditCalculator.CalculateCap(302_415_004, 70m));
    }

    [Fact]
    public void CalculateCap_SupportsAMonthsMultiplier()
    {
        // 302,415,004 x 70% x 2 = 423,381,005.6
        Assert.Equal(423_381_005L, CreditCalculator.CalculateCap(302_415_004, 70m, 2));
    }

    [Theory]
    [InlineData(0L, 70, 1)]
    [InlineData(100_000_000L, 0, 1)]
    [InlineData(100_000_000L, 70, 0)]
    [InlineData(-5L, 70, 1)]
    public void CalculateCap_ReturnsZeroForInvalidInputs(long net, int percent, int months)
    {
        Assert.Equal(0L, CreditCalculator.CalculateCap(net, percent, months));
    }

    [Fact]
    public void Assess_RequestInsideRemaining_IsNormal()
    {
        var result = CreditCalculator.Assess(100_000_000, 70m, 1, activeTotalRial: 20_000_000, requestedRial: 50_000_000);

        Assert.Equal(70_000_000L, result.Cap);
        Assert.Equal(50_000_000L, result.Remaining);
        Assert.Equal(CreditClass.Normal, result.Class);
        Assert.False(result.IsSurplus);
        Assert.Equal(0L, result.ExcessAmount);
    }

    [Fact]
    public void Assess_RequestAboveRemaining_IsSurplusWithExcess()
    {
        var result = CreditCalculator.Assess(100_000_000, 70m, 1, 20_000_000, 60_000_000);

        Assert.Equal(CreditClass.Surplus, result.Class);
        Assert.True(result.IsSurplus);
        Assert.Equal(10_000_000L, result.ExcessAmount);
    }

    [Fact]
    public void Assess_WhenActiveAlreadyAboveCap_WholeRequestIsExcess()
    {
        var result = CreditCalculator.Assess(100_000_000, 70m, 1, 80_000_000, 5_000_000);

        Assert.Equal(0L, result.Remaining);
        Assert.Equal(CreditClass.Surplus, result.Class);
        Assert.Equal(5_000_000L, result.ExcessAmount);
    }

    [Fact]
    public void Assess_WithZeroNetPay_EverythingIsSurplus()
    {
        var result = CreditCalculator.Assess(0, 70m, 1, 0, 1_000_000);

        Assert.Equal(0L, result.Cap);
        Assert.Equal(CreditClass.Surplus, result.Class);
        Assert.Equal(1_000_000L, result.ExcessAmount);
    }

    [Fact]
    public void Assess_RequestExactlyEqualToRemaining_IsNormal()
    {
        var result = CreditCalculator.Assess(100_000_000, 70m, 1, 20_000_000, 50_000_000);

        Assert.Equal(CreditClass.Normal, result.Class);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Assess_ThrowsForNonPositiveRequests(long requested)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreditCalculator.Assess(100_000_000, 70m, 1, 0, requested));
    }
}
