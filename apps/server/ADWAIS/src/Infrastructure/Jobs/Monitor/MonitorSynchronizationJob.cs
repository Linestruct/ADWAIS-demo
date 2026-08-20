// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Infrastructure.Persistence;
using Cronos;
using Adwais.Domain.Entities.Monitoring;
using Hangfire;
using Hangfire.Storage;
using Adwais.Application.Common.Caching;
using Adwais.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Adwais.Infrastructure.Jobs.Monitor;

public class MonitorSynchronizationJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IEnumerable<IMonitoringProvider> monitoringProviders,
    IMemoryCache cache,
    IRecurringJobManager recurringJobManager)
{
    public async Task ExecuteAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var globalConfig = await dbContext.GlobalConfigs.SingleOrDefaultAsync();
        if (globalConfig == null
            || string.IsNullOrWhiteSpace(globalConfig.MonitoringProviderSettings)
            || !globalConfig.MonitoringFetchEnabled)
        {
            return;
        }
        var monitoringProvider = monitoringProviders.ForProvider(globalConfig.MonitoringProvider);
        
        var upStreamMonitors = await monitoringProvider.GetMonitorsAsync();
        
        var lowestIntervalMins = upStreamMonitors.Any() 
            ? Math.Max(1, upStreamMonitors.Min(m => m.UpdateInterval) / 60)
            : 5;
        recurringJobManager.AddOrUpdate<MonitorSynchronizationJob>("sync-monitoring-fleet", job => job.ExecuteAsync(), Cron.MinuteInterval(lowestIntervalMins));

        var localMonitors = await dbContext.Monitors
            .Where(monitor => monitor.Provider == monitoringProvider.Provider)
            .ToListAsync();
        var localByExternalId = localMonitors.ToDictionary(monitor => monitor.ExternalId, StringComparer.Ordinal);
        var cronExpression = JobStorage.Current.GetConnection().GetRecurringJobs()
            .SingleOrDefault(j => j.Id == "sync-monitoring-fleet")?.Cron;
        
        TimeSpan cacheDuration = TimeSpan.FromMinutes(6);
        
        if (!string.IsNullOrWhiteSpace(cronExpression))
        {
            try
            {
                var cron = CronExpression.Parse(cronExpression, CronFormat.Standard);
                var nextRun = cron.GetNextOccurrence(DateTime.UtcNow);
            
                if (nextRun.HasValue)
                {
                    cacheDuration = nextRun.Value - DateTime.UtcNow + TimeSpan.FromMinutes(1);
                    Console.WriteLine("Set cache duration to {0} minute", cacheDuration.Minutes);
                }
            }
            catch (CronFormatException) { }
        }
        
        var liveStates = new List<(UptimeMonitor Monitor, string Status)>();
        foreach (var remote in upStreamMonitors)
        {
            if (localByExternalId.TryGetValue(remote.ExternalId, out var local))
            {
                local.Type = remote.Type;
                local.Name = remote.Name;
                local.Url = remote.Url;
                local.UpdateInterval = remote.UpdateInterval;
                local.HttpMethod = remote.HttpMethod;
                local.TimeoutSeconds = remote.TimeoutSeconds;
                local.SslExpiresAt = remote.SslExpiresAt;
                local.DomainExpiresAt = remote.DomainExpiresAt;
                local.MonitoredRegions = remote.MonitoredRegions ?? [];
                local.CurrentStateDurationSeconds = remote.CurrentStateDurationSeconds;
                local.LastIncidentId = remote.LastIncident?.ExternalId;
                local.LastIncidentStatus = remote.LastIncident?.Status;
                local.LastIncidentCause = remote.LastIncident?.Cause;
                local.LastIncidentReason = remote.LastIncident?.Reason;
                local.LastIncidentStartedAt = remote.LastIncident?.StartedAt;
                local.LastIncidentDurationSeconds = remote.LastIncident?.DurationSeconds;
                local.CreatedDate = remote.CreatedDate;
                local.StatusStr = remote.Status;
                local.LastUpdate = DateTimeOffset.UtcNow;
                local.Tags = remote.Tags;
            }
            else
            {
                var monitorState = !remote.Status.Equals("PAUSED");
                local = new UptimeMonitor
                {
                    TenantId = AnalyticsDbContext.SystemTenantGuid,
                    Provider = monitoringProvider.Provider,
                    ExternalId = remote.ExternalId,
                    Type = remote.Type,
                    Name = remote.Name,
                    Url = remote.Url,
                    UpdateInterval = remote.UpdateInterval,
                    HttpMethod = remote.HttpMethod,
                    TimeoutSeconds = remote.TimeoutSeconds,
                    SslExpiresAt = remote.SslExpiresAt,
                    DomainExpiresAt = remote.DomainExpiresAt,
                    MonitoredRegions = remote.MonitoredRegions ?? [],
                    CurrentStateDurationSeconds = remote.CurrentStateDurationSeconds,
                    LastIncidentId = remote.LastIncident?.ExternalId,
                    LastIncidentStatus = remote.LastIncident?.Status,
                    LastIncidentCause = remote.LastIncident?.Cause,
                    LastIncidentReason = remote.LastIncident?.Reason,
                    LastIncidentStartedAt = remote.LastIncident?.StartedAt,
                    LastIncidentDurationSeconds = remote.LastIncident?.DurationSeconds,
                    UptimeMonitorEnabled = monitorState,
                    CreatedDate = remote.CreatedDate,
                    StatusStr = remote.Status,
                    LastUpdate = DateTimeOffset.UtcNow,
                    Tags = remote.Tags
                };
                dbContext.Monitors.Add(local);
            }

            liveStates.Add((local, remote.Status));
        }

        var upStreamIds = upStreamMonitors.Select(monitor => monitor.ExternalId).ToHashSet(StringComparer.Ordinal);
        var toDelete = localMonitors.Where(monitor => !upStreamIds.Contains(monitor.ExternalId));
        
        dbContext.Monitors.RemoveRange(toDelete);

        await dbContext.SaveChangesAsync();

        foreach (var (monitor, status) in liveStates)
        {
            var existing = cache.TryGetValue(GlobalCacheKeys.MonitorState(monitor.Id), out LiveMonitorState? state) ? state : null;
            cache.Set(
                GlobalCacheKeys.MonitorState(monitor.Id),
                new LiveMonitorState(status, existing?.CurrentLatency),
                cacheDuration);
        }
    }
}


