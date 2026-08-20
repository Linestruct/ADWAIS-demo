// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Models;
using Adwais.Application.Interfaces;
using Adwais.Domain.Enums;

namespace Adwais.Application.Services;

/// <summary>
/// Resolves reporting periods using the time zone stored in global configuration.
/// UTC remains the persistence format; the configured zone only defines business
/// calendar concepts such as a day, year, weekday, and hour.
/// </summary>
public sealed class ReportingCalendar(IGlobalConfigService globalConfigService) : IReportingCalendar
{
    private const string DefaultTimeZoneId = "Europe/Stockholm";
    private TimeZoneInfo? _timeZone;

    public async Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken ct = default)
    {
        if (_timeZone is not null) return _timeZone;

        var config = await globalConfigService.GetConfigAsync(ct);
        var timeZoneId = string.IsNullOrWhiteSpace(config.ReportingTimeZoneId)
            ? DefaultTimeZoneId
            : config.ReportingTimeZoneId;

        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }
        catch (InvalidTimeZoneException)
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }

        return _timeZone!;
    }

    public async Task<ResolvedPeriod> ResolvePeriodAsync(
        Timeframe timeframe,
        ComparisonType comparisonType = ComparisonType.Preceding,
        CancellationToken ct = default)
    {
        var timeZone = await GetTimeZoneAsync(ct);
        return TimeframeResolver.Resolve(timeframe, comparisonType, timeZone);
    }

    public DateTimeOffset GetStartOfDayUtc(DateTimeOffset instant, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(instant, timeZone);
        return TimeframeResolver.ConvertLocalToUtc(local.Date, timeZone);
    }
}
