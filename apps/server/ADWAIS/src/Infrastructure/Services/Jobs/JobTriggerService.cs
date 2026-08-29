// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Jobs;
using Adwais.Infrastructure.Jobs.Monitor;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services.Jobs;

public class JobTriggerService(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IBackgroundJobClient backgroundJobClient) : IJobTriggerService
{
    public async Task TriggerOrderSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(dbContextFactory, organizationId, ct))
        {
            backgroundJobClient.Enqueue<OrderFetchDispatchJob>(
                job => job.ExecuteAsync(orgId));
        }
    }

    public async Task TriggerUptimeSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(dbContextFactory, organizationId, ct))
        {
            backgroundJobClient.Enqueue<MonitorUptimeDispatchJob>(
                job => job.ExecuteAsync(orgId));
        }
    }

    public async Task TriggerLatencySyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(dbContextFactory, organizationId, ct))
        {
            backgroundJobClient.Enqueue<MonitorLatencyDispatchJob>(
                job => job.ExecuteAsync(orgId));
        }
    }

    public async Task TriggerFleetSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(dbContextFactory, organizationId, ct))
        {
            backgroundJobClient.Enqueue<SyncOrganizationFleetJob>(
                job => job.ExecuteAsync(orgId));
        }
    }

    public async Task TriggerAccountStatsSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(dbContextFactory, organizationId, ct))
        {
            backgroundJobClient.Enqueue<SyncOrganizationAccountStatsJob>(
                job => job.ExecuteAsync(orgId));
        }
    }

    public async Task TriggerFeedSyncAsync(Guid? organizationId, CancellationToken ct = default)
    {
        foreach (var orgId in await ResolveTargetOrgsAsync(dbContextFactory, organizationId, ct))
        {
            backgroundJobClient.Enqueue<AggregateOrganizationFeedsJob>(
                job => job.ExecuteAsync(orgId, CancellationToken.None));
        }
    }

    private static async Task<List<Guid>> ResolveTargetOrgsAsync(
        IDbContextFactory<AnalyticsDbContext> dbContextFactory, Guid? organizationId, CancellationToken ct)
    {
        if (organizationId is { } org) return [org];

        await using var db = await dbContextFactory.CreateDbContextAsync(ct);
        return await db.OrganizationConfigs
            .Select(c => c.OrganizationId)
            .ToListAsync(ct);
    }
}