// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;

namespace Adwais.Application.DTOs.System;

public record SystemHealthDto(
    string DatabaseStatus,
    HangfireHealthDto Hangfire,
    SyncHealthDto Sync,
    DateTimeOffset? LastLitiumSync,
    DateTimeOffset? LastBlogSync,
    DateTimeOffset? LastFleetUpdate,
    DateTimeOffset? LastFleetUptimeUpdate,
    DateTimeOffset? LastFleetLatencyUpdate
);

public record HangfireHealthDto(
    string Status,
    long FailedCount,
    long ProcessingCount,
    long EnqueuedCount,
    long ScheduledCount
);

public record SyncHealthDto(
    string Status,
    int TenantsWithErrorsCount,
    int MonitorsWithErrorsCount,
    int FeedsWithErrorsCount,
    string? GlobalSyncError
);

public record BackgroundJobStatusDto(
    string JobId,
    string JobName,
    string? JobArgs,
    string State,
    DateTime? CreatedAt,
    double? DurationSeconds,
    string? ExceptionMessage,
    string? TenantName = null,
    string? MonitorName = null
);
