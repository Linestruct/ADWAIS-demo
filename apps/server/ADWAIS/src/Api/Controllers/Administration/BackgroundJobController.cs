// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

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
public class BackgroundJobController : ControllerBase
{
    /// <summary>
    /// Triggers the monitoring-provider synchronization job immediately.
    /// </summary>
    [HttpPost("trigger/monitor-sync")]
    [Authorize(Policy = "AdminOnly")]
    public ActionResult TriggerMonitorSync()
    {
        RecurringJob.TriggerJob("sync-monitoring-fleet");
        return Ok();
    }
    
    /// <summary>
    /// Triggers the monitoring uptime metrics collection job immediately.
    /// </summary>
    [HttpPost("trigger/uptime-sync")]
    [Authorize(Policy = "AdminOnly")]
    public ActionResult TriggerUptimeSync()
    {
        RecurringJob.TriggerJob("dispatch-monitoring-uptime");
        return Ok();
    }

    /// <summary>
    /// Triggers the monitoring latency metrics collection job immediately.
    /// </summary>
    [HttpPost("trigger/latency-sync")]
    [Authorize(Policy = "AdminOnly")]
    public ActionResult TriggerLatencySync()
    {
        RecurringJob.TriggerJob("dispatch-monitoring-latency");
        return Ok();
    }

    /// <summary>
    /// Triggers the monitoring account statistics synchronization job immediately.
    /// </summary>
    [HttpPost("trigger/user-stats-sync")]
    [Authorize(Policy = "AdminOnly")]
    public ActionResult TriggerUserStatsSync()
    {
        RecurringJob.TriggerJob("sync-monitoring-account-stats");
        return Ok();
    }

    /// <summary>
    /// Triggers the order ingestion job immediately.
    /// </summary>
    [HttpPost("trigger/order-sync")]
    [Authorize(Policy = "AdminOnly")]
    public ActionResult TriggerOrderSync()
    {
        RecurringJob.TriggerJob("dispatch-order-fetch");
        return Ok();
    }

    /// <summary>
    /// Triggers a refresh of the financial materialized views.
    /// </summary>
    [HttpPost("trigger/refresh-historic-order-data")]
    [Authorize(Policy = "StaffAccess")]
    public ActionResult TriggerMaterialViewRefresh()
    {
        RecurringJob.TriggerJob("refresh-financial-materialized-views");
        return Ok();
    }
    
    /// <summary>
    /// Triggers a refresh of all monitoring materialized views (latency and availability).
    /// </summary>
    [HttpPost("trigger/refresh-monitoring-data")]
    [Authorize(Policy = "StaffAccess")]
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



