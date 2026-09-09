// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Ingestion;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Adwais.Api.Controllers.Integrations;

/// <summary>
/// Handles manual data ingestion and historical backfills from external sources.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class IngestionController(
    IApplicationDbContext dbContext,
    IBackgroundJobClient backgroundJobClient,
    IEnumerable<IOrderSource> orderSources,
    ICurrentAccess currentAccess,
    IPipelineRunService? pipelineRunService = null)
    : ControllerBase
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient;
    private readonly ICurrentAccess _currentAccess = currentAccess;
    private readonly IPipelineRunService? _pipelineRunService = pipelineRunService;

    /// <summary>
    /// Manually triggers a historical backfill for a specific tenant within a given date range.
    /// </summary>
    /// <remarks>
    /// If StartDate or EndDate are omitted, the system defaults to a 2-year lookback ending now.
    /// This job is offloaded to Hangfire for background processing.
    /// Validation is handled automatically by the ValidationFilter.
    /// </remarks>
    /// <param name="request">The backfill request parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The ID of the enqueued background job.</returns>
    /// <response code="202">If the job was successfully enqueued.</response>
    /// <response code="404">If the tenant was not found.</response>
    /// <response code="409">If a fetch job is already running for this tenant.</response>
    [HttpPost("backfill")]
    public async Task<IActionResult> ExecuteHistoricalBackfill(
        [FromQuery] HistoricalBackfillRequestDto request,
        CancellationToken ct)
    {
        var context = _dbContext;
        var tenant = await context.Tenants.SingleOrDefaultAsync(t => t.Id == request.TenantId, ct);
        
        if (tenant == null) return NotFound("Tenant not found.");
        var scopeOrgId = _currentAccess.Scope?.OrganizationId;
        if (scopeOrgId is not null && tenant.OrganizationId != scopeOrgId) return Forbid();
        if (!orderSources.ForProvider(tenant.OrderProvider).IsConfigured(tenant.OrderProviderSettings))
            return BadRequest("Tenant is missing valid order provider settings.");
        if (tenant.CurrentlyFetching) return Conflict(new { message = $"Tenant {request.TenantId} is currently fetching. Wait for the active job to complete." });

        tenant.CurrentlyFetching = true;
        await context.SaveChangesAsync(ct);

        var startDate = request.StartDate ?? DateTimeOffset.UtcNow.AddYears(request.DefaultLookBackPeriodYears);
        var endDate = request.EndDate ?? DateTimeOffset.UtcNow;

        var pipelineRun = _pipelineRunService is null
            ? null
            : await _pipelineRunService.StartAsync(
                tenant.OrganizationId,
                tenant.Id,
                PipelineKind.OrderIngestion,
                PipelineTriggerKind.Manual,
                resourceKey: tenant.Id.ToString("D"),
                resourceName: tenant.Name,
                actorUserId: TryGetActorId(User),
                requestId: HttpContext.TraceIdentifier,
                traceId: Activity.Current?.TraceId.ToHexString(),
                ct: ct);

        string jobId;
        try
        {
            jobId = pipelineRun is null
                ? _backgroundJobClient.Enqueue<IOrderIngestionService>(
                    service => service.ExecuteIngestionAsync(tenant.OrganizationId, tenant.Id, startDate, endDate, CancellationToken.None))
                : _backgroundJobClient.Enqueue<IOrderIngestionService>(
                    service => service.ExecuteIngestionTrackedAsync(tenant.OrganizationId, tenant.Id, startDate, endDate, pipelineRun.Id, CancellationToken.None));
            if (pipelineRun is not null)
                await _pipelineRunService!.AttachHangfireJobAsync(pipelineRun.Id, jobId, CancellationToken.None);
        }
        catch
        {
            tenant.CurrentlyFetching = false;
            try
            {
                await context.SaveChangesAsync(CancellationToken.None);
            }
            catch
            {
                // Preserve the enqueue failure; the dispatcher will repair a
                // stale flag on its next pass.
            }
            if (pipelineRun is not null)
                await _pipelineRunService!.FailAsync(pipelineRun.Id, "dispatch.failed", "The ingestion job could not be queued.", CancellationToken.None);
            throw;
        }

        return Accepted(new { JobId = jobId, PipelineRunId = pipelineRun?.Id });
    }

    private static Guid? TryGetActorId(ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)
            ? actorId
            : null;
}
