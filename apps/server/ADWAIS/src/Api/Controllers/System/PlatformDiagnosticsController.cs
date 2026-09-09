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

[ApiController]
[Route("api/platform/diagnostics")]
[Authorize(Policy = "PlatformDiagnosticsRead")]
public sealed class PlatformDiagnosticsController(IDiagnosticsService diagnostics) : ControllerBase
{
    [HttpGet("health")]
    public async Task<ActionResult<PlatformDiagnosticsDto>> GetHealth(CancellationToken ct)
    {
        var result = await diagnostics.GetPlatformDiagnosticsAsync(ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    [HttpGet("pipelines")]
    public async Task<ActionResult<IReadOnlyList<OrganizationDiagnosticsDto>>> GetPipelines(
        [FromQuery] Guid? organizationId,
        CancellationToken ct = default)
    {
        var result = await diagnostics.GetPlatformPipelinesAsync(organizationId, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    [HttpGet("runs")]
    public async Task<ActionResult<IReadOnlyList<PipelineRunDto>>> GetRuns(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? tenantId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var result = await diagnostics.GetRunsAsync(organizationId, tenantId, take, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    [HttpGet("runs/{runId:guid}")]
    public async Task<ActionResult<PipelineRunDetailsDto>> GetRun(Guid runId, CancellationToken ct)
    {
        var result = await diagnostics.GetRunAsync(null, runId, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<DiagnosticEventDto>>> GetEvents(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? tenantId,
        [FromQuery] int take = 50,
        [FromQuery] SystemEventLevel? minLevel = null,
        CancellationToken ct = default)
    {
        var result = await diagnostics.GetEventsAsync(organizationId, tenantId, take, minLevel, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
}
