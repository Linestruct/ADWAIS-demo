// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.DTOs.System;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.System;

/// <summary>
/// Provides a high-level overview of system health and background job status.
/// </summary>
[ApiController]
[Route("api/system/health")]
public class SystemHealthController(ISystemHealthService healthService) : ControllerBase
{
    /// <summary>
    /// Retrieves an aggregated health report of the entire application pipeline.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "PlatformDiagnosticsRead")]
    public async Task<ActionResult<SystemHealthDto>> GetHealth()
    {
        var health = await healthService.GetHealthAsync();
        return Ok(health);
    }

    /// <summary>
    /// Clears all stored sync errors from tenants, monitors, and global configuration.
    /// </summary>
    [HttpPost("clear-errors")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public Task<IActionResult> ClearErrors()
    {
        IActionResult result = StatusCode(StatusCodes.Status410Gone, new ProblemDetails
        {
            Title = "Clearing diagnostics is disabled",
            Detail = "A successful operation resolves its own current state; history is retained by policy.",
            Status = StatusCodes.Status410Gone,
            Type = "https://adwais.app/problems/diagnostics-clearing-disabled"
        });
        return Task.FromResult(result);
    }

    /// <summary>
    /// The legacy Hangfire job projection has been retired. Use the scoped
    /// diagnostics runs endpoints instead.
    /// </summary>
    [HttpGet("jobs")]
    [Authorize(Policy = "DiagnosticsRead")]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public IActionResult GetRecentJobs()
    {
        return StatusCode(StatusCodes.Status410Gone, new ProblemDetails
        {
            Title = "Legacy job history is disabled",
            Detail = "Use the organization or platform diagnostics runs endpoint.",
            Status = StatusCodes.Status410Gone,
            Type = "https://adwais.app/problems/legacy-job-history-disabled"
        });
    }
}
