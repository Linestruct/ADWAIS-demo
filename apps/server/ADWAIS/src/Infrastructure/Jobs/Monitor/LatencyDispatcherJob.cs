// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

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
        
        if (globalConfig == null || string.IsNullOrWhiteSpace(globalConfig.MonitoringProviderSettings) || !globalConfig.MonitoringFetchEnabled)
        {
            return;
        }
        
        var globalInterval = globalConfig.LatencyFetchIntervalMinutes;
        
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
