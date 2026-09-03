// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Services;

namespace Adwais.Tests.Services;

public class FinancialMathTests
{
    [Theory]
    [InlineData(100, 50, 100)]
    [InlineData(50, 100, -50)]
    [InlineData(0, 0, 0)]
    [InlineData(100, 0, 0)]
    [InlineData(4, 3, 33.33)]
    [InlineData(400, 216.67, 84.61)]
    public void CalculateGrowthPercentage_ReturnsRoundedChange(decimal current, decimal previous, decimal expected)
    {
        Assert.Equal(expected, FinancialMath.CalculateGrowthPercentage(current, previous));
    }

    [Theory]
    [InlineData(new double[] { }, 0)]
    [InlineData(new double[] { 5 }, 5)]
    [InlineData(new double[] { 1, 2, 3 }, 2)]
    [InlineData(new double[] { 1, 2, 3, 4 }, 2.5)]
    public void CalculateMedian_ReturnsMiddleOfSortedInput(double[] values, double expected)
    {
        Assert.Equal((decimal)expected, FinancialMath.CalculateMedian(values.Select(v => (decimal)v).ToList()));
    }

    [Theory]
    [InlineData(new double[] { }, 10, 50)]
    [InlineData(new double[] { 7 }, 7, 50)]
    [InlineData(new double[] { 100, 1000 }, 100, 25)]
    [InlineData(new double[] { 100, 1000 }, 1000, 75)]
    [InlineData(new double[] { 100, 1000 }, 550, 50)]
    public void CalculatePercentileRank_RanksWithinCohort(double[] values, double target, int expected)
    {
        Assert.Equal(expected, FinancialMath.CalculatePercentileRank(
            values.Select(v => (decimal)v).ToList(), (decimal)target));
    }

    [Fact]
    public void CalculateQuartiles_ReturnsQ1MedianQ3()
    {
        Assert.Equal((0m, 0m, 0m), FinancialMath.CalculateQuartiles([]));
        Assert.Equal((5m, 5m, 5m), FinancialMath.CalculateQuartiles([5m]));
        Assert.Equal((1.75m, 2.5m, 3.25m), FinancialMath.CalculateQuartiles([1m, 2m, 3m, 4m]));
    }

    [Theory]
    [InlineData(new double[] { }, 0.5, 0)]
    [InlineData(new double[] { 7 }, 0.5, 7)]
    [InlineData(new double[] { 1, 2, 3, 4 }, 0.5, 2.5)]
    [InlineData(new double[] { 1, 2, 3, 4 }, 0.25, 1.75)]
    [InlineData(new double[] { 1, 2, 3, 4 }, 1, 4)]
    public void CalculatePercentile_InterpolatesLinearly(double[] values, double percentile, double expected)
    {
        Assert.Equal((decimal)expected, FinancialMath.CalculatePercentile(
            values.Select(v => (decimal)v).ToList(), (decimal)percentile));
    }

    [Theory]
    [InlineData(new double[] { }, 5)]
    [InlineData(new double[] { 5 }, 5)]
    [InlineData(new double[] { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }, 5)]
    public void CalculateAdaptiveBinCount_HandlesDegenerateInput(double[] values, int expected)
    {
        Assert.Equal(expected, FinancialMath.CalculateAdaptiveBinCount(
            values.Select(v => (decimal)v).ToList()));
    }

    [Fact]
    public void CalculateAdaptiveBinCount_ClampsSpreadInput()
    {
        var values = new List<decimal> { 100m, 150m, 250m, 400m, 999m };

        var count = FinancialMath.CalculateAdaptiveBinCount(values);

        Assert.InRange(count, 5, 30);
    }

    [Fact]
    public void Percentile_InterpolatesWithDoublePosition()
    {
        Assert.Equal(1.75m, FinancialMath.Percentile([1m, 2m, 3m, 4m], 0.25));
    }
}
