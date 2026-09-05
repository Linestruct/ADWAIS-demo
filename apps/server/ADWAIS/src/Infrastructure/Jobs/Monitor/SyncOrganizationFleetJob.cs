// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using Adwais.Application.Interfaces;
using Adwais.Application.Common.Jobs;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Hangfire.Storage;
using Adwais.Application.Common.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Cronos;

namespace Adwais.Infrastructure.Jobs.Monitor;

/// <summary>
/// Syncs one organization's monitor fleet with its upstream provider and
/// refreshes the live monitor state cache. A single org's failure is
/// isolated to this job and retried by Hangfire.
/// </summary>
public class SyncOrganizationFleetJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IEnumerable<IMonitoringProvider> monitoringProviders,
    IMemoryCache cache,
    IRecurringJobManager recurringJobManager) : IOrgScopedJob
{
    protected virtual string? CurrentSyncCron => JobStorage.Current.GetConnection().GetRecurringJobs()
        .SingleOrDefault(j => j.Id == RecurringJobId.For(RecurringJobKind.FleetSync, OrganizationIdOfLastRun))?.Cron;

    private Guid OrganizationIdOfLastRun { get; set; } = Guid.Empty;

    public async Task ExecuteAsync(Guid organizationId)
    {
        OrganizationIdOfLastRun = organizationId;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var orgConfig = await dbContext.OrganizationConfigs
            .SingleOrDefaultAsync(config => config.OrganizationId == organizationId
                && config.MonitoringProviderSettings != null
                && config.MonitoringFetchEnabled);
        if (orgConfig is null) return;

        var monitoringProvider = monitoringProviders.ForProvider(orgConfig.MonitoringProvider);
        var upStreamMonitors = await monitoringProvider.GetMonitorsAsync(organizationId);

        var bucketId = await dbContext.Tenants
            .Where(t => t.OrganizationId == organizationId && t.IsSystem)
            .Select(t => (Guid?)t.Id)
            .SingleOrDefaultAsync()
            ?? throw new InvalidOperationException($"Organization {organizationId} has no unassigned monitor bucket.");

        var localMonitors = await dbContext.Monitors
            .Include(m => m.Tenant)
            .Where(monitor => monitor.Provider == monitoringProvider.Provider
                && (monitor.TenantId == bucketId || monitor.Tenant!.OrganizationId == organizationId))
            .ToListAsync();
        var localByExternalId = localMonitors
            .GroupBy(monitor => monitor.ExternalId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

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
                    TenantId = bucketId,
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

        await dbContext.SaveChangesAsync();

        var lowestIntervalSeconds = upStreamMonitors.Count > 0
            ? upStreamMonitors.Min(m => m.UpdateInterval)
            : 300;
        var lowestIntervalMins = Math.Max(1, lowestIntervalSeconds / 60);
        recurringJobManager.AddOrUpdate<SyncOrganizationFleetJob>(
            RecurringJobId.For(RecurringJobKind.FleetSync, organizationId),
            job => job.ExecuteAsync(organizationId),
            Cron.MinuteInterval(lowestIntervalMins));

        var cacheDuration = ResolveCacheDuration();

        foreach (var (monitor, status) in liveStates)
        {
            var existing = cache.TryGetValue(GlobalCacheKeys.MonitorState(monitor.Id), out LiveMonitorState? state) ? state : null;
            cache.Set(
                GlobalCacheKeys.MonitorState(monitor.Id),
                new LiveMonitorState(status, existing?.CurrentLatency),
                cacheDuration);
        }
    }

    private TimeSpan ResolveCacheDuration()
    {
        var cronExpression = CurrentSyncCron;
        if (string.IsNullOrWhiteSpace(cronExpression)) return TimeSpan.FromMinutes(6);

        try
        {
            var cron = CronExpression.Parse(cronExpression, CronFormat.Standard);
            var nextRun = cron.GetNextOccurrence(DateTime.UtcNow);
            if (nextRun.HasValue)
            {
                return nextRun.Value - DateTime.UtcNow + TimeSpan.FromMinutes(1);
            }
        }
        catch (CronFormatException) { }

        return TimeSpan.FromMinutes(6);
    }
}
