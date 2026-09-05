// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Entities.Monitoring;
using Adwais.Application.DTOs.Monitoring;
using Adwais.Domain.Entities;
using Adwais.Application.Common.Models;
using FluentResults;

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
    Task<Result<IReadOnlyList<UptimeMonitor>>> GetMonitorsAsync(ResolvedPeriod period, Guid? tenantId = null, CancellationToken ct = default);

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
    /// Retrieves unassigned monitors for the current scope: the caller's organization bucket,
    /// or every organization's bucket for platform admins.
    /// </summary>
    Task<IEnumerable<UptimeMonitor>> GetUnassignedMonitorsAsync(ResolvedPeriod period, CancellationToken ct = default);

    /// <summary>
    /// Moves a monitor to its own organization's unassigned bucket.
    /// </summary>
    Task<Result> UnassignMonitorAsync(int monitorId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a specific uptime monitor for a tenant, hydrated with uptime for the given timeframe.
    /// </summary>
    Task<Result<UptimeMonitor>> GetMonitorAsync(Guid tenantId, int id, ResolvedPeriod period, CancellationToken ct = default);

    /// <summary>
    /// Creates a new uptime monitor for a tenant.
    /// </summary>
    Task<Result<UptimeMonitor>> CreateMonitorAsync(Guid tenantId, string name, string url, string? type, double? uptimeSla, CancellationToken ct = default, int? latencyDegradedFloor = null);

    /// <summary>
    /// Creates a new uptime monitor in the caller's organization unassigned bucket.
    /// Platform admins without an organization scope use the default organization.
    /// </summary>
    Task<Result<UptimeMonitor>> CreateUnassignedMonitorAsync(string name, string url, string? type, double? uptimeSla, CancellationToken ct = default, int? latencyDegradedFloor = null);

    /// <summary>
    /// Assigns an existing monitor to a specific tenant.
    /// </summary>
    Task<Result> AssignMonitorAsync(int monitorId, Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Reassigns all monitors from a specific tenant to the system tenant.
    /// </summary>
    Task ReassignAllTenantMonitorsToSystemAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a monitor for a specific tenant.
    /// </summary>
    Task<Result> DeleteMonitorAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Pauses an uptime monitor.
    /// </summary>
    Task<Result> PauseMonitorAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Starts (resumes) a paused uptime monitor.
    /// </summary>
    Task<Result> StartMonitorAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves aggregated latency (response time) data for a specific monitor within a timeframe.
    /// </summary>
    Task<Result<IEnumerable<ResponseTime>>> GetAggregatedLatencyAsync(Guid tenantId, int id, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);

    /// <summary>
    /// Updates a specific monitor.
    /// </summary>
    Task<Result<UptimeMonitor>> UpdateMonitorAsync(int id, string? name, string? url, string? type, double? uptimeSla, List<string>? tags, CancellationToken ct = default, int? latencyDegradedFloor = null);
}
