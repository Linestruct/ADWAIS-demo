// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.DTOs.Financial;

/// <summary>
/// Request DTO for retrieving recent order.
/// </summary>
public record OrderRequestDto
{
    /// <summary>
    ///  Optional. Defaults to 5 minutes ago.
    /// </summary>
    [FromQuery(Name = "dateSince")] 
    public DateTimeOffset DateSince { get; set; } = DateTimeOffset.UtcNow.AddMinutes(-5);
    
    /// <summary>
    /// Optional. Defaults to now.
    /// </summary>
    [FromQuery(Name = "dateUntil")]
    public DateTimeOffset DateUntil { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Optional. Defaults to 20.
    /// </summary>
    [FromQuery(Name = "CeilingCount")]
    public int CeilingCount { get; set; } = 20;
}
    