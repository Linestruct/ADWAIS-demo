// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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
        if (globalConfig == null || !globalConfig.MonitoringFetchEnabled)
        {
            return;
        }

        var orgConfigs = await dbContext.OrganizationConfigs
            .Where(config => config.MonitoringProviderSettings != null)
            .ToListAsync();

        var liveStates = new List<(UptimeMonitor Monitor, string Status)>();
        int? lowestUpstreamInterval = null;

        foreach (var orgConfig in orgConfigs)
        {
            var monitoringProvider = monitoringProviders.ForProvider(orgConfig.MonitoringProvider);

            var upStreamMonitors = await monitoringProvider.GetMonitorsAsync(orgConfig.OrganizationId);
            if (upStreamMonitors.Count > 0)
            {
                var orgLowest = upStreamMonitors.Min(m => m.UpdateInterval);
                lowestUpstreamInterval = lowestUpstreamInterval is null
                    ? orgLowest
                    : Math.Min(lowestUpstreamInterval.Value, orgLowest);
            }

            var bucketId = await dbContext.Tenants
                .Where(t => t.OrganizationId == orgConfig.OrganizationId && t.IsSystem)
                .Select(t => (Guid?)t.Id)
                .SingleOrDefaultAsync()
                ?? throw new InvalidOperationException($"Organization {orgConfig.OrganizationId} has no unassigned monitor bucket.");

            var localMonitors = await dbContext.Monitors
                .Include(m => m.Tenant)
                .Where(monitor => monitor.Provider == monitoringProvider.Provider
                    && (monitor.TenantId == bucketId || monitor.Tenant!.OrganizationId == orgConfig.OrganizationId))
                .ToListAsync();
            var localByExternalId = localMonitors
                .GroupBy(monitor => monitor.ExternalId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

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
        }

        await dbContext.SaveChangesAsync();

        var lowestIntervalMins = lowestUpstreamInterval is null
            ? 5
            : Math.Max(1, lowestUpstreamInterval.Value / 60);
        recurringJobManager.AddOrUpdate<MonitorSynchronizationJob>("sync-monitoring-fleet", job => job.ExecuteAsync(), Cron.MinuteInterval(lowestIntervalMins));

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