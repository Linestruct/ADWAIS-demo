// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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
    [Authorize(Policy = "PlatformAdminOnly")]
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
    public async Task<IActionResult> ClearErrors()
    {
        await healthService.ClearErrorsAsync();
        return NoContent();
    }

    /// <summary>
    /// Retrieves a list of recent background job executions and their status,
    /// filtered to the caller's organization.
    /// </summary>
    [HttpGet("jobs")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<BackgroundJobStatusDto>>> GetRecentJobs()
    {
        var jobs = await healthService.GetRecentJobsAsync();
        return Ok(jobs);
    }
}
