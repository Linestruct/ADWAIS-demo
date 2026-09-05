// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.DTOs.Financial;

/// <summary>
/// Request parameters for order value distribution (histogram).
/// </summary>
public record OrderDistributionRequestDto
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
    /// Optional. The number of bins to use for the histogram. 
    /// If null, an adaptive binning strategy is used.
    /// </summary>
    [FromQuery(Name = "binCount")]
    public int? BinCount { get; init; }

    /// <summary>
    /// The comparison period for the timeframe.
    /// Defaults to Preceding.
    /// </summary>
    [FromQuery(Name = "comparison")]
    public ComparisonType Comparison { get; init; } = ComparisonType.Preceding;
}


