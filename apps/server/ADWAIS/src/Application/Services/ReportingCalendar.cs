// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Models;
using Adwais.Application.Interfaces;
using Adwais.Domain.Enums;

namespace Adwais.Application.Services;

/// <summary>
/// Resolves reporting periods using the time zone stored in the current
/// organization's configuration. UTC remains the persistence format; the
/// configured zone only defines business calendar concepts such as a day,
/// year, weekday, and hour.
/// </summary>
public sealed class ReportingCalendar(
    IOrganizationConfigService organizationConfigService,
    ICurrentAccess currentAccess) : IReportingCalendar
{
    private const string DefaultTimeZoneId = "Europe/Stockholm";
    private readonly ConcurrentDictionary<Guid, TimeZoneInfo> _timeZonesByOrg = new();

    public async Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken ct = default)
    {
        var orgId = currentAccess.Scope.OrganizationId;
        if (orgId is not null && _timeZonesByOrg.TryGetValue(orgId.Value, out var cached))
            return cached;

        var timeZoneId = DefaultTimeZoneId;
        if (orgId is not null)
        {
            var config = await organizationConfigService.GetConfigAsync(orgId.Value, ct);
            if (!string.IsNullOrWhiteSpace(config?.ReportingTimeZoneId))
                timeZoneId = config.ReportingTimeZoneId;
        }

        var resolved = ResolveTimeZone(timeZoneId);
        if (orgId is not null) _timeZonesByOrg[orgId.Value] = resolved;
        return resolved;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }
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
