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
/// only when their kind is in the managed visibility set. The set is
/// stored on the global configuration as kind names and editable from
/// the platform settings page.
/// </summary>
public static class RecurringJobVisibility
{
    public static readonly IReadOnlyCollection<RecurringJobKind> DefaultVisiblePlatformKinds =
        new[]
        {
            RecurringJobKind.FinancialViewRefresh,
            RecurringJobKind.MonitoringViewRefresh,
            RecurringJobKind.StaleViewRefresh,
            RecurringJobKind.SystemEventCleanup,
            RecurringJobKind.CalendarSync
        };

    public static string DefaultVisiblePlatformJobs => JoinVisibleKinds(DefaultVisiblePlatformKinds);

    public static RecurringJobKind[] ParseVisibleKinds(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<RecurringJobKind>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(entry => Enum.TryParse<RecurringJobKind>(entry, ignoreCase: true, out var kind) ? kind : (RecurringJobKind?)null)
                .Where(kind => kind is not null)
                .Select(kind => kind!.Value)
                .Distinct()
                .ToArray();

    public static string JoinVisibleKinds(IEnumerable<RecurringJobKind> kinds)
        => string.Join(",", kinds.Select(kind => kind.ToString()));

    public static bool IsVisible(string jobId, Guid organizationId, IReadOnlySet<RecurringJobKind> visiblePlatformKinds)
    {
        if (RecurringJobId.TryParse(jobId, out var kind, out var jobOrganizationId))
        {
            if (jobOrganizationId == organizationId) return true;
            if (jobOrganizationId is null) return visiblePlatformKinds.Contains(kind);
        }

        return false;
    }

    public static bool IsPlatformWide(string jobId)
        => RecurringJobId.TryParse(jobId, out _, out var organizationId) && organizationId is null;
}