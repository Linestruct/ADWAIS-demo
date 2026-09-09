// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Persistence;
using Adwais.Domain.Entities;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Adwais.Infrastructure.Jobs;

/// <summary>
/// Enqueues order ingestion jobs for one organization's eligible tenants.
/// Runs on the organization's own recurring cadence.
/// </summary>
public class OrderFetchDispatchJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IBackgroundJobClient backgroundJobClient,
    ILogger<OrderFetchDispatchJob> logger,
    ISystemEventService eventService,
    IPipelineRunService? pipelineRunService = null) : IOrgScopedJob
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(60);
    private static readonly TimeSpan MaxAutomatedGap = TimeSpan.FromDays(31);

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(Guid organizationId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var orgConfig = await db.OrganizationConfigs
            .SingleOrDefaultAsync(c => c.OrganizationId == organizationId);
        if (orgConfig is { OrderFetchEnabled: false }) return;

        var tenants = await db.Tenants
            .Where(t => t.OrganizationId == organizationId
                && t.OrderFetchingEnabled && !t.IsSystem
                && t.OrderProviderSettings != null)
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        var dispatched = 0;

        foreach (var tenant in tenants)
        {
            if (tenant.CurrentlyFetching && tenant.LastPolled.HasValue
                && now - tenant.LastPolled.Value > StaleThreshold)
            {
                await eventService.LogWarningAsync(
                    nameof(OrderFetchDispatchJob),
                    "A stale order-ingestion state was reset for this tenant.",
                    details: null,
                    tenantId: tenant.Id,
                    organizationId: organizationId,
                    code: "pipeline.stale-state-reset",
                    audience: SystemEventAudience.Organization,
                    suggestedAction: "Review the latest ingestion run if the tenant remains stale.");
                tenant.CurrentlyFetching = false;
            }

            if (tenant.CurrentlyFetching) continue;

            var start = tenant.FetchedUntil ?? now.AddDays(-2);

            if (now - start > MaxAutomatedGap)
            {
                await eventService.LogWarningAsync(
                    nameof(OrderFetchDispatchJob),
                    "Automated order ingestion was skipped because the data gap is too large.",
                    details: null,
                    tenantId: tenant.Id,
                    organizationId: organizationId,
                    code: "pipeline.gap-too-large",
                    audience: SystemEventAudience.Organization,
                    suggestedAction: "Use the manual backfill action to recover this tenant.");
                continue;
            }

            var pipelineRun = pipelineRunService is null
                ? null
                : await pipelineRunService.StartAsync(
                    organizationId,
                    tenant.Id,
                    PipelineKind.OrderIngestion,
                    PipelineTriggerKind.Scheduled,
                    resourceKey: tenant.Id.ToString("D"),
                    resourceName: tenant.Name);

            try
            {
                if (pipelineRun is null)
                {
                    backgroundJobClient.Enqueue<IOrderIngestionService>(
                        ingestion => ingestion.ExecuteIngestionAsync(organizationId, tenant.Id, start, now, CancellationToken.None));
                }
                else
                {
                    var jobId = backgroundJobClient.Enqueue<IOrderIngestionService>(
                        ingestion => ingestion.ExecuteIngestionTrackedAsync(organizationId, tenant.Id, start, now, pipelineRun.Id, CancellationToken.None));
                    await pipelineRunService!.AttachHangfireJobAsync(pipelineRun.Id, jobId, CancellationToken.None);
                }
            }
            catch
            {
                if (pipelineRun is not null)
                    await pipelineRunService!.FailAsync(pipelineRun.Id, "dispatch.failed", "The ingestion job could not be queued.", CancellationToken.None);
                throw;
            }

            tenant.CurrentlyFetching = true;
            tenant.LastPolled = now;
            dispatched++;
        }

        await db.SaveChangesAsync();

        logger.LogInformation("Order fetch dispatch complete for org {OrgId}. Enqueued {Dispatched}/{Total} tenants.",
            organizationId, dispatched, tenants.Count);
    }
}
