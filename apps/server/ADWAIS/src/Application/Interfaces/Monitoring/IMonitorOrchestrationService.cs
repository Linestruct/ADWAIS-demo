// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities.Monitoring;
using Adwais.Application.DTOs.Monitoring;
using Adwais.Domain.Entities;
using Adwais.Application.Common.Models;

namespace Adwais.Application.Interfaces;

public interface IMonitorOrchestrationService
{
    /// <summary>
    /// Retrieves aggregated monitoring KPIs and latency time-series.
    /// </summary>
    Task<MonitorAnalyticsDto> GetAnalyticsAsync(
        ResolvedPeriod period,
        Guid? tenantId = null,
        int? monitorId = null,
        string[]? tags = null,
        string[]? statuses = null,
        CancellationToken ct = default,
        string[]? excludedTags = null,
        string[]? excludedStatuses = null);

    /// <summary>
    /// Retrieves monitors in one batch and hydrates their period uptime in grouped queries.
    /// </summary>
    Task<IReadOnlyList<UptimeMonitor>> GetMonitorsAsync(ResolvedPeriod period, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Retrieves daily availability for the selected fleet, tenant, or monitor scope.
    /// </summary>
    Task<MonitorAvailabilitySeriesDto> GetAvailabilitySeriesAsync(
        ResolvedPeriod period,
        TimeZoneInfo reportingTimeZone,
        Guid? tenantId = null,
        int? monitorId = null,
        string[]? tags = null,
        string[]? statuses = null,
        CancellationToken ct = default,
        string[]? excludedTags = null,
        string[]? excludedStatuses = null);

    /// <summary>
    /// Retrieves all uptime monitors associated with a specific tenant, hydrated with uptime for the given timeframe.
    /// </summary>
    Task<IEnumerable<UptimeMonitor>> GetMonitorsByTenantAsync(Guid tenantId, ResolvedPeriod period, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a specific uptime monitor for a tenant, hydrated with uptime for the given timeframe.
    /// </summary>
    Task<UptimeMonitor> GetMonitorAsync(Guid tenantId, int id, ResolvedPeriod period, CancellationToken ct = default);

    /// <summary>
    /// Creates a new uptime monitor for a tenant.
    /// </summary>
    Task<UptimeMonitor> CreateMonitorAsync(Guid tenantId, string name, string url, string? type, double? uptimeSla, CancellationToken ct = default, int? latencyDegradedFloor = null);

    /// <summary>
    /// Assigns an existing monitor to a specific tenant.
    /// </summary>
    Task AssignMonitorAsync(int monitorId, Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Reassigns all monitors from a specific tenant to the system tenant.
    /// </summary>
    Task ReassignAllTenantMonitorsToSystemAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a monitor for a specific tenant.
    /// </summary>
    Task DeleteMonitorAsync(Guid tenantId, int id, CancellationToken ct = default);

    /// <summary>
    /// Pauses an uptime monitor.
    /// </summary>
    Task PauseMonitorAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Starts (resumes) a paused uptime monitor.
    /// </summary>
    Task StartMonitorAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves aggregated latency (response time) data for a specific monitor within a timeframe.
    /// </summary>
    Task<IEnumerable<ResponseTime>> GetAggregatedLatencyAsync(Guid tenantId, int id, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);

    /// <summary>
    /// Updates a specific monitor.
    /// </summary>
    Task<UptimeMonitor> UpdateMonitorAsync(int id, string? name, string? url, string? type, double? uptimeSla, List<string>? tags, CancellationToken ct = default, int? latencyDegradedFloor = null);
}
