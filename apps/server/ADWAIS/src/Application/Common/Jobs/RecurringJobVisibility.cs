// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;

namespace Adwais.Application.Common.Jobs;

/// <summary>
/// Controls which recurring jobs an organization-scoped caller may see.
/// Organization jobs are always visible. Platform-wide jobs are visible
/// only when listed in the managed visibility set; anything not listed
/// stays hidden. The set is stored on the global configuration and
/// editable from the platform settings page.
/// </summary>
public static class RecurringJobVisibility
{
    public const string DefaultVisiblePlatformJobs =
        "refresh-financial-materialized-views,refresh-monitoring-materialized-views,refresh-stale-materialized-views,system-event-cleanup,sync-intranet-calendars";

    public static string[] ParseVisibleJobs(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<string>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string JoinVisibleJobs(IEnumerable<string> jobs) => string.Join(",", jobs);

    public static bool IsVisible(string jobId, Guid organizationId, IReadOnlySet<string> visiblePlatformJobs)
        => jobId.EndsWith($"-{organizationId}", StringComparison.Ordinal)
            || visiblePlatformJobs.Contains(jobId);
}