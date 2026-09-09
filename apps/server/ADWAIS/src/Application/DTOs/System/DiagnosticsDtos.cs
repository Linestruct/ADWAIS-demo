// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;

namespace Adwais.Application.DTOs.System;

public record OrganizationDiagnosticsDto(
    Guid OrganizationId,
    string OrganizationName,
    DateTimeOffset ObservedAt,
    int PipelineCount,
    int HealthyCount,
    int AttentionCount,
    int RunningCount,
    IReadOnlyList<PipelineStatusDto> Pipelines);

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

public record PipelineRunDetailsDto(
    PipelineRunDto Run,
    IReadOnlyList<DiagnosticEventDto> Events);

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

public record PlatformDiagnosticsDto(
    DateTimeOffset ObservedAt,
    SystemHealthDto Health,
    IReadOnlyList<OrganizationIssueSummaryDto> AffectedOrganizations);

public record OrganizationIssueSummaryDto(
    Guid OrganizationId,
    string OrganizationName,
    int FailedRuns,
    int ActiveRuns,
    int RecentEvents);
