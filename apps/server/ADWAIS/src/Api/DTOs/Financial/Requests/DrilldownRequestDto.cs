// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.DTOs.Financial;

/// <summary>
/// Request parameters for tenant-specific (drilldown) metrics.
/// </summary>
public record DrilldownRequestDto
{
    /// <summary>
    /// The primary timeframe for the calculation (e.g., T7, T30). 
    /// Defaults to T30.
    /// </summary>
    [FromQuery(Name = "timeframe")]
    public Timeframe Timeframe { get; init; } = Timeframe.T30;

    /// <summary>
    /// The unique identifier of the tenant.
    /// </summary>
    [FromQuery(Name = "tenantId")]
    public required Guid TenantId { get; init; }

    /// <summary>
    /// The comparison period for the timeframe.
    /// Defaults to Preceding.
    /// </summary>
    [FromQuery(Name = "comparison")]
    public ComparisonType Comparison { get; init; } = ComparisonType.Preceding;
}


