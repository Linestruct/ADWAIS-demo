// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;

namespace Adwais.Application.DTOs.System;

/// <summary>
/// Current pipeline status and aggregate counts for one organization.
/// </summary>
/// <param name="OrganizationId">The organization identifier.</param>
/// <param name="OrganizationName">The organization display name.</param>
/// <param name="ObservedAt">The UTC time at which the status was calculated.</param>
/// <param name="PipelineCount">Total number of tracked pipelines.</param>
/// <param name="HealthyCount">Number of pipelines currently healthy.</param>
/// <param name="AttentionCount">Number of pipelines requiring attention.</param>
/// <param name="RunningCount">Number of pipelines with an active run.</param>
/// <param name="Pipelines">The individual pipeline status projections.</param>
public record OrganizationDiagnosticsDto(
    Guid OrganizationId,
    string OrganizationName,
    DateTimeOffset ObservedAt,
    int PipelineCount,
    int HealthyCount,
    int AttentionCount,
    int RunningCount,
    IReadOnlyList<PipelineStatusDto> Pipelines);

/// <summary>
/// Current operational state of one pipeline.
/// </summary>
/// <param name="Kind">The pipeline kind.</param>
/// <param name="ResourceKey">The stable resource key.</param>
/// <param name="ResourceName">The resource display name.</param>
/// <param name="DisplayName">The display name shown to users.</param>
/// <param name="TenantId">The related tenant identifier, when applicable.</param>
/// <param name="TenantName">The related tenant display name, when applicable.</param>
/// <param name="Enabled">Whether the pipeline is enabled.</param>
/// <param name="Configured">Whether the pipeline has the configuration required to run.</param>
/// <param name="Schedule">The configured schedule, when applicable.</param>
/// <param name="NextExpectedAt">The next expected run time, when known.</param>
/// <param name="State">The current run state.</param>
/// <param name="Freshness">The current data freshness state.</param>
/// <param name="LastAttemptAt">The time of the most recent attempt, when known.</param>
/// <param name="LastSuccessAt">The time of the most recent successful run, when known.</param>
/// <param name="DataThrough">The latest data timestamp covered by the pipeline, when known.</param>
/// <param name="CurrentRunId">The active run identifier, when a run is in progress.</param>
/// <param name="IssueCode">A stable issue code, when attention is required.</param>
/// <param name="IssueSummary">A safe issue description, when attention is required.</param>
/// <param name="SuggestedAction">A suggested remediation, when available.</param>
public record PipelineStatusDto(
    string Kind,
    string ResourceKey,
    string ResourceName,
    string DisplayName,
    Guid? TenantId,
    string? TenantName,
    bool Enabled,
    bool Configured,
    string? Schedule,
    DateTimeOffset? NextExpectedAt,
    string State,
    string Freshness,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? LastSuccessAt,
    DateTimeOffset? DataThrough,
    Guid? CurrentRunId,
    string? IssueCode,
    string? IssueSummary,
    string? SuggestedAction);

/// <summary>
/// A summary projection of one pipeline run.
/// </summary>
/// <param name="Id">The run identifier.</param>
/// <param name="OrganizationId">The owning organization identifier.</param>
/// <param name="OrganizationName">The owning organization display name, when available.</param>
/// <param name="TenantId">The related tenant identifier, when applicable.</param>
/// <param name="TenantName">The related tenant display name, when available.</param>
/// <param name="Kind">The pipeline kind.</param>
/// <param name="Trigger">What initiated the run.</param>
/// <param name="State">The current run state.</param>
/// <param name="ResourceKey">The stable pipeline resource key, when available.</param>
/// <param name="ResourceName">The pipeline resource display name, when available.</param>
/// <param name="RequestedAt">When the run was requested.</param>
/// <param name="StartedAt">When execution started, when known.</param>
/// <param name="LastStateChangedAt">When the run last changed state.</param>
/// <param name="CompletedAt">When execution completed, when known.</param>
/// <param name="NextRetryAt">When a retry is scheduled, when applicable.</param>
/// <param name="AttemptCount">Number of execution attempts.</param>
/// <param name="OutcomeCode">A stable outcome code, when available.</param>
/// <param name="SafeSummary">A safe, user-facing result summary.</param>
/// <param name="WorkCount">Number of work items processed, when known.</param>
/// <param name="RequestId">The request identifier, when available.</param>
/// <param name="TraceId">The distributed trace identifier, when available.</param>
public record PipelineRunDto(
    Guid Id,
    Guid OrganizationId,
    string? OrganizationName,
    Guid? TenantId,
    string? TenantName,
    string Kind,
    string Trigger,
    string State,
    string? ResourceKey,
    string? ResourceName,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset LastStateChangedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? NextRetryAt,
    int AttemptCount,
    string? OutcomeCode,
    string? SafeSummary,
    int? WorkCount,
    string? RequestId,
    string? TraceId);

/// <summary>
/// A pipeline run together with its related diagnostic events.
/// </summary>
/// <param name="Run">The pipeline run summary.</param>
/// <param name="Events">Events associated with the run.</param>
public record PipelineRunDetailsDto(
    PipelineRunDto Run,
    IReadOnlyList<DiagnosticEventDto> Events);

/// <summary>
/// A safe projection of an operational diagnostic event.
/// </summary>
/// <param name="Id">The event identifier.</param>
/// <param name="Timestamp">When the event occurred.</param>
/// <param name="Level">The event severity level.</param>
/// <param name="Code">The stable event code.</param>
/// <param name="Source">The component that emitted the event.</param>
/// <param name="Message">A safe event message.</param>
/// <param name="SuggestedAction">A suggested remediation, when available.</param>
/// <param name="OrganizationId">The related organization identifier, when known.</param>
/// <param name="OrganizationName">The related organization display name, when available.</param>
/// <param name="TenantId">The related tenant identifier, when known.</param>
/// <param name="TenantName">The related tenant display name, when available.</param>
/// <param name="PipelineRunId">The related pipeline run identifier, when known.</param>
/// <param name="RequestId">The request identifier, when available.</param>
/// <param name="TraceId">The distributed trace identifier, when available.</param>
public record DiagnosticEventDto(
    Guid Id,
    DateTimeOffset Timestamp,
    string Level,
    string Code,
    string Source,
    string Message,
    string? SuggestedAction,
    Guid? OrganizationId,
    string? OrganizationName,
    Guid? TenantId,
    string? TenantName,
    Guid? PipelineRunId,
    string? RequestId,
    string? TraceId);

/// <summary>
/// Current platform health and organizations with recent issues.
/// </summary>
/// <param name="ObservedAt">The UTC time at which the status was calculated.</param>
/// <param name="Health">The aggregated platform health state.</param>
/// <param name="AffectedOrganizations">Organizations with failed runs, active runs, or recent events.</param>
public record PlatformDiagnosticsDto(
    DateTimeOffset ObservedAt,
    SystemHealthDto Health,
    IReadOnlyList<OrganizationIssueSummaryDto> AffectedOrganizations);

/// <summary>
/// A compact summary of recent operational issues for one organization.
/// </summary>
/// <param name="OrganizationId">The organization identifier.</param>
/// <param name="OrganizationName">The organization display name.</param>
/// <param name="FailedRuns">Number of failed runs in the reporting window.</param>
/// <param name="ActiveRuns">Number of currently active runs.</param>
/// <param name="RecentEvents">Number of recent diagnostic events.</param>
public record OrganizationIssueSummaryDto(
    Guid OrganizationId,
    string OrganizationName,
    int FailedRuns,
    int ActiveRuns,
    int RecentEvents);
