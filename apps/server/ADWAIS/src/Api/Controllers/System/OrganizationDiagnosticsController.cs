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
/// Provides diagnostics scoped to a single organization.
/// </summary>
[ApiController]
[Route("api/organizations/{organizationId:guid}/diagnostics")]
[Authorize(Policy = "DiagnosticsRead")]
public sealed class OrganizationDiagnosticsController(IDiagnosticsService diagnostics) : ControllerBase
{
    /// <summary>
    /// Retrieves the current pipeline status summary for an organization.
    /// </summary>
    /// <param name="organizationId">The organization whose pipelines should be inspected.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The organization's pipeline counts and individual pipeline states.</returns>
    [HttpGet("pipelines")]
    public async Task<ActionResult<OrganizationDiagnosticsDto>> GetPipelines(Guid organizationId, CancellationToken ct)
    {
        var result = await diagnostics.GetOrganizationPipelinesAsync(organizationId, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    /// <summary>
    /// Retrieves recent pipeline runs for an organization.
    /// </summary>
    /// <param name="organizationId">The organization whose runs should be returned.</param>
    /// <param name="tenantId">Optional tenant filter.</param>
    /// <param name="take">Maximum number of runs to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of recent pipeline runs.</returns>
    [HttpGet("runs")]
    public async Task<ActionResult<IReadOnlyList<PipelineRunDto>>> GetRuns(
        Guid organizationId,
        [FromQuery] Guid? tenantId,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var result = await diagnostics.GetRunsAsync(organizationId, tenantId, take, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    /// <summary>
    /// Retrieves one pipeline run and its related diagnostic events.
    /// </summary>
    /// <param name="organizationId">The organization that owns the run.</param>
    /// <param name="runId">The pipeline run identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The run details and associated events.</returns>
    [HttpGet("runs/{runId:guid}")]
    public async Task<ActionResult<PipelineRunDetailsDto>> GetRun(Guid organizationId, Guid runId, CancellationToken ct)
    {
        var result = await diagnostics.GetRunAsync(organizationId, runId, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    /// <summary>
    /// Retrieves recent diagnostic events for an organization.
    /// </summary>
    /// <param name="organizationId">The organization whose events should be returned.</param>
    /// <param name="tenantId">Optional tenant filter.</param>
    /// <param name="take">Maximum number of events to return.</param>
    /// <param name="minLevel">Optional minimum event level.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of safe diagnostic event projections.</returns>
    [HttpGet("events")]
    public async Task<ActionResult<IReadOnlyList<DiagnosticEventDto>>> GetEvents(
        Guid organizationId,
        [FromQuery] Guid? tenantId,
        [FromQuery] int take = 50,
        [FromQuery] SystemEventLevel? minLevel = null,
        CancellationToken ct = default)
    {
        var result = await diagnostics.GetEventsAsync(organizationId, tenantId, take, minLevel, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }
}
