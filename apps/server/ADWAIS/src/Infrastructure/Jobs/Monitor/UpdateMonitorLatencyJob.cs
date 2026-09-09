// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Infrastructure.Persistence;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Domain.Entities;
using Adwais.Application.Common.Caching;
using Adwais.Application.Common.Observability;
using Adwais.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Adwais.Infrastructure.Jobs.Monitor;

public class UpdateMonitorLatencyJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IEnumerable<IMonitoringProvider> monitoringProviders,
    IMemoryCache cache,
    ISystemEventService eventService,
    IViewRefreshTracker viewRefreshTracker,
    IPipelineRunService? pipelineRunService = null) : IOrgScopedJob
{
    public async Task ExecuteAsync(Guid organizationId, int monitorId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var currentStep = "Initializing Database Connection";
        PipelineRun? run = null;
        Guid? monitorTenantId = null;
        var startedAt = Stopwatch.GetTimestamp();
        var outcome = "succeeded";
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

            monitorTenantId = monitor.TenantId;

            if (monitorId <= 0) return;
            using var activity = ObservabilityTelemetry.ActivitySource.StartActivity(
                "adwais.pipeline.monitor_sync", ActivityKind.Internal);
            activity?.SetTag("adwais.organization_id", organizationId);
            activity?.SetTag("adwais.tenant_id", monitor.TenantId);
            activity?.SetTag("adwais.pipeline", PipelineKind.MonitorSync.ToString());
            activity?.SetTag("adwais.resource_id", monitorId);
            if (pipelineRunService is not null)
            {
                run = await pipelineRunService.StartAsync(
                    organizationId,
                    monitor.TenantId,
                    PipelineKind.MonitorSync,
                    PipelineTriggerKind.Scheduled,
                    resourceKey: monitorId.ToString(),
                    resourceName: monitor.Name,
                    traceId: Activity.Current?.TraceId.ToHexString());
                await pipelineRunService.MarkRunningAsync(run.Id);
                activity?.SetTag("adwais.pipeline_run_id", run.Id);
            }
            ObservabilityTelemetry.PipelineAttempts.Add(1,
                new KeyValuePair<string, object?>("pipeline", "monitor_sync"));
            var monitoringProvider = monitoringProviders.ForProvider(monitor.Provider);
            currentStep = "Fetching response latency time-series from monitoring provider";
            (int? Average, int? Lowest, int? Highest) responseTime;
            try
            {
                responseTime = await monitoringProvider.GetResponseTimeAsync(
                    monitor.Tenant!.OrganizationId, monitor.ExternalId, startDate, endDate, monitor.Name);
                ObservabilityTelemetry.ProviderOutcomes.Add(1,
                    new KeyValuePair<string, object?>("provider", monitoringProvider.Provider),
                    new KeyValuePair<string, object?>("outcome", "succeeded"));
            }
            catch
            {
                ObservabilityTelemetry.ProviderOutcomes.Add(1,
                    new KeyValuePair<string, object?>("provider", monitoringProvider.Provider),
                    new KeyValuePair<string, object?>("outcome", "failed"));
                throw;
            }

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
            if (run is not null)
                await pipelineRunService!.CompleteAsync(run.Id, null, "Monitor latency synchronization completed.");
        }
        catch (Exception ex)
        {
            outcome = "failed";
            var detailedErrorMessage = $"Failed during step '{currentStep}': {ex.Message}";
            try
            {
                if (pipelineRunService is null)
                {
                    // Preserve the legacy direct-call contract for callers
                    // that have not opted into run tracking yet.
                    await eventService.LogErrorAsync(nameof(UpdateMonitorLatencyJob), detailedErrorMessage, ex, tenantId: null);
                }
                else
                {
                    await eventService.LogErrorAsync(
                        nameof(UpdateMonitorLatencyJob),
                        "Monitor synchronization failed for this organization.",
                        ex,
                        tenantId: monitorTenantId,
                        organizationId: organizationId,
                        code: "pipeline.failed",
                        audience: SystemEventAudience.Organization,
                        pipelineRunId: run?.Id,
                        traceId: Activity.Current?.TraceId.ToHexString(),
                        suggestedAction: "Review the monitoring configuration and retry synchronization.");
                }
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
            if (run is not null)
                await pipelineRunService!.FailAsync(
                    run.Id,
                    "pipeline.failed",
                    "Monitor synchronization failed.",
                    CancellationToken.None);
            throw;
        }
        finally
        {
            if (run is not null)
            {
                ObservabilityTelemetry.PipelineOutcomes.Add(1,
                    new KeyValuePair<string, object?>("pipeline", "monitor_sync"),
                    new KeyValuePair<string, object?>("outcome", outcome));
                ObservabilityTelemetry.PipelineDuration.Record(
                    Stopwatch.GetElapsedTime(startedAt).TotalSeconds,
                    new KeyValuePair<string, object?>("pipeline", "monitor_sync"));
            }
        }
    }
}
