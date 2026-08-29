// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Access;
using Adwais.Application.Interfaces;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Administration;

/// <summary>
/// Provides administrative endpoints to manually trigger or configure background jobs.
/// </summary>
[ApiController]
[Route("api/job")]
public class BackgroundJobController(
    IJobTriggerService jobTriggerService,
    ICurrentAccess currentAccess) : ControllerBase
{
    private readonly IJobTriggerService _jobTriggerService = jobTriggerService;
    private readonly ICurrentAccess _currentAccess = currentAccess;

    /// <summary>
    /// Triggers the monitoring-provider synchronization job for the caller's organization.
    /// </summary>
    [HttpPost("trigger/monitor-sync")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> TriggerMonitorSync(CancellationToken ct)
    {
        await _jobTriggerService.TriggerFleetSyncAsync(_currentAccess.Scope?.OrganizationId, ct);
        return Ok();
    }
    
    /// <summary>
    /// Triggers uptime metrics collection for the caller's organization.
    /// </summary>
    [HttpPost("trigger/uptime-sync")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> TriggerUptimeSync(CancellationToken ct)
    {
        await _jobTriggerService.TriggerUptimeSyncAsync(_currentAccess.Scope?.OrganizationId, ct);
        return Ok();
    }

    /// <summary>
    /// Triggers latency metrics collection for the caller's organization.
    /// </summary>
    [HttpPost("trigger/latency-sync")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> TriggerLatencySync(CancellationToken ct)
    {
        await _jobTriggerService.TriggerLatencySyncAsync(_currentAccess.Scope?.OrganizationId, ct);
        return Ok();
    }

    /// <summary>
    /// Triggers monitoring account statistics synchronization for the caller's organization.
    /// </summary>
    [HttpPost("trigger/user-stats-sync")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> TriggerUserStatsSync(CancellationToken ct)
    {
        await _jobTriggerService.TriggerAccountStatsSyncAsync(_currentAccess.Scope?.OrganizationId, ct);
        return Ok();
    }

    /// <summary>
    /// Triggers order ingestion for the caller's organization.
    /// </summary>
    [HttpPost("trigger/order-sync")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> TriggerOrderSync(CancellationToken ct)
    {
        await _jobTriggerService.TriggerOrderSyncAsync(_currentAccess.Scope?.OrganizationId, ct);
        return Ok();
    }

    /// <summary>
    /// Triggers a refresh of the financial materialized views.
    /// </summary>
    [HttpPost("trigger/refresh-historic-order-data")]
    [Authorize(Policy = "PlatformAdminOnly")]
    public ActionResult TriggerMaterialViewRefresh()
    {
        RecurringJob.TriggerJob("refresh-financial-materialized-views");
        return Ok();
    }
    
    /// <summary>
    /// Triggers a refresh of all monitoring materialized views (latency and availability).
    /// </summary>
    [HttpPost("trigger/refresh-monitoring-data")]
    [Authorize(Policy = "PlatformAdminOnly")]
    public ActionResult TriggerMonitoringMaterialViewRefresh()
    {
        RecurringJob.TriggerJob("refresh-monitoring-materialized-views");
        return Ok();
    }

    /// <summary>
    /// Retrieves a list of all registered recurring jobs and their current schedules.
    /// </summary>
    [HttpGet("recurring")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult> GetRecurringJobs()
    {
        var recurringJobs = await Task.Run(() => JobStorage.Current.GetConnection().GetRecurringJobs());
        return Ok(recurringJobs.Select(j => new
        {
            j.Id,
            j.Cron,
            j.LastExecution,
            j.NextExecution,
            j.LastJobState,
            j.Queue
        }));
    }

    /// <summary>
    /// Retrieves the status and history of a specific background job.
    /// </summary>
    /// <param name="jobId">The Hangfire Job ID.</param>
    [HttpGet("status/{jobId}")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult> GetJobStatus(string jobId)
    {
        var monitoringApi = JobStorage.Current.GetMonitoringApi();
        var jobDetails = await Task.Run(() => monitoringApi.JobDetails(jobId));

        if (jobDetails == null) return NotFound("Job not found.");

        var history = jobDetails.History.OrderByDescending(h => h.CreatedAt).ToList();
        var latestState = history.FirstOrDefault();

        return Ok(new
        {
            JobId = jobId,
            State = latestState?.StateName,
            Reason = latestState?.Reason,
            CreatedAt = jobDetails.CreatedAt,
            History = history.Select(h => new
            {
                h.StateName,
                h.CreatedAt,
                h.Reason
            })
        });
    }
}



