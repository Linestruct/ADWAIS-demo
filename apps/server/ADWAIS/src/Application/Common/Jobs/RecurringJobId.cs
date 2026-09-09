// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

namespace Adwais.Application.Common.Jobs;

public enum RecurringJobKind
{
    OrderFetch,
    UptimeFetch,
    LatencyFetch,
    UserStatsFetch,
    FleetSync,
    FeedFetch,
    FinancialViewRefresh,
    MonitoringViewRefresh,
    StaleViewRefresh,
    SystemEventCleanup,
    CalendarSync,
    RuntimeDataSeeder
}

/// <summary>
/// The single source of truth for recurring job ids. An organization
/// scoped kind always carries a `-{organizationId}` suffix; a platform
/// wide kind never does. Construction, parsing, classification, and
/// naming all live here, so the conventions cannot drift.
/// </summary>
public static class RecurringJobId
{
    private static readonly IReadOnlyDictionary<RecurringJobKind, string> BaseIds =
        new Dictionary<RecurringJobKind, string>
        {
            [RecurringJobKind.OrderFetch] = "dispatch-order-fetch",
            [RecurringJobKind.UptimeFetch] = "dispatch-monitoring-uptime",
            [RecurringJobKind.LatencyFetch] = "dispatch-monitoring-latency",
            [RecurringJobKind.UserStatsFetch] = "sync-monitoring-account-stats",
            [RecurringJobKind.FleetSync] = "sync-monitoring-fleet",
            [RecurringJobKind.FeedFetch] = "aggregate-intranet-feeds",
            [RecurringJobKind.FinancialViewRefresh] = "refresh-financial-materialized-views",
            [RecurringJobKind.MonitoringViewRefresh] = "refresh-monitoring-materialized-views",
            [RecurringJobKind.StaleViewRefresh] = "refresh-stale-materialized-views",
            [RecurringJobKind.SystemEventCleanup] = "system-event-cleanup",
            [RecurringJobKind.CalendarSync] = "sync-intranet-calendars",
            [RecurringJobKind.RuntimeDataSeeder] = "dev-runtime-data-seeder"
        };

    private static readonly IReadOnlyDictionary<RecurringJobKind, string> DisplayNames =
        new Dictionary<RecurringJobKind, string>
        {
            [RecurringJobKind.OrderFetch] = "Order Fetch",
            [RecurringJobKind.UptimeFetch] = "Uptime Fetch",
            [RecurringJobKind.LatencyFetch] = "Latency Fetch",
            [RecurringJobKind.UserStatsFetch] = "User Stats Fetch",
            [RecurringJobKind.FleetSync] = "Fleet Sync",
            [RecurringJobKind.FeedFetch] = "Feed Fetch",
            [RecurringJobKind.FinancialViewRefresh] = "Financial View Refresh",
            [RecurringJobKind.MonitoringViewRefresh] = "Monitoring View Refresh",
            [RecurringJobKind.StaleViewRefresh] = "Stale View Refresh",
            [RecurringJobKind.SystemEventCleanup] = "System Event Cleanup",
            [RecurringJobKind.CalendarSync] = "Calendar Sync",
            [RecurringJobKind.RuntimeDataSeeder] = "Runtime Data Seeder"
        };

    public static bool IsOrganizationScoped(RecurringJobKind kind)
        => kind is RecurringJobKind.OrderFetch
            or RecurringJobKind.UptimeFetch
            or RecurringJobKind.LatencyFetch
            or RecurringJobKind.UserStatsFetch
            or RecurringJobKind.FleetSync
            or RecurringJobKind.FeedFetch;

    public static string For(RecurringJobKind kind, Guid organizationId)
    {
        if (!IsOrganizationScoped(kind))
        {
            throw new ArgumentException($"{kind} is platform wide and cannot be organization scoped.", nameof(kind));
        }

        return $"{BaseIds[kind]}-{organizationId}";
    }

    public static string Platform(RecurringJobKind kind)
    {
        if (IsOrganizationScoped(kind))
        {
            throw new ArgumentException($"{kind} is organization scoped and requires an organization id.", nameof(kind));
        }

        return BaseIds[kind];
    }

    public static bool TryParse(string jobId, out RecurringJobKind kind, out Guid? organizationId)
    {
        organizationId = null;

        foreach (var (candidateKind, baseId) in BaseIds)
        {
            if (!jobId.StartsWith(baseId, StringComparison.Ordinal)) continue;

            if (jobId.Length == baseId.Length)
            {
                kind = candidateKind;
                return true;
            }

            if (jobId.Length > baseId.Length + 1
                && jobId[baseId.Length] == '-'
                && Guid.TryParse(jobId[(baseId.Length + 1)..], out var orgId)
                && IsOrganizationScoped(candidateKind))
            {
                kind = candidateKind;
                organizationId = orgId;
                return true;
            }
        }

        kind = default;
        return false;
    }

    public static string DisplayName(string jobId, string? organizationName = null)
    {
        if (!TryParse(jobId, out var kind, out var organizationId))
        {
            return jobId;
        }

        var label = DisplayNames[kind];
        return organizationId is not null && organizationName is not null
            ? $"{label} ({organizationName})"
            : label;
    }
}