// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Jobs.Monitor;

public class UptimeDispatcherJob(IDbContextFactory<AnalyticsDbContext> dbContextFactory, IBackgroundJobClient backgroundJobClient)
{
    public async Task ExecuteAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var globalConfig = await dbContext.GlobalConfigs.SingleOrDefaultAsync();
        if (globalConfig == null || string.IsNullOrWhiteSpace(globalConfig.MonitoringProviderSettings) || !globalConfig.MonitoringFetchEnabled)
        {
            return;
        }
        
        var monitors = await dbContext.Monitors
            .Where(m => m.Id > 0 && m.UptimeMonitorEnabled)
            .Select(m => new { m.Id, m.LastUptimeUpdate })
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var yesterdayStart = todayStart.AddDays(-1);
        var earliestRelevantDate = monitors
            .Select(monitor => monitor.LastUptimeUpdate.HasValue && monitor.LastUptimeUpdate.Value < todayStart
                ? new DateTimeOffset(
                    monitor.LastUptimeUpdate.Value.Year,
                    monitor.LastUptimeUpdate.Value.Month,
                    monitor.LastUptimeUpdate.Value.Day,
                    0, 0, 0, TimeSpan.Zero)
                : yesterdayStart)
            .DefaultIfEmpty(yesterdayStart)
            .Min();
        var retentionStart = todayStart.AddDays(-730);
        if (earliestRelevantDate < retentionStart) earliestRelevantDate = retentionStart;

        var finalizedDays = await dbContext.MonitorAvailabilities
            .AsNoTracking()
            .Where(row => row.IsFinalized
                && row.Date >= earliestRelevantDate
                && row.Date < todayStart)
            .Select(row => new { row.MonitorId, row.Date })
            .ToListAsync();
        var finalizedKeys = finalizedDays
            .Select(row => (row.MonitorId, Date: row.Date.UtcDateTime.Date))
            .ToHashSet();

        int index = 0;
        var olderBackfills = new List<(int MonitorId, DateTimeOffset Start, DateTimeOffset End)>();

        foreach (var monitor in monitors)
        {
            // Finalize yesterday first, then keep today's partial bucket fresh.
            if (!finalizedKeys.Contains((monitor.Id, yesterdayStart.UtcDateTime.Date)))
            {
                backgroundJobClient.Schedule<UpdateMonitorUptimeJob>(
                    x => x.ExecuteAsync(monitor.Id, yesterdayStart, todayStart.AddSeconds(-1)),
                    TimeSpan.FromSeconds(index * 2));
                index++;
            }

            backgroundJobClient.Schedule<UpdateMonitorUptimeJob>(
                x => x.ExecuteAsync(monitor.Id, todayStart, now),
                TimeSpan.FromSeconds(index * 2));
            index++;

            // Queue older gaps after every monitor has received its current-day update.
            var cursor = monitor.LastUptimeUpdate.HasValue && monitor.LastUptimeUpdate.Value < todayStart
                ? new DateTimeOffset(
                    monitor.LastUptimeUpdate.Value.Year,
                    monitor.LastUptimeUpdate.Value.Month,
                    monitor.LastUptimeUpdate.Value.Day,
                    0, 0, 0, TimeSpan.Zero)
                : yesterdayStart;
            if (cursor < retentionStart) cursor = retentionStart;

            while (cursor < yesterdayStart)
            {
                if (!finalizedKeys.Contains((monitor.Id, cursor.UtcDateTime.Date)))
                {
                    olderBackfills.Add((monitor.Id, cursor, cursor.AddDays(1).AddSeconds(-1)));
                }
                cursor = cursor.AddDays(1);
            }
        }

        foreach (var backfill in olderBackfills)
        {
            backgroundJobClient.Schedule<UpdateMonitorUptimeJob>(
                x => x.ExecuteAsync(backfill.MonitorId, backfill.Start, backfill.End),
                TimeSpan.FromSeconds(index * 2));
            index++;
        }
    }
}
