// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Adwais.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Adwais.Infrastructure.Jobs;

public class OrderFetchDispatcherJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IBackgroundJobClient backgroundJobClient,
    ILogger<OrderFetchDispatcherJob> logger,
    ISystemEventService eventService)
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(60);

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync()
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        var config = await db.GlobalConfigs.SingleOrDefaultAsync();

        if (config is null || !config.OrderFetchEnabled)
        {
            logger.LogInformation("Order fetching disabled globally. Skipping.");
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var tenants = await db.Tenants
            .Where(t => t.OrderFetchingEnabled && t.Id != AnalyticsDbContext.SystemTenantGuid
                        && t.OrderProviderSettings != null)
            .ToListAsync();

        var dispatched = 0;

        foreach (var tenant in tenants)
        {
            if (tenant.CurrentlyFetching && tenant.LastPolled.HasValue
                && now - tenant.LastPolled.Value > StaleThreshold)
            {
                var msg = $"Tenant {tenant.Id} has stale CurrentlyFetching flag. Resetting.";
                await eventService.LogWarningAsync(nameof(OrderFetchDispatcherJob), msg, $"Last polled: {tenant.LastPolled}", tenant.Id);
                tenant.CurrentlyFetching = false;
            }

            if (tenant.CurrentlyFetching)
            {
                logger.LogInformation("Tenant {Id} is currently fetching. Skipping.", tenant.Id);
                continue;
            }

            var start = tenant.FetchedUntil ?? now.AddDays(-2);
            var end = now;

            if (end - start > TimeSpan.FromDays(31))
            {
                var msg = $"Fetch gap too large ({Math.Floor((end - start).TotalDays)} days). Skipping automated sync.";
                await eventService.LogWarningAsync(nameof(OrderFetchDispatcherJob), msg, "Automated sync only handles gaps up to 31 days. Use the manual backfill endpoint to recover this tenant.", tenant.Id);
                continue;
            }

            backgroundJobClient.Enqueue<IOrderIngestionService>(
                ingestionService => ingestionService.ExecuteIngestionAsync(tenant.Id, start, end, CancellationToken.None));

            tenant.CurrentlyFetching = true;
            tenant.LastPolled = now;
            dispatched++;
        }

        config.LastPolled = now;
        await db.SaveChangesAsync();

        logger.LogInformation("Order fetch dispatch complete. Enqueued {Dispatched}/{Total} tenants.",
            dispatched, tenants.Count);
    }
}



