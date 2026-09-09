// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.System;
using Adwais.Application.Interfaces;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services;

public class SystemHealthService(IApplicationDbContext dbContext, ICurrentAccess currentAccess) : ISystemHealthService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ICurrentAccess _currentAccess = currentAccess;

    public async Task<SystemHealthDto> GetHealthAsync(CancellationToken ct = default)
    {
        var db = _dbContext;
        
        string dbStatus = "Healthy";
        DateTimeOffset? lastLitiumSync = null;
        DateTimeOffset? lastBlogSync = null;
        DateTimeOffset? lastFleetUpdate = null;
        DateTimeOffset? lastFleetUptimeUpdate = null;
        DateTimeOffset? lastFleetLatencyUpdate = null;
        string? globalSyncError = null;
        
        int totalMonitors = 0;
        int monitorsWithErrors = 0;
        int tenantsWithErrors = 0;
        int feedsWithErrors = 0;

        try
        {
            var config = await db.GlobalConfigs.AsNoTracking().SingleOrDefaultAsync(ct);
            lastLitiumSync = config?.LastPolled;
            var hasGlobalSyncError = await db.OrganizationConfigs
                .Where(c => c.LastSyncError != null)
                .Select(_ => true)
                .FirstOrDefaultAsync(ct);
            globalSyncError = hasGlobalSyncError
                ? "One or more organization pipelines have a recorded failure."
                : null;

            // Safe checks in case of empty sequences
            if (await db.Monitors.AnyAsync(ct))
            {
                lastFleetUpdate = await db.Monitors.MaxAsync(m => m.LastUpdate, ct);
                lastFleetUptimeUpdate = await db.Monitors.MaxAsync(m => m.LastUptimeUpdate, ct);
                lastFleetLatencyUpdate = await db.Monitors.MaxAsync(m => m.LastLatencyUpdate, ct);
            }

            totalMonitors = await db.Monitors.CountAsync(m => m.UptimeMonitorEnabled, ct);
            monitorsWithErrors = await db.Monitors.CountAsync(m => m.LastSyncError != null && m.UptimeMonitorEnabled, ct);
            tenantsWithErrors = await db.Tenants.CountAsync(t => t.LastSyncError != null && !t.IsSystem, ct);

            var activeFeeds = await db.FeedSources.AsNoTracking().Where(fs => fs.IsActive).ToListAsync(ct);
            if (activeFeeds.Any())
            {
                var successDates = activeFeeds.Where(fs => fs.LastSuccessAt.HasValue).Select(fs => fs.LastSuccessAt!.Value).ToList();
                if (successDates.Any())
                {
                    lastBlogSync = successDates.Max();
                }
                feedsWithErrors = activeFeeds.Count(fs => fs.LastSyncError != null);
            }
        }
        catch
        {
            dbStatus = "Unhealthy";
        }

        // Calculate Sync Status (Healthy, Degraded, Failed) based on thresholds
        string syncStatus = "Healthy";
        if (dbStatus == "Unhealthy" || globalSyncError != null)
        {
            syncStatus = "Failed";
        }
        else if (tenantsWithErrors > 0 || feedsWithErrors > 0)
        {
            syncStatus = "Degraded";
        }
        else if (totalMonitors > 0 && monitorsWithErrors > 0)
        {
            double errorRate = (double)monitorsWithErrors / totalMonitors;
            if (errorRate >= 0.15)
            {
                syncStatus = "Failed";
            }
            else
            {
                syncStatus = "Degraded";
            }
        }

        // Calculate Hangfire Status (Healthy, Warning, Failed)
        string hangfireStatus = "Healthy";
        long failedCount = 0;
        long processingCount = 0;
        long enqueuedCount = 0;
        long scheduledCount = 0;

        try
        {
            var monitorApi = JobStorage.Current.GetMonitoringApi();
            var stats = await Task.Run(() => monitorApi.GetStatistics(), ct);
            failedCount = stats.Failed;
            processingCount = stats.Processing;
            enqueuedCount = stats.Enqueued;
            scheduledCount = stats.Scheduled;
            
            var failedJobs = await Task.Run(() => monitorApi.FailedJobs(0, 15), ct);
            var recentFailures = failedJobs.Count(j => j.Value.FailedAt.HasValue && DateTime.UtcNow - j.Value.FailedAt.Value < TimeSpan.FromHours(24));

            if (recentFailures > 5)
            {
                hangfireStatus = "Failed";
            }
            else if (recentFailures > 0)
            {
                hangfireStatus = "Warning";
            }
        }
        catch
        {
            hangfireStatus = "Failed";
        }

        return new SystemHealthDto(
            DatabaseStatus: dbStatus,
            Hangfire: new HangfireHealthDto(
                Status: hangfireStatus,
                FailedCount: failedCount,
                ProcessingCount: processingCount,
                EnqueuedCount: enqueuedCount,
                ScheduledCount: scheduledCount
            ),
            Sync: new SyncHealthDto(
                Status: syncStatus,
                TenantsWithErrorsCount: tenantsWithErrors,
                MonitorsWithErrorsCount: monitorsWithErrors,
                FeedsWithErrorsCount: feedsWithErrors,
                GlobalSyncError: globalSyncError
            ),
            LastLitiumSync: lastLitiumSync,
            LastBlogSync: lastBlogSync,
            LastFleetUpdate: lastFleetUpdate,
            LastFleetUptimeUpdate: lastFleetUptimeUpdate,
            LastFleetLatencyUpdate: lastFleetLatencyUpdate
        );
    }
}
