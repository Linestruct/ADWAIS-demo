// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.Services;

/// <summary>
/// Statistics helpers for the financial endpoints. Parameters named
/// "sorted" expect sorted input; the methods do not sort.
/// </summary>
public static class FinancialMath
{
    public static decimal CalculateGrowthPercentage(decimal current, decimal previous)
    {
        if (previous == 0)
            return 0m;

        return Math.Round((current - previous) / previous * 100, 2);
    }

    public static decimal CalculateMedian(List<decimal> sorted)
    {
        if (sorted.Count == 0) return 0;
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }

    public static int CalculatePercentileRank(List<decimal> sortedValues, decimal targetValue)
    {
        if (sortedValues.Count <= 1) return 50;
        int countLess = 0;
        int countEqual = 0;
        foreach (var v in sortedValues)
        {
            if (v < targetValue) countLess++;
            else if (v == targetValue) countEqual++;
        }
        return (int)Math.Round(((countLess + 0.5m * countEqual) / sortedValues.Count) * 100m);
    }

    public static (decimal Q1, decimal Median, decimal Q3) CalculateQuartiles(List<decimal> sortedValues)
    {
        if (sortedValues.Count == 0) return (0m, 0m, 0m);
        if (sortedValues.Count == 1) return (sortedValues[0], sortedValues[0], sortedValues[0]);

        var median = CalculatePercentile(sortedValues, 0.5m);
        var q1 = CalculatePercentile(sortedValues, 0.25m);
        var q3 = CalculatePercentile(sortedValues, 0.75m);
        return (q1, median, q3);
    }

    public static decimal CalculatePercentile(List<decimal> sortedValues, decimal percentile)
    {
        if (sortedValues.Count == 0) return 0m;
        if (sortedValues.Count == 1) return sortedValues[0];

        var n = sortedValues.Count;
        var position = (n - 1) * percentile;
        var index = (int)Math.Floor(position);
        var fraction = position - index;

        if (index >= n - 1) return sortedValues[^1];
        return Math.Round(sortedValues[index] + fraction * (sortedValues[index + 1] - sortedValues[index]), 2);
    }

    public static int CalculateAdaptiveBinCount(List<decimal> sortedValues)
    {
        var n = sortedValues.Count;
        if (n < 2) return 5;

        var q1 = Percentile(sortedValues, 0.25);
        var q3 = Percentile(sortedValues, 0.75);
        var iqr = q3 - q1;

        if (iqr == 0)
        {
            return (int)Math.Ceiling(Math.Log2(n) + 1);
        }

        var binWidth = 2.0 * (double)iqr * Math.Pow(n, -1.0 / 3.0);
        var range = (double)(sortedValues[^1] - sortedValues[0]);
        var count = (int)Math.Ceiling(range / binWidth);

        return Math.Clamp(count, 5, 30);
    }

    public static decimal Percentile(List<decimal> sorted, double p)
    {
        var index = p * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper) return sorted[lower];

        var weight = (decimal)(index - lower);
        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }
}
