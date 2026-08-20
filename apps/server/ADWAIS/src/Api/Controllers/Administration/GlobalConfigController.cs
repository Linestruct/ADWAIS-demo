// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Administration;

/// <summary>
/// Provides access to system-wide global settings and configurations.
/// </summary>
[ApiController]
[Route("api/global-config")]
[Authorize]
public class GlobalConfigController(IGlobalConfigService globalConfigService) : ControllerBase
{
    private readonly IGlobalConfigService _globalConfigService = globalConfigService;

    /// <summary>
    /// Retrieves the current global system configuration.
    /// Sensitive values like API keys are masked.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<GlobalConfigResponseDto>> GetConfig()
    {
        var config = await _globalConfigService.GetConfigAsync();
        return Ok(config);
    }

    /// <summary>
    /// Partially updates the global system configuration.
    /// Updating fetch intervals will automatically reschedule associated background jobs.
    /// </summary>
    /// <param name="request">The request containing the settings to update.</param>
    [HttpPatch]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<GlobalConfigResponseDto>> UpdateConfig([FromBody] UpdateGlobalConfigRequestDto request)
    {
        return Ok(await _globalConfigService.UpdateConfigAsync(request));
    }

    /// <summary>
    /// Triggers the intranet feed aggregation job immediately.
    /// </summary>
    [HttpPost("feeds/fetch")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> TriggerFeedFetch()
    {
        await _globalConfigService.TriggerFeedFetchAsync();
        return Ok(new { message = "Feed fetch triggered successfully" });
    }

    /// <summary>
    /// Retrieves the current global fetch intervals for all background jobs.
    /// </summary>
    [HttpGet("intervals")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<FetchIntervalsDto>> GetFetchIntervals()
    {
        var intervals = await _globalConfigService.GetFetchIntervalsAsync();
        return Ok(intervals);
    }

    /// <summary>
    /// Updates the global fetch intervals for background jobs.
    /// </summary>
    [HttpPatch("intervals")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<FetchIntervalsDto>> UpdateFetchIntervals([FromBody] UpdateFetchIntervalsRequestDto request)
    {
        var updated = await _globalConfigService.UpdateFetchIntervalsAsync(request);
        return Ok(updated);
    }
}
