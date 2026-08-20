// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.DTOs.Monitoring;

/// <summary>
/// Request parameters for monitor metrics.
/// </summary>
public record MonitorRequestDto
{
    /// <summary>
    /// Optional. Scopes the metrics to a specific tenant. 
    /// If null, metrics represent the global portfolio total.
    /// </summary>
    [FromQuery(Name = "tenantId")] 
    public Guid? TenantId { get; init; }
    
    /// <summary>
    /// Optional. Scopes the metrics to a specific monitor. 
    /// If null, metrics represent the global portfolio total.
    /// Overrides tenantId parameter.
    /// </summary>
    [FromQuery(Name = "monitorId")] 
    public int? MonitorId { get; init; }

    /// <summary>
    /// The primary timeframe for the calculation (e.g., T7, T30). 
    /// Defaults to T30.
    /// </summary>
    [FromQuery(Name = "timeframe")] 
    public Timeframe Timeframe { get; init; } = Timeframe.T30;

    /// <summary>
    /// The comparison period for the timeframe.
    /// Defaults to Preceding.
    /// </summary>
    [FromQuery(Name = "comparison")]
    public ComparisonType Comparison { get; init; } = ComparisonType.Preceding;

    /// <summary>
    /// Optional. Filters the monitors by specific tags.
    /// </summary>
    [FromQuery(Name = "tags")]
    public string[]? Tags { get; init; }

    /// <summary>
    /// Optional. Excludes monitors matching any of these tags. Exclusion takes precedence.
    /// </summary>
    [FromQuery(Name = "excludedTags")]
    public string[]? ExcludedTags { get; init; }

    /// <summary>
    /// Optional. Filters the monitors by specific statuses.
    /// </summary>
    [FromQuery(Name = "statuses")]
    public string[]? Statuses { get; init; }

    /// <summary>
    /// Optional. Excludes monitors with any of these statuses.
    /// </summary>
    [FromQuery(Name = "excludedStatuses")]
    public string[]? ExcludedStatuses { get; init; }
}

