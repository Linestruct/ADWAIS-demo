// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Infrastructure.Persistence;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Application.Common.Caching;
using Adwais.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Adwais.Infrastructure.Jobs.Monitor;

public class UpdateMonitorLatencyJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IEnumerable<IMonitoringProvider> monitoringProviders,
    IMemoryCache cache,
    ISystemEventService eventService,
    IViewRefreshTracker viewRefreshTracker)
{
    public async Task ExecuteAsync(Guid organizationId, int monitorId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var currentStep = "Initializing Database Connection";
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            
            currentStep = $"Fetching Monitor metadata for MonitorId {monitorId}";
            var monitor = await dbContext.Monitors
                .Include(m => m.Tenant)
                .FirstOrDefaultAsync(m => m.Id == monitorId);

            if (monitor == null || !monitor.UptimeMonitorEnabled) return;

            if (monitor.Tenant!.OrganizationId != organizationId)
                throw new InvalidOperationException($"Monitor {monitorId} does not belong to organization {organizationId}.");

            if (monitorId <= 0) return;
            var monitoringProvider = monitoringProviders.ForProvider(monitor.Provider);
            currentStep = "Fetching response latency time-series from monitoring provider";
            var responseTime = await monitoringProvider.GetResponseTimeAsync(
                monitor.Tenant!.OrganizationId, monitor.ExternalId, startDate, endDate, monitor.Name);

            if (responseTime.Average.HasValue)
            {
                currentStep = "Saving ResponseTime measurements to database";
                dbContext.ResponseTimes.Add(new ResponseTime
                {
                    MonitorId = monitorId,
                    Average = responseTime.Average.Value,
                    Lowest = responseTime.Lowest,
                    Highest = responseTime.Highest,
                    Date = endDate
                });

                currentStep = "Updating local memory cache state";
                var orgConfig = await dbContext.OrganizationConfigs.AsNoTracking()
                    .SingleOrDefaultAsync(c => c.OrganizationId == monitor.Tenant!.OrganizationId);
                var intervalMins = orgConfig?.LatencyFetchIntervalMinutes ?? 10;

                var existing = cache.TryGetValue(GlobalCacheKeys.MonitorState(monitorId), out LiveMonitorState? state) ? state : null;
                cache.Set(
                    GlobalCacheKeys.MonitorState(monitorId),
                    new LiveMonitorState(existing?.StatusStr ?? monitor.StatusStr, (double)responseTime.Average.Value),
                    TimeSpan.FromMinutes(intervalMins * 2)
                );
            }

            currentStep = "Updating Monitor metadata and clearing sync errors";
            monitor.LastLatencyUpdate = endDate;
            monitor.LastSyncError = null;
            await dbContext.SaveChangesAsync();
            if (responseTime.Average.HasValue)
            {
                await viewRefreshTracker.MarkDirtyAsync(monitor.Tenant!.OrganizationId, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            var detailedErrorMessage = $"Failed during step '{currentStep}': {ex.Message}";
            try
            {
                await eventService.LogErrorAsync(nameof(UpdateMonitorLatencyJob), detailedErrorMessage, ex, tenantId: null);
            }
            catch
            {
                // Suppress logging service failure
            }

            try
            {
                await using var errorContext = await dbContextFactory.CreateDbContextAsync(CancellationToken.None);
                var monitor = await errorContext.Monitors.FirstOrDefaultAsync(m => m.Id == monitorId);
                if (monitor != null)
                {
                    monitor.LastSyncError = detailedErrorMessage;
                    await errorContext.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch
            {
                // Suppress nested DB update failure
            }
            throw;
        }
    }
}
