// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Domain.Entities;

/// <summary>
/// A stable, application-owned record of one logical pipeline invocation.
/// Hangfire remains responsible for scheduling and retries; this record is
/// the history that can safely be shown to an organization.
/// </summary>
public class PipelineRun
{
    public Guid Id { get; set; }
    public required Guid OrganizationId { get; set; }
    public Guid? TenantId { get; set; }
    public PipelineKind Kind { get; set; }
    public PipelineTriggerKind Trigger { get; set; }
    public string? ResourceKey { get; set; }
    public string? ResourceName { get; set; }
    public string? HangfireJobId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? RequestId { get; set; }
    public string? TraceId { get; set; }
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset LastStateChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
    public int AttemptCount { get; set; }
    public PipelineRunState State { get; set; } = PipelineRunState.Pending;
    public string? OutcomeCode { get; set; }
    public string? SafeSummary { get; set; }
    public int? WorkCount { get; set; }

    public Organization? Organization { get; set; }
    public Tenant? Tenant { get; set; }
}

public enum PipelineKind
{
    OrderIngestion,
    FeedRefresh,
    MonitorSync,
    AccountStats
}

public enum PipelineTriggerKind
{
    Manual,
    Scheduled,
    Retry,
    System
}

public enum PipelineRunState
{
    Pending,
    Queued,
    Running,
    RetryScheduled,
    Succeeded,
    Failed,
    Canceled,
    Skipped,
    Unknown
}
