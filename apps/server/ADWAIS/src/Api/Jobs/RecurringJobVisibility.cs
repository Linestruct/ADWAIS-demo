// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;

namespace Adwais.Api.Jobs;

/// <summary>
/// Controls which recurring jobs an organization-scoped caller may see.
/// Organization jobs are always visible. Platform-wide jobs are visible
/// only when curated as benign; anything not listed stays hidden.
/// </summary>
public static class RecurringJobVisibility
{
    public static readonly IReadOnlySet<string> PlatformJobsVisibleToOrganizations = new HashSet<string>(StringComparer.Ordinal)
    {
        "refresh-financial-materialized-views",
        "refresh-monitoring-materialized-views",
        "refresh-stale-materialized-views",
        "system-event-cleanup",
        "sync-intranet-calendars"
    };

    public static bool VisibleToOrganizationScope(string jobId, Guid organizationId)
        => jobId.EndsWith($"-{organizationId}", StringComparison.Ordinal)
            || PlatformJobsVisibleToOrganizations.Contains(jobId);
}