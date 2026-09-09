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
/// Provides the legacy system health projection used by dashboard status widgets.
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

}
