// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Adwais.Infrastructure.Jobs;

/// <summary>
/// Enqueues order ingestion jobs for one organization's eligible tenants.
/// Runs on the organization's own recurring cadence.
/// </summary>
public class OrderFetchDispatchJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IBackgroundJobClient backgroundJobClient,
    ILogger<OrderFetchDispatchJob> logger,
    ISystemEventService eventService) : IOrgScopedJob
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan MaxAutomatedGap = TimeSpan.FromDays(31);

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(Guid organizationId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var orgConfig = await db.OrganizationConfigs
            .SingleOrDefaultAsync(c => c.OrganizationId == organizationId);
        if (orgConfig is { OrderFetchEnabled: false }) return;

        var tenants = await db.Tenants
            .Where(t => t.OrganizationId == organizationId
                && t.OrderFetchingEnabled && !t.IsSystem
                && t.OrderProviderSettings != null)
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        var dispatched = 0;

        foreach (var tenant in tenants)
        {
            if (tenant.CurrentlyFetching && tenant.LastPolled.HasValue
                && now - tenant.LastPolled.Value > StaleThreshold)
            {
                var msg = $"Tenant {tenant.Id} has stale CurrentlyFetching flag. Resetting.";
                await eventService.LogWarningAsync(nameof(OrderFetchDispatchJob), msg, $"Last polled: {tenant.LastPolled}", tenant.Id);
                tenant.CurrentlyFetching = false;
            }

            if (tenant.CurrentlyFetching) continue;

            var start = tenant.FetchedUntil ?? now.AddDays(-2);

            if (now - start > MaxAutomatedGap)
            {
                var msg = $"Fetch gap too large ({Math.Floor((now - start).TotalDays)} days). Skipping automated sync.";
                await eventService.LogWarningAsync(nameof(OrderFetchDispatchJob), msg, "Automated sync only handles gaps up to 31 days. Use the manual backfill endpoint to recover this tenant.", tenant.Id);
                continue;
            }

            backgroundJobClient.Enqueue<IOrderIngestionService>(
                ingestion => ingestion.ExecuteIngestionAsync(organizationId, tenant.Id, start, now, CancellationToken.None));

            tenant.CurrentlyFetching = true;
            tenant.LastPolled = now;
            dispatched++;
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Order fetch dispatch complete for org {OrgId}. Enqueued {Dispatched}/{Total} tenants.",
            organizationId, dispatched, tenants.Count);
    }
}