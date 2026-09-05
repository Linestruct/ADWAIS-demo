// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Api.DTOs.Monitoring;

/// <summary>
/// Request DTO updating a monitor.
/// </summary>
public record UpdateMonitorRequestDto
{
    /// <summary>
    /// Friendly name for the monitor.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// URL for the monitor.
    /// </summary>
    public string? Url { get; init; }

    /// <summary>
    /// UptimeRobot monitor type. Omit to leave the type unchanged.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// SLA for the monitor.
    /// </summary>
    public double? Sla { get; init; }

    /// <summary>
    /// Optional per-monitor latency threshold in milliseconds. Omit to leave the current value unchanged.
    /// </summary>
    public int? LatencyDegradedFloor { get; init; }

    /// <summary>
    /// Tags associated with the monitor.
    /// </summary>
    public List<string>? Tags { get; init; }
}


