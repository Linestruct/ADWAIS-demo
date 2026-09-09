// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;

namespace Adwais.Application.Interfaces;

/// <summary>
/// Records the small amount of application-owned history needed by the
/// diagnostics API. It is deliberately not a scheduler or retry abstraction.
/// </summary>
public interface IPipelineRunService
{
    Task<PipelineRun> StartAsync(
        Guid organizationId,
        Guid? tenantId,
        PipelineKind kind,
        PipelineTriggerKind trigger,
        string? resourceKey = null,
        string? resourceName = null,
        Guid? actorUserId = null,
        string? requestId = null,
        string? traceId = null,
        CancellationToken ct = default);

    Task MarkRunningAsync(Guid runId, CancellationToken ct = default);

    Task AttachHangfireJobAsync(Guid runId, string jobId, CancellationToken ct = default);

    Task CompleteAsync(Guid runId, int? workCount, string safeSummary, CancellationToken ct = default);

    Task FailAsync(Guid runId, string outcomeCode, string safeSummary, CancellationToken ct = default);
}
