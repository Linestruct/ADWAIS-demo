// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.ComponentModel.DataAnnotations;

namespace Adwais.Api.DTOs.Monitoring;

/// <summary>
/// Data transfer object for creating a new uptime monitor.
/// </summary>
public sealed record CreateMonitorRequestDto
{
    /// <summary>
    /// The display name for the monitor.
    /// </summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// The absolute URL or host to monitor.
    /// </summary>
    [Required]
    public required string Url { get; init; }

    /// <summary>
    /// Optional UptimeRobot monitor type. Defaults to HTTP when omitted.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Optional target uptime percentage (0-100).
    /// </summary>
    public double? UptimeSla { get; init; }

    /// <summary>
    /// Optional per-monitor latency threshold in milliseconds. When omitted, latency is not classified as degraded.
    /// </summary>
    public int? LatencyDegradedFloor { get; init; }
}


