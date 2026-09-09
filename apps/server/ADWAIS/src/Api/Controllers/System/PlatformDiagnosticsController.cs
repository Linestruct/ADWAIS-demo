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
/// Provides diagnostics across all organizations for platform administrators.
/// </summary>
[ApiController]
[Route("api/platform/diagnostics")]
[Authorize(Policy = "PlatformDiagnosticsRead")]
public sealed class PlatformDiagnosticsController(IDiagnosticsService diagnostics) : ControllerBase
{
    /// <summary>
    /// Retrieves the current platform health summary.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Platform health and organizations with recent issues.</returns>
    [HttpGet("health")]
    public async Task<ActionResult<PlatformDiagnosticsDto>> GetHealth(CancellationToken ct)
    {
        var result = await diagnostics.GetPlatformDiagnosticsAsync(ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    /// <summary>
    /// Retrieves current pipeline status for the platform or one organization.
    /// </summary>
    /// <param name="organizationId">Optional organization filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Pipeline status summaries grouped by organization.</returns>
    [HttpGet("pipelines")]
    public async Task<ActionResult<IReadOnlyList<OrganizationDiagnosticsDto>>> GetPipelines(
        [FromQuery] Guid? organizationId,
        CancellationToken ct = default)
    {
        var result = await diagnostics.GetPlatformPipelinesAsync(organizationId, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    /// <summary>
    /// Retrieves recent pipeline runs across the platform.
    /// </summary>
    /// <param name="organizationId">Optional organization filter.</param>
    /// <param name="tenantId">Optional tenant filter.</param>
    /// <param name="take">Maximum number of runs to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of recent pipeline runs.</returns>
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

    /// <summary>
    /// Retrieves one platform pipeline run and its related diagnostic events.
    /// </summary>
    /// <param name="runId">The pipeline run identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The run details and associated events.</returns>
    [HttpGet("runs/{runId:guid}")]
    public async Task<ActionResult<PipelineRunDetailsDto>> GetRun(Guid runId, CancellationToken ct)
    {
        var result = await diagnostics.GetRunAsync(null, runId, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    /// <summary>
    /// Retrieves recent diagnostic events across the platform.
    /// </summary>
    /// <param name="organizationId">Optional organization filter.</param>
    /// <param name="tenantId">Optional tenant filter.</param>
    /// <param name="take">Maximum number of events to return.</param>
    /// <param name="minLevel">Optional minimum event level.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of safe diagnostic event projections.</returns>
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
