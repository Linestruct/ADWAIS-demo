// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Monitoring.Upstream;

public sealed record MonitoringProviderMonitor(
    string ExternalId,
    string Type,
    string Name,
    string Url,
    string Status,
    DateTimeOffset CreatedDate,
    int UpdateInterval,
    List<string> Tags,
    string? HttpMethod = null,
    int? TimeoutSeconds = null,
    DateTimeOffset? SslExpiresAt = null,
    DateTimeOffset? DomainExpiresAt = null,
    List<string>? MonitoredRegions = null,
    long? CurrentStateDurationSeconds = null,
    MonitoringProviderIncident? LastIncident = null);

public sealed record MonitoringProviderIncident(
    string? ExternalId,
    string? Status,
    string? Cause,
    string? Reason,
    DateTimeOffset? StartedAt,
    long? DurationSeconds);

public sealed record MonitoringProviderAccount(
    string Email,
    string FullName,
    int MonitorsCount,
    int MonitorLimit,
    string ActiveSubscriptionPlan);
