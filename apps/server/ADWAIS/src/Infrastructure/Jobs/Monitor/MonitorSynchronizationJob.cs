// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Jobs.Monitor;

/// <summary>
/// Platform scheduler for fleet sync. Reads no provider data itself; it
/// fans out one per-org sync job per enabled organization and tunes its
/// own cadence from the locally synced monitor intervals.
/// </summary>
public class MonitorSynchronizationJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IBackgroundJobClient backgroundJobClient,
    IRecurringJobManager recurringJobManager)
{
    public async Task ExecuteAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var orgIds = await dbContext.OrganizationConfigs
            .Where(config => config.MonitoringProviderSettings != null && config.MonitoringFetchEnabled)
            .Select(config => config.OrganizationId)
            .ToListAsync();

        var lowestIntervalSeconds = await dbContext.Monitors
            .Where(m => m.UptimeMonitorEnabled && orgIds.Contains(m.Tenant!.OrganizationId))
            .Select(m => (int?)m.UpdateInterval)
            .MinAsync() ?? 300;
        var lowestIntervalMins = Math.Max(1, lowestIntervalSeconds / 60);

        recurringJobManager.AddOrUpdate<MonitorSynchronizationJob>(
            "sync-monitoring-fleet",
            job => job.ExecuteAsync(),
            Cron.MinuteInterval(lowestIntervalMins));

        foreach (var orgId in orgIds)
        {
            backgroundJobClient.Enqueue<SyncOrganizationFleetJob>(
                job => job.ExecuteAsync(orgId));
        }
    }
}