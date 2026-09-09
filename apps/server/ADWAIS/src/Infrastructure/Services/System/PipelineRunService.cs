// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Adwais.Infrastructure.Services;

public sealed class PipelineRunService(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    ILogger<PipelineRunService> logger) : IPipelineRunService
{
    public async Task<PipelineRun> StartAsync(
        Guid organizationId,
        Guid? tenantId,
        PipelineKind kind,
        PipelineTriggerKind trigger,
        string? resourceKey = null,
        string? resourceName = null,
        Guid? actorUserId = null,
        string? requestId = null,
        string? traceId = null,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var run = new PipelineRun
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TenantId = tenantId,
            Kind = kind,
            Trigger = trigger,
            ResourceKey = Trim(resourceKey, 200),
            ResourceName = Trim(resourceName, 255),
            ActorUserId = actorUserId,
            RequestId = Trim(requestId, 200),
            TraceId = Trim(traceId, 64),
            RequestedAt = now,
            LastStateChangedAt = now,
            State = PipelineRunState.Pending
        };

        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            db.PipelineRuns.Add(run);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // A failed diagnostics write must never turn a successful business
            // operation into a retry. Keep the original failure in the logger.
            logger.LogError(ex, "Could not persist pipeline run {RunId} for organization {OrganizationId}.",
                run.Id, organizationId);
        }

        return run;
    }

    public Task MarkRunningAsync(Guid runId, CancellationToken ct = default)
        => UpdateAsync(runId, run =>
        {
            var now = DateTimeOffset.UtcNow;
            run.State = PipelineRunState.Running;
            run.StartedAt ??= now;
            run.LastStateChangedAt = now;
            run.AttemptCount++;
            run.TraceId ??= Trim(Activity.Current?.TraceId.ToHexString(), 64);
            run.CompletedAt = null;
            run.NextRetryAt = null;
            run.OutcomeCode = null;
            run.SafeSummary = null;
        }, ct);

    public Task AttachHangfireJobAsync(Guid runId, string jobId, CancellationToken ct = default)
        => UpdateAsync(runId, run =>
        {
            run.HangfireJobId = Trim(jobId, 100);
            if (run.State == PipelineRunState.Pending)
            {
                run.State = PipelineRunState.Queued;
                run.LastStateChangedAt = DateTimeOffset.UtcNow;
            }
        }, ct);

    public Task CompleteAsync(Guid runId, int? workCount, string safeSummary, CancellationToken ct = default)
        => UpdateAsync(runId, run =>
        {
            var now = DateTimeOffset.UtcNow;
            run.State = PipelineRunState.Succeeded;
            run.CompletedAt = now;
            run.LastStateChangedAt = now;
            run.WorkCount = workCount;
            run.OutcomeCode = "succeeded";
            run.SafeSummary = Trim(safeSummary, 500);
        }, ct);

    public Task FailAsync(Guid runId, string outcomeCode, string safeSummary, CancellationToken ct = default)
        => UpdateAsync(runId, run =>
        {
            var now = DateTimeOffset.UtcNow;
            run.State = PipelineRunState.Failed;
            run.CompletedAt = now;
            run.LastStateChangedAt = now;
            run.OutcomeCode = Trim(outcomeCode, 100);
            run.SafeSummary = Trim(safeSummary, 500);
        }, ct);

    private async Task UpdateAsync(Guid runId, Action<PipelineRun> update, CancellationToken ct)
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync(ct);
            var run = await db.PipelineRuns.SingleOrDefaultAsync(candidate => candidate.Id == runId, ct);
            if (run is null) return;
            update(run);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not update pipeline run {RunId}.", runId);
        }
    }

    private static string? Trim(string? value, int maxLength)
        => value is null ? null : value.Length <= maxLength ? value : value[..maxLength];
}
