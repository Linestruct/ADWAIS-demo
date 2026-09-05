// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain;

namespace Adwais.Domain.Entities.Monitoring;

public class UptimeMonitor
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public string Provider { get; set; } = IntegrationProviders.UptimeRobot;
    public string ExternalId { get; set; } = string.Empty;
    public string Type { get; set; } = UptimeMonitorTypes.Http;
    public required string Name { get; set; }
    public required string Url { get; set; }
    /// <summary>Per-monitor uptime target. Null means no SLA is configured; no global fallback is applied.</summary>
    public double? UptimeSla { get; set; }
    public bool UptimeMonitorEnabled { get; set; }
    public DateTimeOffset? LastUpdate { get; set; }
    public DateTimeOffset? LastUptimeUpdate { get; set; }
    public DateTimeOffset? LastLatencyUpdate { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public int UpdateInterval { get; set; }
    public string? HttpMethod { get; set; }
    public int? TimeoutSeconds { get; set; }
    public DateTimeOffset? SslExpiresAt { get; set; }
    public DateTimeOffset? DomainExpiresAt { get; set; }
    public List<string> MonitoredRegions { get; set; } = new();
    public long? CurrentStateDurationSeconds { get; set; }
    public string? LastIncidentId { get; set; }
    public string? LastIncidentStatus { get; set; }
    public string? LastIncidentCause { get; set; }
    public string? LastIncidentReason { get; set; }
    public DateTimeOffset? LastIncidentStartedAt { get; set; }
    public long? LastIncidentDurationSeconds { get; set; }
    /// <summary>Per-monitor latency degradation threshold in milliseconds. Null means latency is not thresholded.</summary>
    public int? LatencyDegradedFloor { get; set; }
    public double? CurrentUptimePercentage { get; set; }
    public double? CurrentLatency { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? LastSyncError { get; set; }
    public Tenant? Tenant { get; set; }
    public ICollection<ResponseTime> ResponseTimes { get; set; } = new List<ResponseTime>();
    
    // Telemetry and live statuses; do not persist.
    public string StatusStr { get; set; } = "Unknown";
}


