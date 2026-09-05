// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Infrastructure.DemoDataSeeding;

internal readonly record struct DemoLatencySample(double Average, double Lowest, double Highest);

internal static class DemoDataSimulation
{
    private static readonly double[] HourlyWeights =
    [
        0.020, 0.010, 0.005, 0.005, 0.007, 0.010, 0.020, 0.035,
        0.050, 0.058, 0.065, 0.068, 0.075, 0.075, 0.068, 0.060,
        0.050, 0.045, 0.048, 0.055, 0.070, 0.075, 0.065, 0.042
    ];

    private static readonly double[] DailyWeights =
    [
        0.12, 0.16, 0.16, 0.15, 0.16, 0.14, 0.11
    ];

    private static readonly double HourlyWeightTotal = HourlyWeights.Sum();

    public static int GenerateOrderCount(
        DemoTenantProfile profile,
        DateTimeOffset timestamp,
        TimeZoneInfo reportingTimeZone,
        Random random)
    {
        var expectedOrdersPerRun = GetExpectedOrderCountPerRun(
            profile,
            timestamp,
            reportingTimeZone);

        return (int)expectedOrdersPerRun
            + (random.NextDouble() < expectedOrdersPerRun % 1.0 ? 1 : 0);
    }

    public static double GetExpectedOrderCountPerRun(
        DemoTenantProfile profile,
        DateTimeOffset timestamp,
        TimeZoneInfo reportingTimeZone)
    {
        var reportingTimestamp = TimeZoneInfo.ConvertTime(timestamp, reportingTimeZone);
        var expectedOrdersToday = profile.DailyVolume * 7.0 * DailyWeights[(int)reportingTimestamp.DayOfWeek];
        var expectedOrdersThisHour = expectedOrdersToday * HourlyWeights[reportingTimestamp.Hour] / HourlyWeightTotal;
        return expectedOrdersThisHour * RuntimeDataSeederJob.FinancialSimulationIntervalMinutes / 60.0;
    }

    public static decimal GenerateOrderValue(DemoTenantProfile profile, Random random)
    {
        var distribution = profile.OrderValues;
        var median = distribution.SecondaryMedianAov.HasValue
            && random.NextDouble() < distribution.SecondaryWeight
                ? distribution.SecondaryMedianAov.Value
                : distribution.MedianAov;

        // Rejection sampling preserves the configured bounds without creating
        // artificial spikes at either edge of the distribution.
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var u1 = 1.0 - random.NextDouble();
            var u2 = 1.0 - random.NextDouble();
            var standardNormal =
                Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            var value = (decimal)(
                (double)median * Math.Exp(distribution.LogStandardDeviation * standardNormal));

            if (value >= profile.MinAov && value <= profile.MaxAov)
                return Math.Round(value, 2);
        }

        return median;
    }

    public static DemoLatencySample GenerateLatency(int monitorId, Random random)
    {
        var stableBaseLatency = 80 + (int)(Math.Abs((long)monitorId) % 200);
        var average = stableBaseLatency + random.Next(-10, 20);
        var highest = average + random.Next(10, 60);

        var spikeChance = random.NextDouble();
        if (spikeChance < 0.005)
        {
            highest += random.Next(800, 2500);
            average += random.Next(150, 400);
        }
        else if (spikeChance < 0.03)
        {
            highest += random.Next(150, 300);
            average += random.Next(30, 80);
        }

        return new DemoLatencySample(
            Math.Max(10, average),
            Math.Max(10, average - random.Next(5, 20)),
            highest);
    }

    public static double GenerateAvailability(int monitorId, Random random)
    {
        var ordinal = (int)(Math.Abs((long)monitorId) - 1);
        return DemoDataCatalog.GenerateUptime(random, DemoDataCatalog.GetReliability(ordinal));
    }

    public static DateTimeOffset FloorToFinancialInterval(DateTimeOffset timestamp)
        => FloorToInterval(timestamp, RuntimeDataSeederJob.FinancialSimulationIntervalMinutes);

    public static DateTimeOffset FloorToLatencyInterval(DateTimeOffset timestamp)
        => FloorToInterval(timestamp, RuntimeDataSeederJob.LatencySimulationIntervalMinutes);

    public static DateTimeOffset FloorToAvailabilityInterval(DateTimeOffset timestamp)
        => FloorToInterval(timestamp, RuntimeDataSeederJob.AvailabilitySimulationIntervalMinutes);

    private static DateTimeOffset FloorToInterval(DateTimeOffset timestamp, int intervalMinutes)
    {
        var utcTicks = timestamp.UtcTicks;
        var intervalTicks = TimeSpan.FromMinutes(intervalMinutes).Ticks;
        return new DateTimeOffset(utcTicks - utcTicks % intervalTicks, TimeSpan.Zero);
    }
}
