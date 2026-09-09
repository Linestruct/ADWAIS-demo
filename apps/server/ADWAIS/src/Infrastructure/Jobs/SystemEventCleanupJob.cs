// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Adwais.Infrastructure.Jobs;

public class SystemEventCleanupJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    ILogger<SystemEventCleanupJob> logger)
{
    public async Task ExecuteAsync()
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        var config = await db.GlobalConfigs.AsNoTracking().SingleOrDefaultAsync();
        
        var retentionDays = Math.Max(1, config?.SystemEventRetentionDays ?? 30);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        logger.LogInformation("Starting SystemEvent cleanup. Removing events older than {RetentionDays} days (Cutoff: {Cutoff})", 
            retentionDays, cutoff);

        try
        {
            var deletedCount = await db.SystemEvents
                .Where(e => e.Timestamp < cutoff)
                .ExecuteDeleteAsync();

            // Keep active or unresolved runs available for reconciliation. A
            // completed run is historical once its completion is outside the
            // same retention window as events.
            var deletedRunCount = await db.PipelineRuns
                .Where(run => run.CompletedAt != null
                    && run.CompletedAt < cutoff
                    && run.State != Domain.Entities.PipelineRunState.Pending
                    && run.State != Domain.Entities.PipelineRunState.Queued
                    && run.State != Domain.Entities.PipelineRunState.Running
                    && run.State != Domain.Entities.PipelineRunState.RetryScheduled
                    && run.State != Domain.Entities.PipelineRunState.Unknown)
                .ExecuteDeleteAsync();

            logger.LogInformation("Successfully deleted {EventCount} old system events and {RunCount} completed pipeline runs.",
                deletedCount, deletedRunCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to perform SystemEvent cleanup.");
        }
    }
}


