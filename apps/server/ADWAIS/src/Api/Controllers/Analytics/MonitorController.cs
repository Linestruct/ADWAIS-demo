// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Monitoring;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Api.Controllers.Analytics;

/// <summary>
/// Manages uptime monitors and retrieves monitoring metrics.
/// </summary>
[ApiController]
[Route("api/monitors")]
public class MonitorController(
    IApplicationDbContext dbContext,
    IMonitorOrchestrationService monitorService,
    IReportingCalendar reportingCalendar,
    IEnumerable<IMonitoringProvider> monitoringProviders,
    IEnumerable<IOrderSource> orderSources) : ControllerBase
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IMonitorOrchestrationService _monitorService = monitorService;
    private readonly IEnumerable<IMonitoringProvider> _monitoringProviders = monitoringProviders;
    private readonly IEnumerable<IOrderSource> _orderSources = orderSources;

    private async Task<bool> IsMonitoringProviderConfiguredAsync(CancellationToken ct)
    {
        var db = _dbContext;
        var config = await db.GlobalConfigs.AsNoTracking().SingleOrDefaultAsync(ct);
        return config != null
            && _monitoringProviders.ForProvider(config.MonitoringProvider).IsConfigured(config.MonitoringProviderSettings);
    }

    /// <summary>
    /// Unified analytics endpoint for monitoring data.
    /// Provides latency time-series and monitoring KPIs for the specified timeframe (defaults to T30).
    /// </summary>
    [HttpGet("analytics")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<MonitorAnalyticsResponseDto>> GetAnalytics([FromQuery] MonitorRequestDto request, CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(request.Timeframe, request.Comparison, ct);
        var result = await _monitorService.GetAnalyticsAsync(
            period,
            request.TenantId,
            request.MonitorId,
            request.Tags,
            request.Statuses,
            ct,
            excludedTags: request.ExcludedTags,
            excludedStatuses: request.ExcludedStatuses);

        return Ok(new MonitorAnalyticsResponseDto
        {
            GlobalAverageLatency = result.GlobalAverageLatency,
            LatencyPoints = result.LatencyPoints.Select(p => new LatencyPointResponseDto
            {
                Timestamp = p.Timestamp,
                Average = p.Average,
                PreviousAverage = p.PreviousAverage,
                P10 = p.Lowest,
                P90 = p.Highest,
                CurrentState = p.CurrentState,
                PreviousState = p.PreviousState
            }).ToList(),
            Kpis = new MonitorKpiResponseDto(
                result.Kpis.AverageUptime,
                result.Kpis.PreviousAverageUptime,
                result.Kpis.UptimeGrowthPercentage,
                result.Kpis.AverageLatency,
                result.Kpis.PreviousAverageLatency,
                result.Kpis.LatencyGrowthPercentage,
                result.Kpis.HighestLatency,
                result.Kpis.PreviousHighestLatency,
                result.Kpis.HighestLatencyGrowthPercentage,
                result.Kpis.LowestLatency,
                result.Kpis.PreviousLowestLatency,
                result.Kpis.LowestLatencyGrowthPercentage)
        });
    }

    /// <summary>
    /// Returns daily availability for the selected fleet, tenant, or monitor scope.
    /// </summary>
    [HttpGet("availability")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<MonitorAvailabilitySeriesResponseDto>> GetAvailability(
        [FromQuery] MonitorRequestDto request,
        CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(request.Timeframe, request.Comparison, ct);
        var timeZone = await reportingCalendar.GetTimeZoneAsync(ct);
        var result = await _monitorService.GetAvailabilitySeriesAsync(
            period,
            timeZone,
            request.TenantId,
            request.MonitorId,
            request.Tags,
            request.Statuses,
            ct,
            excludedTags: request.ExcludedTags,
            excludedStatuses: request.ExcludedStatuses);

        return Ok(new MonitorAvailabilitySeriesResponseDto
        {
            PeriodStart = result.PeriodStart,
            PeriodEnd = result.PeriodEnd,
            AverageUptimePercentage = result.AverageUptimePercentage,
            LowestUptimePercentage = result.LowestUptimePercentage,
            Points = result.Points.Select(point => new MonitorAvailabilityPointResponseDto
            {
                Date = point.Date,
                EndDate = point.EndDate,
                UptimePercentage = point.UptimePercentage,
                LowestMonitorUptimePercentage = point.LowestMonitorUptimePercentage,
                MonitorCount = point.MonitorCount,
                IsPartial = point.IsPartial
            }).ToList()
        });
    }

    /// <summary>
    /// Retrieves monitors, optionally filtered by tenant or specific monitor ID.
    /// Returns monitors hydrated with uptime for the specified timeframe (defaults to T30).
    /// </summary>
    /// <param name="request">The request containing query filters and timeframe.</param>
    /// <param name="ct">Cancellation token</param>
    [HttpGet]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<UptimeMonitorDto>>> GetMonitors([FromQuery] MonitorRequestDto request, CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(request.Timeframe, request.Comparison, ct);

        IEnumerable<UptimeMonitorDto> resultDtos;

        if (request.MonitorId.HasValue)
        {
            var db = _dbContext;
            var tid = await db.Monitors.Where(m => m.Id == request.MonitorId.Value).Select(m => (Guid?)m.TenantId).SingleOrDefaultAsync(ct);
            if (tid == null) return Ok(Enumerable.Empty<UptimeMonitorDto>());

            var m = await _monitorService.GetMonitorAsync(tid.Value, request.MonitorId.Value, period, ct);
            resultDtos = new[] { ToDto(m) };
        }
        else if (request.TenantId.HasValue)
        {
            var monitors = await _monitorService.GetMonitorsByTenantAsync(request.TenantId.Value, period, ct);
            resultDtos = monitors.Select(ToDto);
        }
        else 
        {
            var monitors = await _monitorService.GetMonitorsAsync(period, ct: ct);
            resultDtos = monitors.Select(ToDto);
        }

        if (request.Tags != null && request.Tags.Any())
        {
            resultDtos = resultDtos.Where(m => m.Tags != null && m.Tags.Intersect(request.Tags, StringComparer.OrdinalIgnoreCase).Any());
        }

        if (request.Statuses != null && request.Statuses.Any())
        {
            resultDtos = resultDtos.Where(m => request.Statuses.Contains(m.CurrentStatus, StringComparer.OrdinalIgnoreCase));
        }

        if (request.ExcludedTags is { Length: > 0 })
        {
            resultDtos = resultDtos.Where(m =>
                m.Tags == null
                || !m.Tags.Intersect(request.ExcludedTags, StringComparer.OrdinalIgnoreCase).Any());
        }

        if (request.ExcludedStatuses is { Length: > 0 })
        {
            resultDtos = resultDtos.Where(m =>
                !request.ExcludedStatuses.Contains(m.CurrentStatus, StringComparer.OrdinalIgnoreCase));
        }

        return Ok(resultDtos);
    }

    /// <summary>
    /// Retrieves monitors that are not assigned to any specific tenant.
    /// </summary>
    /// <param name="timeframe">The timeframe for calculating uptime percentage.</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="comparison">The comparison period type.</param>
    [HttpGet("unassigned")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<UptimeMonitorDto>>> GetUnassignedMonitors([FromQuery] Timeframe timeframe = Timeframe.T30, [FromQuery] ComparisonType comparison = ComparisonType.Preceding, CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(timeframe, comparison, ct);
        var monitors = await _monitorService.GetMonitorsByTenantAsync(IApplicationDbContext.SystemTenantGuid, period, ct);
        return Ok(monitors.Select(ToDto));
    }

    /// <summary>
    /// Creates a new uptime monitor in UptimeRobot and registers it in the system.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UptimeMonitorDto>> CreateMonitor(
        [FromQuery] Guid tenantId,
        [FromBody] CreateMonitorRequestDto request,
        CancellationToken ct = default)
    {
        if (!await IsMonitoringProviderConfiguredAsync(ct)) return BadRequest("Monitoring provider settings are not configured.");
        var m = await _monitorService.CreateMonitorAsync(tenantId, request.Name, request.Url, request.Type, request.UptimeSla, ct, request.LatencyDegradedFloor);
        return CreatedAtAction(nameof(GetMonitors), new { id = m.Id }, ToDto(m));
    }

    /// <summary>
    /// Reassigns a monitor to a different tenant.
    /// </summary>
    [HttpPatch("{id:int}/assign/{tenantId:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AssignMonitor(int id, Guid tenantId, CancellationToken ct = default)
    {
        await _monitorService.AssignMonitorAsync(id, tenantId, ct);
        return Ok();
    }

    /// <summary>
    /// Moves a monitor to the unassigned (system) tenant.
    /// </summary>
    [HttpPatch("{id:int}/unassign")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UnassignMonitor(int id, CancellationToken ct = default)
    {
        await _monitorService.AssignMonitorAsync(id, IApplicationDbContext.SystemTenantGuid, ct);
        return Ok();
    }

    /// <summary>
    /// Pauses monitoring for a specific monitor in UptimeRobot.
    /// </summary>
    [HttpPost("{id:int}/pause")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> PauseMonitor(int id, CancellationToken ct = default)
    {
        if (!await IsMonitoringProviderConfiguredAsync(ct)) return BadRequest("Monitoring provider settings are not configured.");
        await _monitorService.PauseMonitorAsync(id, ct);
        return Ok();
    }

    /// <summary>
    /// Resumes monitoring for a specific monitor in UptimeRobot.
    /// </summary>
    [HttpPost("{id:int}/start")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> StartMonitor(int id, CancellationToken ct = default)
    {
        if (!await IsMonitoringProviderConfiguredAsync(ct)) return BadRequest("Monitoring provider settings are not configured.");
        await _monitorService.StartMonitorAsync(id, ct);
        return Ok();
    }

    /// <summary>
    /// Deletes a monitor from both the system and UptimeRobot.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteMonitor(int id, [FromQuery] Guid? tenantId, CancellationToken ct = default)
    {
        if (!await IsMonitoringProviderConfiguredAsync(ct)) return BadRequest("Monitoring provider settings are not configured.");

        if (!tenantId.HasValue)
        {
            var db = _dbContext;
            tenantId = await db.Monitors.Where(m => m.Id == id).Select(m => (Guid?)m.TenantId).SingleOrDefaultAsync(ct);
        }

        if (tenantId == null) return NotFound();

        await _monitorService.DeleteMonitorAsync(tenantId.Value, id, ct);
        return NoContent();
    }

    /// <summary>
    /// Retrieves aggregated latency metrics for a specific monitor.
    /// </summary>
    [HttpGet("{id:int}/latency")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<LatencyMetricsDto>>> GetLatencyMetrics(
        int id,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        if (!tenantId.HasValue)
        {
             var db = _dbContext;
             tenantId = await db.Monitors.Where(m => m.Id == id).Select(m => (Guid?)m.TenantId).SingleOrDefaultAsync(ct);
        }

        if (tenantId == null) return NotFound();

        var metrics = await _monitorService.GetAggregatedLatencyAsync(tenantId.Value, id, from, to, ct);
        return Ok(metrics);
    }

    /// <summary>
    /// Updates monitor properties, such as SLA.
    /// </summary>
    [HttpPatch("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UptimeMonitorDto>> UpdateMonitor(int id, [FromBody] UpdateMonitorRequestDto request, CancellationToken ct = default)
    {
        if (!await IsMonitoringProviderConfiguredAsync(ct)) return BadRequest("Monitoring provider settings are not configured.");
        await _monitorService.UpdateMonitorAsync(id, request.Name, request.Url, request.Type, request.Sla, request.Tags, ct, request.LatencyDegradedFloor);
        
        var db = _dbContext;
        var tenantId = await db.Monitors.Where(m => m.Id == id).Select(m => (Guid?)m.TenantId).SingleOrDefaultAsync(ct);
        if (tenantId == null) return NotFound();

        var period = await reportingCalendar.ResolvePeriodAsync(Timeframe.T30, ct: ct);
        var m = await _monitorService.GetMonitorAsync(tenantId.Value, id, period, ct);
        return Ok(ToDto(m));
    }

    private UptimeMonitorDto ToDto(UptimeMonitor m)
    {
        return new UptimeMonitorDto(
            Id: m.Id,
            TenantId: m.TenantId,
            TenantName: m.Tenant?.Name,
            Type: m.Type,
            Name: m.Name,
            Url: m.Url,
            UpdateInterval: m.UpdateInterval,
            LatencyDegradedFloor: m.LatencyDegradedFloor,
            UptimeSla: m.UptimeSla,
            CurrentUptimePercentage: m.CurrentUptimePercentage,
            CurrentLatency: m.CurrentLatency,
            UptimeMonitorEnabled: m.UptimeMonitorEnabled,
            CurrentStatus: m.StatusStr, // Guaranteed by InMemoryCache service
            LastUpdate: m.LastUpdate,
            LastUptimeUpdate: m.LastUptimeUpdate,
            LastLatencyUpdate: m.LastLatencyUpdate,
            CreatedDate: m.CreatedDate,
            LastSyncError: m.LastSyncError,
            Tags: m.Tags,
            TenantBaseUrl: m.Tenant is null
                ? null
                : _orderSources.ForProvider(m.Tenant.OrderProvider).GetPublicSettings(m.Tenant.OrderProviderSettings).GetValueOrDefault("endpointUrl"),
            TenantImageUrl: m.Tenant?.ImageUrl,
            HttpMethod: m.HttpMethod,
            TimeoutSeconds: m.TimeoutSeconds,
            SslExpiresAt: m.SslExpiresAt,
            DomainExpiresAt: m.DomainExpiresAt,
            MonitoredRegions: m.MonitoredRegions,
            CurrentStateDurationSeconds: m.CurrentStateDurationSeconds,
            LatestIncident: m.LastIncidentId is null
                && m.LastIncidentStatus is null
                && m.LastIncidentReason is null
                && m.LastIncidentStartedAt is null
                    ? null
                    : new MonitorIncidentDto
                    {
                        Id = m.LastIncidentId,
                        Status = m.LastIncidentStatus,
                        Cause = m.LastIncidentCause,
                        Reason = m.LastIncidentReason,
                        StartedAt = m.LastIncidentStartedAt,
                        DurationSeconds = m.LastIncidentDurationSeconds
                    },
            Provider: m.Provider,
            ExternalId: m.ExternalId);
    }
}
