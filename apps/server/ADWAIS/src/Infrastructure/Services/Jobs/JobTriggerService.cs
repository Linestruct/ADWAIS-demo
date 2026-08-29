// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services.Jobs;

/// <summary>
/// Triggers the per-organization recurring jobs on demand. Organization
/// scope fires that organization's jobs; platform scope fires every
/// organization's jobs. Firing through the recurring manager keeps the
/// scheduled-jobs table's last execution and status up to date.
/// </summary>
public class JobTriggerService(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IRecurringJobManager recurringJobManager) : IJobTriggerService
{
    public async Task TriggerOrderSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(organizationId, ct))
        {
            recurringJobManager.Trigger($"dispatch-order-fetch-{orgId}");
        }
    }

    public async Task TriggerUptimeSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(organizationId, ct))
        {
            recurringJobManager.Trigger($"dispatch-monitoring-uptime-{orgId}");
        }
    }

    public async Task TriggerLatencySyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(organizationId, ct))
        {
            recurringJobManager.Trigger($"dispatch-monitoring-latency-{orgId}");
        }
    }

    public async Task TriggerFleetSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(organizationId, ct))
        {
            recurringJobManager.Trigger($"sync-monitoring-fleet-{orgId}");
        }
    }

    public async Task TriggerAccountStatsSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(organizationId, ct))
        {
            recurringJobManager.Trigger($"sync-monitoring-account-stats-{orgId}");
        }
    }

    public async Task TriggerFeedSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(organizationId, ct))
        {
            recurringJobManager.Trigger($"aggregate-intranet-feeds-{orgId}");
        }
    }

    private async Task<List<Guid>> ResolveTargetOrgsAsync(Guid? organizationId, CancellationToken ct)
    {
        if (organizationId is { } org) return [org];

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.OrganizationConfigs
            .Select(c => c.OrganizationId)
            .ToListAsync(ct);
    }
}