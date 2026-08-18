// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Jobs.Monitor;

public class LatencyDispatcherJob(IDbContextFactory<AnalyticsDbContext> dbContextFactory, IBackgroundJobClient backgroundJobClient)
{
    public async Task ExecuteAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var globalConfig = await dbContext.GlobalConfigs.SingleOrDefaultAsync();
        var hasConfiguredOrg = await dbContext.OrganizationConfigs
            .AnyAsync(c => c.MonitoringProviderSettings != null);

        if (globalConfig == null || !globalConfig.MonitoringFetchEnabled || !hasConfiguredOrg)
        {
            return;
        }

        var defaultOrgConfig = await dbContext.OrganizationConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.MonitoringProviderSettings != null);
        var globalInterval = defaultOrgConfig?.LatencyFetchIntervalMinutes ?? 10;
        
        var monitors = await dbContext.Monitors
            .Where(m => m.Id > 0 && m.UptimeMonitorEnabled)
            .Select(m => new { m.Id, m.LastLatencyUpdate })
            .ToListAsync();

        var end = DateTimeOffset.UtcNow;
        int index = 0;

        foreach (var monitor in monitors)
        {
            var start = monitor.LastLatencyUpdate ?? end.AddMinutes(-globalInterval);

            backgroundJobClient.Schedule<UpdateMonitorLatencyJob>(
                x => x.ExecuteAsync(monitor.Id, start, end), 
                TimeSpan.FromSeconds(index * 2));
            index++;
        }
    }
}
