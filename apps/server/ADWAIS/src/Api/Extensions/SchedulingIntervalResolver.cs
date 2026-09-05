// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;

namespace Adwais.Api.Extensions;

/// <summary>
/// Resolves the recurring job intervals from an organization's config.
/// Missing configs fall back to deployment defaults, and interval values
/// below one minute are clamped so Hangfire never receives a zero cron.
/// </summary>
public static class SchedulingIntervalResolver
{
    public static (int UptimeMinutes, int LatencyMinutes, int OrderFetchMinutes, int UserStatsMinutes, int FeedHours) Resolve(
        OrganizationConfig? config)
        => (
            UptimeMinutes: config?.UptimeFetchIntervalMinutes ?? 60,
            LatencyMinutes: config?.LatencyFetchIntervalMinutes ?? 10,
            OrderFetchMinutes: Math.Max(1, config?.OrderFetchIntervalMinutes ?? 10),
            UserStatsMinutes: config?.UserStatsFetchIntervalMinutes ?? 60,
            FeedHours: Math.Max(1, config?.FeedFetchIntervalHours ?? 2)
        );
}
