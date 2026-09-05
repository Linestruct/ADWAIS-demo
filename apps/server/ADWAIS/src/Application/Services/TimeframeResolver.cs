// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Models;
using Adwais.Domain.Enums;

namespace Adwais.Application.Services;

/// <summary>
/// Converts a <see cref="Timeframe"/> enum into concrete UTC instants using a
/// supplied reporting time zone for local calendar boundaries.
/// </summary>
public static class TimeframeResolver
{
    /// <summary>
    /// Resolves the current and previous period date ranges for a given timeframe.
    /// Calendar boundaries are aligned in <paramref name="timeZone"/> and then
    /// converted to UTC. An explicit <paramref name="now"/> can be supplied for
    /// deterministic tests.
    /// </summary>
    public static ResolvedPeriod Resolve(
        Timeframe timeframe,
        ComparisonType comparisonType = ComparisonType.Preceding,
        TimeZoneInfo? timeZone = null,
        DateTimeOffset? now = null)
    {
        timeZone ??= TimeZoneInfo.Utc;
        var currentEnd = now ?? DateTimeOffset.UtcNow;
        var localNow = TimeZoneInfo.ConvertTime(currentEnd, timeZone);
        var today = localNow.Date;

        return timeframe switch
        {
            Timeframe.Today => BuildRollingHourlyPeriod(currentEnd, 1, comparisonType),
            Timeframe.T7 => BuildRollingHourlyPeriod(currentEnd, 7, comparisonType),
            Timeframe.T30 => BuildFixedPeriod(today, currentEnd, 30, comparisonType, timeZone),
            Timeframe.T90 => BuildFixedPeriod(today, currentEnd, 90, comparisonType, timeZone),
            Timeframe.Ytd => BuildYtdPeriod(today, currentEnd, comparisonType, timeZone),
            Timeframe.T365 => BuildFixedPeriod(today, currentEnd, 365, comparisonType, timeZone),
            _ => BuildFixedPeriod(today, currentEnd, 30, comparisonType, timeZone)
        };
    }

    /// <summary>
    /// Builds a fixed-length period (e.g., trailing 30 days) and its previous comparison period.
    /// </summary>
    private static ResolvedPeriod BuildFixedPeriod(DateTime today, DateTimeOffset currentEnd, int days, ComparisonType comparisonType, TimeZoneInfo timeZone)
    {
        var currentStartLocal = today.AddDays(-(days - 1));
        var previousStartLocal = comparisonType == ComparisonType.YearOverYear
            ? today.AddYears(-1).AddDays(-(days - 1))
            : currentStartLocal.AddDays(-days);
        var currentStart = ConvertLocalToUtc(currentStartLocal, timeZone);
        var previousStart = ConvertLocalToUtc(previousStartLocal, timeZone);
        var previousEnd = previousStart + (currentEnd - currentStart);

        return new ResolvedPeriod(currentStart, currentEnd, previousStart, previousEnd, days, false, false);
    }

    /// <summary>
    /// Builds a rolling hourly period and its previous comparison period.
    /// </summary>
    private static ResolvedPeriod BuildRollingHourlyPeriod(DateTimeOffset currentEnd, int days, ComparisonType comparisonType)
    {
        var currentStart = currentEnd.AddDays(-days);
        var previousStart = comparisonType == ComparisonType.YearOverYear
            ? currentStart.AddYears(-1)
            : currentStart.AddDays(-days);
        var previousEnd = previousStart.AddDays(days);
        
        var steps = days == 7 ? 42 : 48;
        return new ResolvedPeriod(currentStart, currentEnd, previousStart, previousEnd, steps, true, days == 1);
    }

    /// <summary>
    /// Builds a Year-to-Date (YTD) period and an equivalent-length comparison period.
    /// </summary>
    private static ResolvedPeriod BuildYtdPeriod(DateTime today, DateTimeOffset currentEnd, ComparisonType comparisonType, TimeZoneInfo timeZone)
    {
        var currentStartLocal = new DateTime(today.Year, 1, 1);
        var daysInPeriod = (today.AddDays(1) - currentStartLocal).Days;
        var previousStartLocal = comparisonType == ComparisonType.YearOverYear
            ? currentStartLocal.AddYears(-1)
            : currentStartLocal.AddDays(-daysInPeriod);
        var currentStart = ConvertLocalToUtc(currentStartLocal, timeZone);
        var previousStart = ConvertLocalToUtc(previousStartLocal, timeZone);
        var previousEnd = previousStart + (currentEnd - currentStart);

        return new ResolvedPeriod(currentStart, currentEnd, previousStart, previousEnd, daysInPeriod, false, false);
    }

    /// <summary>
    /// Converts an unspecified local wall-clock value to UTC. If a zone has a
    /// rare DST transition at that exact time, advances to its first valid minute.
    /// </summary>
    public static DateTimeOffset ConvertLocalToUtc(DateTime localDateTime, TimeZoneInfo timeZone)
    {
        var local = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        while (timeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(1);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, timeZone), TimeSpan.Zero);
    }
}
