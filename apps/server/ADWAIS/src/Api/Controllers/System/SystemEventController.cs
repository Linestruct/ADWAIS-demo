// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.Extensions;
using Adwais.Application.DTOs.System;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.System;

/// <summary>
/// Provides access to scoped operational events.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemEventController(IDiagnosticsService diagnostics) : ControllerBase
{
    /// <summary>
    /// Retrieves a list of recent operational events, with optional filtering.
    /// </summary>
    /// <param name="take">Number of events to retrieve (default 50).</param>
    /// <param name="minLevel">Minimum event level to include (e.g., Information, Warning, Error).</param>
    /// <param name="tenantId">Filter events related to a specific tenant.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of safe operational event projections.</returns>
    [HttpGet]
    [Authorize(Policy = "DiagnosticsRead")]
    public async Task<ActionResult<IReadOnlyList<DiagnosticEventDto>>> GetEvents(
        [FromQuery] int take = 50, 
        [FromQuery] SystemEventLevel? minLevel = null,
        [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var result = await diagnostics.GetEventsAsync(null, tenantId, take, minLevel, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    /// <summary>
    /// Deletes system events older than a specified number of days.
    /// </summary>
    /// <param name="olderThanDays">Delete events older than this many days (default 30).</param>
    /// <returns>The number of deleted events.</returns>
    [HttpDelete("clear")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public IActionResult ClearEvents([FromQuery] int olderThanDays = 30)
    {
        return StatusCode(StatusCodes.Status410Gone, new ProblemDetails
        {
            Title = "Event deletion is disabled",
            Detail = "Operational history is removed by scheduled retention.",
            Status = StatusCodes.Status410Gone,
            Type = "https://adwais.app/problems/diagnostics-history-deletion-disabled"
        });
    }
}
