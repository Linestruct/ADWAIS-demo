// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Adwais.Api.DTOs.Monitoring;

public record UptimeMonitorDto(
    int Id, 
    Guid TenantId,
    string? TenantName,
    [property: Required] string Type,
    string Name, 
    string Url, 
    int UpdateInterval,
    int? LatencyDegradedFloor,
    double? UptimeSla,
    double? CurrentUptimePercentage,
    double? CurrentLatency,
    bool UptimeMonitorEnabled, 
    string CurrentStatus, 
    DateTimeOffset? LastUpdate,
    DateTimeOffset? LastUptimeUpdate,
    DateTimeOffset? LastLatencyUpdate,
    DateTimeOffset CreatedDate,
    string? LastSyncError,
    List<string> Tags,
    [property: JsonRequired] string? TenantBaseUrl,
    [property: JsonRequired] string? TenantImageUrl,
    [property: JsonRequired] string? HttpMethod,
    [property: JsonRequired] int? TimeoutSeconds,
    [property: JsonRequired] DateTimeOffset? SslExpiresAt,
    [property: JsonRequired] DateTimeOffset? DomainExpiresAt,
    [property: JsonRequired] List<string> MonitoredRegions,
    [property: JsonRequired] long? CurrentStateDurationSeconds,
    [property: JsonRequired] MonitorIncidentDto? LatestIncident,
    [property: JsonRequired] string Provider,
    [property: JsonRequired] string ExternalId);

public sealed record MonitorIncidentDto
{
    public required string? Id { get; init; }
    public required string? Status { get; init; }
    public required string? Cause { get; init; }
    public required string? Reason { get; init; }
    public required DateTimeOffset? StartedAt { get; init; }
    public required long? DurationSeconds { get; init; }
}


