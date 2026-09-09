// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Jobs.Monitor;

/// <summary>
/// Enqueues uptime collection jobs for one organization's monitors.
/// Runs on the organization's own recurring cadence.
/// </summary>
public class MonitorUptimeDispatchJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IBackgroundJobClient backgroundJobClient) : IOrgScopedJob
{
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(Guid organizationId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var hasConfiguredOrg = await dbContext.OrganizationConfigs
            .AnyAsync(c => c.OrganizationId == organizationId
                && c.MonitoringProviderSettings != null
                && c.MonitoringFetchEnabled);
        if (!hasConfiguredOrg) return;

        var monitors = await dbContext.Monitors
            .Where(m => m.Id > 0 && m.UptimeMonitorEnabled && m.Tenant!.OrganizationId == organizationId)
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
            if (!finalizedKeys.Contains((monitor.Id, yesterdayStart.UtcDateTime.Date)))
            {
                backgroundJobClient.Schedule<UpdateMonitorUptimeJob>(
                    x => x.ExecuteAsync(organizationId, monitor.Id, yesterdayStart, todayStart.AddSeconds(-1)),
                    TimeSpan.FromSeconds(index * 2));
                index++;
            }

            backgroundJobClient.Schedule<UpdateMonitorUptimeJob>(
                x => x.ExecuteAsync(organizationId, monitor.Id, todayStart, now),
                TimeSpan.FromSeconds(index * 2));
            index++;

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
                x => x.ExecuteAsync(organizationId, backfill.MonitorId, backfill.Start, backfill.End),
                TimeSpan.FromSeconds(index * 2));
            index++;
        }
    }
}