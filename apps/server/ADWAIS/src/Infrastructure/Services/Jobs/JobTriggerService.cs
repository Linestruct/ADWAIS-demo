// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Jobs.Monitor;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services.Jobs;

public class JobTriggerService(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IBackgroundJobClient backgroundJobClient) : IJobTriggerService
{
    private static readonly TimeSpan MaxAutomatedGap = TimeSpan.FromDays(31);

    public async Task TriggerOrderSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var disabledOrgIds = await db.OrganizationConfigs
            .Where(c => !c.OrderFetchEnabled)
            .Select(c => c.OrganizationId)
            .ToListAsync(ct);
        var disabledOrgIdSet = disabledOrgIds.ToHashSet();

        var tenants = await db.Tenants
            .Where(t => t.OrderFetchingEnabled && !t.IsSystem
                && t.OrderProviderSettings != null
                && (organizationId == null || t.OrganizationId == organizationId))
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var tenant in tenants.Where(t => !disabledOrgIdSet.Contains(t.OrganizationId)))
        {
            if (tenant.CurrentlyFetching) continue;

            var start = tenant.FetchedUntil ?? now.AddDays(-2);
            if (now - start > MaxAutomatedGap) continue;

            backgroundJobClient.Enqueue<IOrderIngestionService>(
                ingestion => ingestion.ExecuteIngestionAsync(tenant.OrganizationId, tenant.Id, start, now, CancellationToken.None));

            tenant.CurrentlyFetching = true;
            tenant.LastPolled = now;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task TriggerUptimeSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var monitors = await db.Monitors
            .Where(m => m.UptimeMonitorEnabled
                && (organizationId == null || m.Tenant!.OrganizationId == organizationId))
            .Select(m => new { m.Id, OrgId = m.Tenant!.OrganizationId })
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);

        foreach (var monitor in monitors)
        {
            backgroundJobClient.Enqueue<UpdateMonitorUptimeJob>(
                job => job.ExecuteAsync(monitor.OrgId, monitor.Id, todayStart, now));
        }
    }

    public async Task TriggerLatencySyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var monitors = await db.Monitors
            .Where(m => m.UptimeMonitorEnabled
                && (organizationId == null || m.Tenant!.OrganizationId == organizationId))
            .Select(m => new { m.Id, m.LastLatencyUpdate, OrgId = m.Tenant!.OrganizationId })
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var monitor in monitors)
        {
            var start = monitor.LastLatencyUpdate ?? now.AddMinutes(-10);
            backgroundJobClient.Enqueue<UpdateMonitorLatencyJob>(
                job => job.ExecuteAsync(monitor.OrgId, monitor.Id, start, now));
        }
    }

    public async Task TriggerFleetSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        foreach (var orgId in await ResolveTargetOrgsAsync(db, organizationId, ct))
        {
            backgroundJobClient.Enqueue<SyncOrganizationFleetJob>(
                job => job.ExecuteAsync(orgId));
        }
    }

    public async Task TriggerAccountStatsSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        foreach (var orgId in await ResolveTargetOrgsAsync(db, organizationId, ct))
        {
            backgroundJobClient.Enqueue<SyncOrganizationAccountStatsJob>(
                job => job.ExecuteAsync(orgId));
        }
    }

    private static async Task<List<Guid>> ResolveTargetOrgsAsync(
        AnalyticsDbContext db, Guid? organizationId, CancellationToken ct)
    {
        if (organizationId is { } org) return [org];

        return await db.OrganizationConfigs
            .Where(c => c.MonitoringProviderSettings != null && c.MonitoringFetchEnabled)
            .Select(c => c.OrganizationId)
            .ToListAsync(ct);
    }
}