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
[Route("api/organizations/{organizationId:guid}/diagnostics")]
[Authorize(Policy = "DiagnosticsRead")]
public sealed class OrganizationDiagnosticsController(IDiagnosticsService diagnostics) : ControllerBase
{
    [HttpGet("pipelines")]
    public async Task<ActionResult<OrganizationDiagnosticsDto>> GetPipelines(Guid organizationId, CancellationToken ct)
    {
        var result = await diagnostics.GetOrganizationPipelinesAsync(organizationId, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

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

    [HttpGet("runs/{runId:guid}")]
    public async Task<ActionResult<PipelineRunDetailsDto>> GetRun(Guid organizationId, Guid runId, CancellationToken ct)
    {
        var result = await diagnostics.GetRunAsync(organizationId, runId, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

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
