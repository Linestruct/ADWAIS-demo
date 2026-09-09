// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Adwais.Application.Common.Observability;
using Adwais.Domain.Entities;
using Adwais.Application.DTOs.Monitoring.Upstream;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Adwais.Infrastructure.Jobs.Monitor;

/// <summary>
/// Syncs one organization's monitor fleet with its upstream provider.
/// A single org's failure is isolated to this job and retried by Hangfire.
/// </summary>
public class SyncOrganizationAccountStatsJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IEnumerable<IMonitoringProvider> monitoringProviders,
    ILogger<SyncOrganizationAccountStatsJob> logger,
    ISystemEventService eventService,
    IPipelineRunService? pipelineRunService = null) : IOrgScopedJob
{
    public async Task ExecuteAsync(Guid organizationId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var orgConfig = await db.OrganizationConfigs
            .SingleOrDefaultAsync(c => c.OrganizationId == organizationId
                && c.MonitoringProviderSettings != null
                && c.MonitoringFetchEnabled);
        if (orgConfig is null) return;

        var startedAt = Stopwatch.GetTimestamp();
        var run = pipelineRunService is null
            ? null
            : await pipelineRunService.StartAsync(
                organizationId,
                tenantId: null,
                PipelineKind.AccountStats,
                PipelineTriggerKind.Scheduled,
                resourceKey: "account",
                resourceName: "Monitoring account statistics");
        using var activity = ObservabilityTelemetry.ActivitySource.StartActivity(
            "adwais.pipeline.account_stats", ActivityKind.Internal);
        activity?.SetTag("adwais.organization_id", organizationId);
        activity?.SetTag("adwais.pipeline", PipelineKind.AccountStats.ToString());
        activity?.SetTag("adwais.pipeline_run_id", run?.Id);
        if (run is not null)
            await pipelineRunService!.MarkRunningAsync(run.Id);
        ObservabilityTelemetry.PipelineAttempts.Add(1,
            new KeyValuePair<string, object?>("pipeline", "account_stats"));
        var outcome = "succeeded";

        try
        {
            var monitoringProvider = monitoringProviders.ForProvider(orgConfig.MonitoringProvider);
            MonitoringProviderAccount user;
            try
            {
                user = await monitoringProvider.GetAccountDetailsAsync(organizationId);
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
            orgConfig.MonitorsCount = user.MonitorsCount;
            orgConfig.MonitorsLimit = user.MonitorLimit;
            orgConfig.ActiveSubscription = user.ActiveSubscriptionPlan;
            orgConfig.LastSyncError = null;
            await db.SaveChangesAsync();
            if (run is not null)
                await pipelineRunService!.CompleteAsync(run.Id, null, "Monitoring account statistics synchronized.");
            logger.LogInformation("Updated monitoring account stats for org {OrgId}: {Count}/{Limit} ({Sub})",
                organizationId, user.MonitorsCount, user.MonitorLimit, user.ActiveSubscriptionPlan);
        }
        catch (Exception ex)
        {
            outcome = "failed";
            var detailedErrorMessage = $"Failed to update monitoring account stats for org {organizationId}: {ex.Message}";
            logger.LogError(ex, "Monitoring account statistics failed for organization {OrganizationId}, run {RunId}.", organizationId, run?.Id);
            try
            {
                await eventService.LogErrorAsync(
                    nameof(SyncOrganizationAccountStatsJob),
                    "Monitoring account statistics failed for this organization.",
                    ex,
                    organizationId: organizationId,
                    code: "pipeline.failed",
                    audience: SystemEventAudience.Organization,
                    pipelineRunId: run?.Id,
                    traceId: Activity.Current?.TraceId.ToHexString(),
                    suggestedAction: "Review the monitoring configuration and retry synchronization.");
            }
            catch
            {
                // Suppress logging service failure
            }

            try
            {
                orgConfig.LastSyncError = detailedErrorMessage;
                await db.SaveChangesAsync();
            }
            catch
            {
                // Suppress nested DB update failure
            }
            if (run is not null)
                await pipelineRunService!.FailAsync(
                    run.Id,
                    "pipeline.failed",
                    "Monitoring account statistics failed.",
                    CancellationToken.None);
        }
        finally
        {
            if (run is not null)
            {
                ObservabilityTelemetry.PipelineOutcomes.Add(1,
                    new KeyValuePair<string, object?>("pipeline", "account_stats"),
                    new KeyValuePair<string, object?>("outcome", outcome));
                ObservabilityTelemetry.PipelineDuration.Record(
                    Stopwatch.GetElapsedTime(startedAt).TotalSeconds,
                    new KeyValuePair<string, object?>("pipeline", "account_stats"));
            }
        }
    }
}
