// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;
using Adwais.Application.Common.Models;
using Adwais.Application.Common.Errors;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Application.DTOs.Monitoring;
using Adwais.Domain.Enums;
using Adwais.Application.Common.Caching;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Common.Access;
using Adwais.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using FluentResults;

namespace Adwais.Application.Services;

public class MonitorOrchestrationService(
    IApplicationDbContext dbContext,
    IEnumerable<IMonitoringProvider> monitoringProviders,
    ICacheService cache,
    ICurrentAccess currentAccess,
    IOrganizationConfigService organizationConfigService) : IMonitorOrchestrationService
{
    private record LatencyRow(DateTimeOffset Timestamp, double? Average, double? P10, double? P90);
    private record AvailabilityRow(int MonitorId, DateTimeOffset Timestamp, double? UptimePercentage);

    private async Task<Guid[]?> GetVisibleTenantIdsAsync(CancellationToken ct)
    {
        var filter = OrganizationFilter.From(currentAccess.Scope);
        return await TenantVisibility.ResolveAsync(filter, dbContext.Tenants, ct);
    }

    private async Task<List<Guid>> GetSystemTenantIdsAsync(CancellationToken ct)
        => await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.IsSystem)
            .Select(t => t.Id)
            .ToListAsync(ct);

    private async Task<Guid> ResolveOrganizationBucketIdAsync(Guid organizationId, CancellationToken ct)
        => await dbContext.Tenants
            .Where(t => t.OrganizationId == organizationId && t.IsSystem)
            .Select(t => (Guid?)t.Id)
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Organization {organizationId} has no unassigned monitor bucket.");

    private static void ValidateTenantInScope(Guid tenantId, Guid[]? visibleTenantIds)
    {
        if (visibleTenantIds is not null && !visibleTenantIds.Contains(tenantId))
            throw new UnauthorizedAccessException($"Tenant {tenantId} is outside the current scope.");
    }

    private async Task ValidateMonitorInScopeAsync(int monitorId, CancellationToken ct)
    {
        var visibleTenantIds = await GetVisibleTenantIdsAsync(ct);
        if (visibleTenantIds is null)
        {
            return;
        }

        var tenantId = await dbContext.Monitors
            .Where(monitor => monitor.Id == monitorId)
            .Select(monitor => monitor.TenantId)
            .SingleOrDefaultAsync(ct);
        ValidateTenantInScope(tenantId, visibleTenantIds);
    }

    private ScopeDeniedError ScopeDenied()
        => new("the requested organization or tenant", currentAccess.Scope?.OrganizationId?.ToString() ?? "none");

    private async Task<ScopeDeniedError?> GetReadScopeFailureAsync(
        Guid? tenantId,
        int? monitorId,
        Guid[]? visibleTenantIds,
        CancellationToken ct)
    {
        if (visibleTenantIds is null)
            return null;

        if (tenantId.HasValue && !visibleTenantIds.Contains(tenantId.Value))
            return ScopeDenied();

        if (!monitorId.HasValue)
            return null;

        var monitorTenantId = await dbContext.Monitors
            .Where(monitor => monitor.Id == monitorId.Value)
            .Select(monitor => (Guid?)monitor.TenantId)
            .SingleOrDefaultAsync(ct);
        return monitorTenantId.HasValue && !visibleTenantIds.Contains(monitorTenantId.Value)
            ? ScopeDenied()
            : null;
    }

    private async Task<Result<Tenant>> GetVisibleTenantForMutationAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await dbContext.Tenants.SingleOrDefaultAsync(candidate => candidate.Id == tenantId, ct);
        if (tenant is null) return Result.Fail<Tenant>(new NotFoundError("tenant", tenantId));

        var visibleTenantIds = await GetVisibleTenantIdsAsync(ct);
        return visibleTenantIds is not null && !visibleTenantIds.Contains(tenantId)
            ? Result.Fail<Tenant>(ScopeDenied())
            : Result.Ok(tenant);
    }

    private async Task<Result<UptimeMonitor>> GetVisibleMonitorForMutationAsync(int monitorId, CancellationToken ct)
    {
        var monitor = await dbContext.Monitors
            .SingleOrDefaultAsync(candidate => candidate.Id == monitorId, ct);
        if (monitor is null) return Result.Fail<UptimeMonitor>(new NotFoundError("monitor", monitorId));

        var visibleTenantIds = await GetVisibleTenantIdsAsync(ct);
        return visibleTenantIds is not null && !visibleTenantIds.Contains(monitor.TenantId)
            ? Result.Fail<UptimeMonitor>(ScopeDenied())
            : Result.Ok(monitor);
    }

    private Task<Guid> GetMonitorOrganizationIdAsync(UptimeMonitor monitor, CancellationToken ct)
        => dbContext.Tenants
            .Where(tenant => tenant.Id == monitor.TenantId)
            .Select(tenant => tenant.OrganizationId)
            .SingleAsync(ct);

    private async Task<Result<IMonitoringProvider>> GetConfiguredMonitoringProviderAsync(
        Guid organizationId,
        CancellationToken ct,
        string? providerName = null)
    {
        var config = await organizationConfigService.GetConfigAsync(organizationId, ct);
        if (config is null)
            return Result.Fail<IMonitoringProvider>(new ConfigurationError("Monitoring is not configured for this organization."));

        var provider = monitoringProviders.ForProvider(providerName ?? config.MonitoringProvider);
        var settings = await dbContext.OrganizationConfigs
            .Where(candidate => candidate.OrganizationId == organizationId)
            .Select(candidate => candidate.MonitoringProviderSettings)
            .SingleOrDefaultAsync(ct);
        return provider.IsConfigured(settings)
            ? Result.Ok(provider)
            : Result.Fail<IMonitoringProvider>(new ConfigurationError("Monitoring provider settings are not configured."));
    }

    public async Task<Result<MonitorAnalyticsDto>> GetAnalyticsAsync(
        ResolvedPeriod period,
        Guid? tenantId = null,
        int? monitorId = null,
        string[]? tags = null,
        string[]? statuses = null,
        CancellationToken ct = default,
        string[]? excludedTags = null,
        string[]? excludedStatuses = null)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var previousStart = period.PreviousStart;
        var steps = period.StepsInPeriod;
        var isHourly = period.IsHourly;
        var includeActualTime = period.IncludeActualTime;

        var visibleTenantIds = await GetVisibleTenantIdsAsync(ct);
        var scopeFailure = await GetReadScopeFailureAsync(tenantId, monitorId, visibleTenantIds, ct);
        if (scopeFailure is not null) return Result.Fail<MonitorAnalyticsDto>(scopeFailure);

        IQueryable<UptimeMonitor> monitorQuery = dbContext.Monitors.AsNoTracking().Include(m => m.Tenant);
        if (monitorId.HasValue)
            monitorQuery = monitorQuery.Where(m => m.Id == monitorId.Value);
        else if (tenantId.HasValue)
            monitorQuery = monitorQuery.Where(m => m.TenantId == tenantId.Value);
        else
        {
            var systemTenantIds = await GetSystemTenantIdsAsync(ct);
            monitorQuery = monitorQuery.Where(m => !systemTenantIds.Contains(m.TenantId));
            if (visibleTenantIds is not null)
                monitorQuery = monitorQuery.Where(m => visibleTenantIds.Contains(m.TenantId));
        }

        var monitors = await monitorQuery.ToListAsync(ct);
        foreach (var m in monitors)
        {
            HydrateLiveStatus(m);
        }

        monitors = ApplyMonitorFilters(monitors, tags, statuses, excludedTags, excludedStatuses);

        var allowedMonitorIds = monitors.Select(m => m.Id).ToList();

        var currentRows = await GetMergedLatencyDataAsync(dbContext, currentStart, currentEnd, isHourly, allowedMonitorIds, ct);
        var previousRows = await GetMergedLatencyDataAsync(dbContext, previousStart, period.PreviousEnd, isHourly, allowedMonitorIds, ct);

        var binSize = isHourly
            ? TimeSpan.FromTicks((currentEnd - currentStart).Ticks / steps)
            : TimeSpan.FromDays(1);
        var currentByStep = BinLatencyRows(currentRows, currentStart, binSize, steps, isHourly);
        var previousByStep = BinLatencyRows(previousRows, previousStart, binSize, steps, isHourly);

        var latencyPoints = new List<LatencyPointDto>(steps);
        for (var i = 0; i < steps; i++)
        {
            var timestamp = isHourly
                ? currentStart.AddTicks(binSize.Ticks * (i + 1))
                : currentStart.AddDays(i);
            
            var data = currentByStep.GetValueOrDefault(i);
            var previousData = previousByStep.GetValueOrDefault(i);

            latencyPoints.Add(new LatencyPointDto(
                timestamp, 
                data?.Avg, 
                previousData?.Avg,
                data?.P10, 
                data?.P90,
                data is null ? LatencySampleState.NoSamples : LatencySampleState.Observed,
                previousData is null ? LatencySampleState.NoSamples : LatencySampleState.Observed));
        }
        
        var periodUptimes = await GetPeriodUptimesAsync(currentStart, currentEnd, allowedMonitorIds, ct);

        foreach (var m in monitors)
        {
            if (periodUptimes.TryGetValue(m.Id, out var periodUptime))
            {
                m.CurrentUptimePercentage = periodUptime;
            }
        }

        var previousPeriodUptimes = await GetPeriodUptimesAsync(previousStart, period.PreviousEnd, allowedMonitorIds, ct);

        var currentEnabledUptimes = monitors
            .Where(m => m.UptimeMonitorEnabled && periodUptimes.TryGetValue(m.Id, out var u) && u.HasValue)
            .Select(m => periodUptimes[m.Id]!.Value)
            .ToList();

        var previousEnabledUptimes = monitors
            .Where(m => m.UptimeMonitorEnabled && previousPeriodUptimes.TryGetValue(m.Id, out var u) && u.HasValue)
            .Select(m => previousPeriodUptimes[m.Id]!.Value)
            .ToList();

        double? avgUptime = currentEnabledUptimes.Count > 0 ? currentEnabledUptimes.Average() : null;
        double? previousAvgUptime = previousEnabledUptimes.Count > 0 ? previousEnabledUptimes.Average() : null;
        double? uptimeGrowth = CalculateGrowthPercentage(avgUptime, previousAvgUptime);

        double? avgLatency = currentRows.Count > 0 ? currentRows.Average(r => r.Average) : null;
        double? prevAvgLatency = previousRows.Count > 0 ? previousRows.Average(r => r.Average) : null;
        double? latencyGrowth = CalculateGrowthPercentage(avgLatency, prevAvgLatency);

        double? highestLatency = currentRows.Count > 0 ? currentRows.Max(r => r.P90) : null;
        double? prevHighestLatency = previousRows.Count > 0 ? previousRows.Max(r => r.P90) : null;
        double? highestLatencyGrowth = CalculateGrowthPercentage(highestLatency, prevHighestLatency);

        double? lowestLatency = currentRows.Count > 0 ? currentRows.Min(r => r.P10) : null;
        double? prevLowestLatency = previousRows.Count > 0 ? previousRows.Min(r => r.P10) : null;
        double? lowestLatencyGrowth = CalculateGrowthPercentage(lowestLatency, prevLowestLatency);

        var kpis = new MonitorKpiDto(
            avgUptime,
            previousAvgUptime,
            uptimeGrowth,
            avgLatency,
            prevAvgLatency,
            latencyGrowth,
            highestLatency,
            prevHighestLatency,
            highestLatencyGrowth,
            lowestLatency,
            prevLowestLatency,
            lowestLatencyGrowth
        );

        double? globalAvgLatency = null;
        if (monitors.Any(m => m.CurrentLatency.HasValue))
        {
            globalAvgLatency = monitors.Where(m => m.CurrentLatency.HasValue).Average(m => m.CurrentLatency!.Value);
        }

        return Result.Ok(new MonitorAnalyticsDto(globalAvgLatency, latencyPoints, kpis));
    }

    private async Task<Dictionary<int, double?>> GetPeriodUptimesAsync(DateTimeOffset start, DateTimeOffset end, List<int>? allowedMonitorIds, CancellationToken ct = default)
    {
        var systemTenantIds = await GetSystemTenantIdsAsync(ct);

        // Floor start boundary to UTC midnight to capture preceding time-series buckets
        var queryStart = new DateTimeOffset(start.UtcDateTime.Date, TimeSpan.Zero);
        var yesterday = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
        
        var histEnd = yesterday < end ? yesterday : end;

        var histQuery = dbContext.DailyAvailabilityMonitorRollups
            .AsNoTracking()
            .Where(r => r.Date >= queryStart && r.Date < histEnd);

        if (allowedMonitorIds != null && allowedMonitorIds.Any())
            histQuery = histQuery.Where(r => allowedMonitorIds.Contains(r.MonitorId));
        else if (allowedMonitorIds != null && !allowedMonitorIds.Any())
            histQuery = histQuery.Where(r => false); // no allowed monitors
        else
            histQuery = histQuery.Where(r => !systemTenantIds.Contains(r.UptimeMonitor.TenantId));

        var historicalDaily = await histQuery
            .GroupBy(r => r.MonitorId)
            .Select(g => new { 
                MonitorId = g.Key, 
                Avg = (double?)g.Average(r => r.UptimePercentage), 
                Count = g.Count(r => r.UptimePercentage != null) 
            })
            .ToDictionaryAsync(x => x.MonitorId, x => (x.Avg, x.Count), ct);

        var todayLive = new Dictionary<int, double?>();
        if (yesterday < end)
        {
            var liveStart = yesterday > queryStart ? yesterday : queryStart;
            IQueryable<MonitorAvailability> liveQuery = dbContext.MonitorAvailabilities
                .AsNoTracking()
                .Where(ma => ma.Date >= liveStart && ma.Date < end);

            if (allowedMonitorIds != null && allowedMonitorIds.Any())
                liveQuery = liveQuery.Where(ma => allowedMonitorIds.Contains(ma.MonitorId));
            else if (allowedMonitorIds != null && !allowedMonitorIds.Any())
                liveQuery = liveQuery.Where(ma => false);
            else
                liveQuery = liveQuery.Where(ma => !systemTenantIds.Contains(ma.UptimeMonitor!.TenantId));

            todayLive = await liveQuery
                .GroupBy(ma => ma.MonitorId)
                .Select(g => new { 
                    MonitorId = g.Key, 
                    Avg = (double?)g.Average(ma => ma.UptimePercentage)
                })
                .ToDictionaryAsync(x => x.MonitorId, x => x.Avg, ct);
        }

        var results = new Dictionary<int, double?>();
        var allMonitorIds = historicalDaily.Keys.Union(todayLive.Keys).Distinct();

        foreach (var mid in allMonitorIds)
        {
            var hasHist = historicalDaily.TryGetValue(mid, out var hist);
            var hasLive = todayLive.TryGetValue(mid, out var live);
            
            var histCount = hasHist ? hist.Count : 0;
            // Historical rows are daily rollups. Treat today's partial aggregate as
            // one partial day as well, rather than allowing minute-level polling
            // frequency to outweigh every completed day in the selected period.
            var liveCount = hasLive && live.HasValue ? 1 : 0;
            var totalCount = histCount + liveCount;
            
            if (totalCount == 0)
            {
                results[mid] = null;
            }
            else
            {
                var histSum = (hasHist ? (hist.Avg ?? 0) : 0) * histCount;
                var liveSum = (hasLive ? (live ?? 0) : 0) * liveCount;
                results[mid] = (histSum + liveSum) / totalCount;
            }
        }

        return results;
    }

    public async Task<Result<MonitorAvailabilitySeriesDto>> GetAvailabilitySeriesAsync(
        ResolvedPeriod period,
        TimeZoneInfo reportingTimeZone,
        Guid? tenantId = null,
        int? monitorId = null,
        string[]? tags = null,
        string[]? statuses = null,
        CancellationToken ct = default,
        string[]? excludedTags = null,
        string[]? excludedStatuses = null)
    {
        var visibleTenantIds = await GetVisibleTenantIdsAsync(ct);
        var scopeFailure = await GetReadScopeFailureAsync(tenantId, monitorId, visibleTenantIds, ct);
        if (scopeFailure is not null) return Result.Fail<MonitorAvailabilitySeriesDto>(scopeFailure);

        IQueryable<UptimeMonitor> monitorQuery = dbContext.Monitors.AsNoTracking();
        if (monitorId.HasValue)
            monitorQuery = monitorQuery.Where(m => m.Id == monitorId.Value);
        else if (tenantId.HasValue)
            monitorQuery = monitorQuery.Where(m => m.TenantId == tenantId.Value);
        else
        {
            var systemTenantIds = await GetSystemTenantIdsAsync(ct);
            monitorQuery = monitorQuery.Where(m => !systemTenantIds.Contains(m.TenantId));
            if (visibleTenantIds is not null)
                monitorQuery = monitorQuery.Where(m => visibleTenantIds.Contains(m.TenantId));
        }

        var monitors = await monitorQuery.ToListAsync(ct);
        foreach (var monitor in monitors)
        {
            HydrateLiveStatus(monitor);
        }

        monitors = ApplyMonitorFilters(monitors, tags, statuses, excludedTags, excludedStatuses);

        var monitorIds = monitors.Select(m => m.Id).ToList();
        var queryStart = period.CurrentStart.AddDays(-1);

        List<AvailabilityRow> rows;
        if (monitorIds.Count == 0)
        {
            rows = [];
        }
        else
        {
            rows = await dbContext.MonitorAvailabilities
                .AsNoTracking()
                .Where(row => monitorIds.Contains(row.MonitorId)
                    && row.Date >= queryStart
                    && row.Date < period.CurrentEnd)
                .Select(row => new AvailabilityRow(row.MonitorId, row.Date, row.UptimePercentage))
                .ToListAsync(ct);
        }

        var firstDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(period.CurrentStart, reportingTimeZone).Date);
        var lastDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(period.CurrentEnd, reportingTimeZone).Date);

        var monitorDayValues = rows
            .Where(row => row.UptimePercentage.HasValue)
            .GroupBy(row => new
            {
                row.MonitorId,
                Date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(row.Timestamp, reportingTimeZone).Date)
            })
            .Where(group => group.Key.Date >= firstDate && group.Key.Date <= lastDate)
            .Select(group => new
            {
                group.Key.MonitorId,
                group.Key.Date,
                Uptime = group.Average(row => row.UptimePercentage!.Value)
            })
            .ToList();

        var points = new List<MonitorAvailabilityPointDto>();
        var periodSpanDays = lastDate.DayNumber - firstDate.DayNumber;
        var bucketSizeDays = periodSpanDays > 90 ? 7 : 1;
        var periodStartsMidday = TimeZoneInfo.ConvertTime(period.CurrentStart, reportingTimeZone).TimeOfDay != TimeSpan.Zero;

        for (var bucketStart = firstDate; bucketStart <= lastDate;)
        {
            var bucketEnd = DateOnly.FromDayNumber(Math.Min(
                bucketStart.DayNumber + bucketSizeDays - 1,
                lastDate.DayNumber));
            var bucketValues = monitorDayValues
                .Where(value => value.Date >= bucketStart && value.Date <= bucketEnd)
                .ToList();
            var isPartial = bucketEnd == lastDate || (bucketStart == firstDate && periodStartsMidday);

            points.Add(new MonitorAvailabilityPointDto(
                bucketStart,
                bucketEnd,
                bucketValues.Count > 0 ? bucketValues.Average(value => value.Uptime) : null,
                bucketValues.Count > 0 ? bucketValues.Min(value => value.Uptime) : null,
                bucketValues.Select(value => value.MonitorId).Distinct().Count(),
                isPartial));

            bucketStart = bucketEnd.AddDays(1);
        }

        return Result.Ok(new MonitorAvailabilitySeriesDto(
            period.CurrentStart,
            period.CurrentEnd,
            monitorDayValues.Count > 0 ? monitorDayValues.Average(value => value.Uptime) : null,
            monitorDayValues.Count > 0 ? monitorDayValues.Min(value => value.Uptime) : null,
            points));
    }

    private async Task<List<LatencyRow>> GetMergedLatencyDataAsync(
        IApplicationDbContext db, DateTimeOffset start, DateTimeOffset end, bool isHourly, List<int>? allowedMonitorIds, CancellationToken ct = default)
    {
        var systemTenantIds = await GetSystemTenantIdsAsync(ct);

        if (isHourly)
        {
            var query = db.ResponseTimes.AsNoTracking().Where(rt => rt.Date >= start && rt.Date < end);
            if (allowedMonitorIds != null && allowedMonitorIds.Any())
                query = query.Where(rt => allowedMonitorIds.Contains(rt.MonitorId));
            else if (allowedMonitorIds != null && !allowedMonitorIds.Any())
                query = query.Where(rt => false);
            else
                query = query.Where(rt => !systemTenantIds.Contains(rt.UptimeMonitor!.TenantId));

            var raw = await query
                .Select(rt => new { rt.Date, rt.Average })
                .ToListAsync(ct);

            var grouped = raw
                .Select(row => new LatencyRow(row.Date, row.Average, row.Average, row.Average))
                .OrderBy(r => r.Timestamp)
                .ToList();
            return grouped;
        }

        var yesterday = new DateTimeOffset(DateTimeOffset.UtcNow.Date);
        var viewEnd = yesterday < end ? yesterday : end;

        var histQuery = db.DailyLatencyMonitorRollups.AsNoTracking().Where(r => r.Date >= start && r.Date < viewEnd);
        if (allowedMonitorIds != null && allowedMonitorIds.Any())
            histQuery = histQuery.Where(r => allowedMonitorIds.Contains(r.MonitorId));
        else if (allowedMonitorIds != null && !allowedMonitorIds.Any())
            histQuery = histQuery.Where(r => false);
        else
            histQuery = histQuery.Where(r => !systemTenantIds.Contains(r.UptimeMonitor.TenantId));

        var historical = await histQuery
            .Select(r => new LatencyRow(r.Date, r.Average, r.P10, r.P90))
            .ToListAsync(ct);

        if (yesterday < end)
        {
            var liveQuery = db.ResponseTimes.AsNoTracking().Where(rt => rt.Date >= yesterday && rt.Date < end);
            if (allowedMonitorIds != null && allowedMonitorIds.Any())
                liveQuery = liveQuery.Where(rt => allowedMonitorIds.Contains(rt.MonitorId));
            else if (allowedMonitorIds != null && !allowedMonitorIds.Any())
                liveQuery = liveQuery.Where(rt => false);
            else
                liveQuery = liveQuery.Where(rt => !systemTenantIds.Contains(rt.UptimeMonitor!.TenantId));

            var liveRaw = await liveQuery
                .Select(rt => new { rt.Date, rt.Average })
                .ToListAsync(ct);

            var fresh = liveRaw
                .GroupBy(rt => new DateTimeOffset(rt.Date.Year, rt.Date.Month, rt.Date.Day, 0, 0, 0, TimeSpan.Zero))
                .Select(g => {
                    var avgList = g.Where(x => x.Average.HasValue).Select(x => x.Average!.Value).ToList();
                    return new LatencyRow(
                        g.Key,
                        avgList.Count > 0 ? avgList.Average() : null,
                        CalculatePercentile(avgList, 0.10),
                        CalculatePercentile(avgList, 0.90)
                    );
                })
                .ToList();
            historical.AddRange(fresh);
        }

        return historical;
    }

    private sealed record LatencyBin(double Avg, double? P10, double? P90);

    private static Dictionary<int, LatencyBin> BinLatencyRows(
        IEnumerable<LatencyRow> rows,
        DateTimeOffset periodStart,
        TimeSpan binSize,
        int steps,
        bool calculateRawPercentiles)
    {
        return rows
            .Where(row => row.Average.HasValue)
            .Select(row => new
            {
                Row = row,
                Index = (int)((row.Timestamp - periodStart).Ticks / binSize.Ticks)
            })
            .Where(item => item.Index >= 0 && item.Index < steps)
            .GroupBy(item => item.Index)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var rowsInBin = group.Select(item => item.Row).ToList();
                    var averages = rowsInBin.Select(row => row.Average!.Value).ToList();
                    return new LatencyBin(
                        averages.Average(),
                        calculateRawPercentiles
                            ? CalculatePercentile(averages, 0.10)
                            : AveragePresent(rowsInBin.Select(row => row.P10)),
                        calculateRawPercentiles
                            ? CalculatePercentile(averages, 0.90)
                            : AveragePresent(rowsInBin.Select(row => row.P90)));
                });
    }

    private static double? AveragePresent(IEnumerable<double?> values)
    {
        var present = values.Where(value => value.HasValue).Select(value => value!.Value).ToList();
        return present.Count == 0 ? null : present.Average();
    }

    private static List<UptimeMonitor> ApplyMonitorFilters(
        IEnumerable<UptimeMonitor> monitors,
        string[]? includedTags,
        string[]? includedStatuses,
        string[]? excludedTags,
        string[]? excludedStatuses)
    {
        var filtered = monitors;

        if (includedTags is { Length: > 0 })
        {
            filtered = filtered.Where(monitor =>
                monitor.Tags != null
                && monitor.Tags.Intersect(includedTags, StringComparer.OrdinalIgnoreCase).Any());
        }

        if (includedStatuses is { Length: > 0 })
        {
            filtered = filtered.Where(monitor =>
                includedStatuses.Contains(monitor.StatusStr, StringComparer.OrdinalIgnoreCase));
        }

        if (excludedTags is { Length: > 0 })
        {
            filtered = filtered.Where(monitor =>
                monitor.Tags == null
                || !monitor.Tags.Intersect(excludedTags, StringComparer.OrdinalIgnoreCase).Any());
        }

        if (excludedStatuses is { Length: > 0 })
        {
            filtered = filtered.Where(monitor =>
                !excludedStatuses.Contains(monitor.StatusStr, StringComparer.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    public async Task<Result<UptimeMonitor>> CreateUnassignedMonitorAsync(string name, string url, string? type, double? uptimeSla, CancellationToken ct = default, int? latencyDegradedFloor = null)
    {
        var filter = OrganizationFilter.From(currentAccess.Scope);
        if (filter.Denied)
        {
            return Result.Fail<UptimeMonitor>(ScopeDenied());
        }

        var organizationId = filter.OrganizationId ?? IApplicationDbContext.DefaultOrganizationGuid;
        var bucketId = await dbContext.Tenants
            .Where(tenant => tenant.OrganizationId == organizationId && tenant.IsSystem)
            .Select(tenant => (Guid?)tenant.Id)
            .SingleOrDefaultAsync(ct);
        if (bucketId is null)
            return Result.Fail<UptimeMonitor>(new ConfigurationError("The organization has no unassigned monitor bucket."));
        return await CreateMonitorAsync(bucketId.Value, name, url, type, uptimeSla, ct, latencyDegradedFloor);
    }

    public async Task<Result<UptimeMonitor>> CreateMonitorAsync(Guid tenantId, string name, string url, string? type, double? uptimeSla, CancellationToken ct = default, int? latencyDegradedFloor = null)
    {
        var tenantResult = await GetVisibleTenantForMutationAsync(tenantId, ct);
        if (tenantResult.IsFailed) return Result.Fail<UptimeMonitor>(tenantResult.Errors);
        var tenant = tenantResult.Value;
        var normalizedType = UptimeMonitorTypes.Normalize(type);
        var providerResult = await GetConfiguredMonitoringProviderAsync(tenant.OrganizationId, ct);
        if (providerResult.IsFailed) return Result.Fail<UptimeMonitor>(providerResult.Errors);
        var monitoringProvider = providerResult.Value;
        var remoteMonitor = await monitoringProvider.CreateMonitorAsync(tenant.OrganizationId, name, url, normalizedType);
        
        var monitor = new UptimeMonitor
        {
            Provider = monitoringProvider.Provider,
            ExternalId = remoteMonitor.ExternalId,
            Type = remoteMonitor.Type,
            TenantId = tenantId,
            Name = remoteMonitor.Name,
            Url = remoteMonitor.Url,
            UpdateInterval = remoteMonitor.UpdateInterval,
            HttpMethod = remoteMonitor.HttpMethod,
            TimeoutSeconds = remoteMonitor.TimeoutSeconds,
            SslExpiresAt = remoteMonitor.SslExpiresAt,
            DomainExpiresAt = remoteMonitor.DomainExpiresAt,
            MonitoredRegions = remoteMonitor.MonitoredRegions ?? [],
            CurrentStateDurationSeconds = remoteMonitor.CurrentStateDurationSeconds,
            LastIncidentId = remoteMonitor.LastIncident?.ExternalId,
            LastIncidentStatus = remoteMonitor.LastIncident?.Status,
            LastIncidentCause = remoteMonitor.LastIncident?.Cause,
            LastIncidentReason = remoteMonitor.LastIncident?.Reason,
            LastIncidentStartedAt = remoteMonitor.LastIncident?.StartedAt,
            LastIncidentDurationSeconds = remoteMonitor.LastIncident?.DurationSeconds,
            UptimeSla = uptimeSla,
            LatencyDegradedFloor = latencyDegradedFloor,
            UptimeMonitorEnabled = true,
            CreatedDate = remoteMonitor.CreatedDate,
            StatusStr = remoteMonitor.Status,
            Tags = remoteMonitor.Tags,
            Tenant = tenant
        };

        dbContext.Monitors.Add(monitor);
        await dbContext.SaveChangesAsync(ct);

        HydrateLiveStatus(monitor);
        return Result.Ok(monitor);
    }

    public async Task<Result> AssignMonitorAsync(int monitorId, Guid tenantId, CancellationToken ct = default)
    {
        var tenantResult = await GetVisibleTenantForMutationAsync(tenantId, ct);
        if (tenantResult.IsFailed) return Result.Fail(tenantResult.Errors);
        var monitorResult = await GetVisibleMonitorForMutationAsync(monitorId, ct);
        if (monitorResult.IsFailed) return Result.Fail(monitorResult.Errors);

        monitorResult.Value.TenantId = tenantId;
        await dbContext.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task ReassignAllTenantMonitorsToSystemAsync(Guid tenantId, CancellationToken ct = default)
    {
        var visibleTenantIds = await GetVisibleTenantIdsAsync(ct);
        ValidateTenantInScope(tenantId, visibleTenantIds);

        var organizationId = await dbContext.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => (Guid?)t.OrganizationId)
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Tenant {tenantId} not found.");
        var bucketId = await ResolveOrganizationBucketIdAsync(organizationId, ct);

        var monitors = await dbContext.Monitors
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(ct);

        foreach (var m in monitors)
        {
            m.TenantId = bucketId;
        }
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<Result> UnassignMonitorAsync(int monitorId, CancellationToken ct = default)
    {
        var monitorResult = await GetVisibleMonitorForMutationAsync(monitorId, ct);
        if (monitorResult.IsFailed) return Result.Fail(monitorResult.Errors);
        var monitor = monitorResult.Value;

        var organizationId = await dbContext.Tenants
            .Where(t => t.Id == monitor.TenantId)
            .Select(t => t.OrganizationId)
            .SingleOrDefaultAsync(ct);
        var bucketId = await dbContext.Tenants
            .Where(tenant => tenant.OrganizationId == organizationId && tenant.IsSystem)
            .Select(tenant => (Guid?)tenant.Id)
            .SingleOrDefaultAsync(ct);
        if (bucketId is null)
            return Result.Fail(new ConfigurationError("The organization has no unassigned monitor bucket."));

        monitor.TenantId = bucketId.Value;
        await dbContext.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<IEnumerable<UptimeMonitor>> GetUnassignedMonitorsAsync(ResolvedPeriod period, CancellationToken ct = default)
    {
        var filter = OrganizationFilter.From(currentAccess.Scope);
        if (filter.Denied) return [];

        IQueryable<Tenant> buckets = dbContext.Tenants.AsNoTracking().Where(t => t.IsSystem);
        if (filter.OrganizationId is { } orgId)
            buckets = buckets.Where(t => t.OrganizationId == orgId);
        if (filter.TenantId is { } pinned)
            buckets = buckets.Where(t => t.Id == pinned);
        var bucketIds = await buckets.Select(t => t.Id).ToListAsync(ct);

        var start = period.CurrentStart;
        var end = period.CurrentEnd;

        var monitors = await dbContext.Monitors
            .AsNoTracking()
            .Include(m => m.Tenant)
            .Where(m => bucketIds.Contains(m.TenantId))
            .ToListAsync(ct);

        var monitorIds = monitors.Select(m => m.Id).ToList();
        var uptimes = await GetPeriodUptimesAsync(start, end, monitorIds, ct);

        foreach (var m in monitors)
        {
            HydrateLiveStatus(m);
            if (uptimes.TryGetValue(m.Id, out var uptime))
            {
                m.CurrentUptimePercentage = uptime;
            }
        }

        return monitors;
    }

    public async Task<Result<IReadOnlyList<UptimeMonitor>>> GetMonitorsAsync(ResolvedPeriod period, Guid? tenantId = null, CancellationToken ct = default)
    {
        var start = period.CurrentStart;
        var end = period.CurrentEnd;

        var visibleTenantIds = await GetVisibleTenantIdsAsync(ct);
        if (tenantId.HasValue && visibleTenantIds is not null && !visibleTenantIds.Contains(tenantId.Value))
            return Result.Fail<IReadOnlyList<UptimeMonitor>>(new ScopeDeniedError("the requested tenant", "the current scope"));

        IQueryable<UptimeMonitor> query = dbContext.Monitors
            .AsNoTracking()
            .Include(m => m.Tenant);

        if (tenantId.HasValue)
            query = query.Where(m => m.TenantId == tenantId.Value);
        else
        {
            var systemTenantIds = await GetSystemTenantIdsAsync(ct);
            query = query.Where(m => !systemTenantIds.Contains(m.TenantId));
            if (visibleTenantIds is not null)
                query = query.Where(m => visibleTenantIds.Contains(m.TenantId));
        }

        var monitors = await query.ToListAsync(ct);
        var monitorIds = monitors.Select(m => m.Id).ToList();

        var uptimes = await GetPeriodUptimesAsync(start, end, monitorIds, ct);

        foreach (var m in monitors)
        {
            HydrateLiveStatus(m);
            if (uptimes.TryGetValue(m.Id, out var uptime))
            {
                m.CurrentUptimePercentage = uptime;
            }
        }

        return Result.Ok<IReadOnlyList<UptimeMonitor>>(monitors);
    }

    public async Task<Result<UptimeMonitor>> GetMonitorAsync(Guid tenantId, int id, ResolvedPeriod period, CancellationToken ct = default)
    {
        var visibleTenantIds = await GetVisibleTenantIdsAsync(ct);
        if (visibleTenantIds is not null && !visibleTenantIds.Contains(tenantId))
            return Result.Fail<UptimeMonitor>(new ScopeDeniedError("the requested tenant", "the current scope"));

        var start = period.CurrentStart;
        var end = period.CurrentEnd;

        var monitor = await dbContext.Monitors
            .AsNoTracking()
            .Include(m => m.Tenant)
            .SingleOrDefaultAsync(m => m.TenantId == tenantId && m.Id == id, ct);

        if (monitor == null) return Result.Fail<UptimeMonitor>(new NotFoundError("monitor", id));

        var uptimes = await GetPeriodUptimesAsync(start, end, new List<int> { id }, ct);
        if (uptimes.TryGetValue(id, out var uptime))
        {
            monitor.CurrentUptimePercentage = uptime;
        }

        HydrateLiveStatus(monitor);
        return Result.Ok(monitor);
    }
    
    private void HydrateLiveStatus(UptimeMonitor monitor)
    {
        if (cache.TryGetValue(GlobalCacheKeys.MonitorState(monitor.Id), out LiveMonitorState? state) && state != null)
        {
            monitor.StatusStr = FormatStatus(state.StatusStr);
            monitor.CurrentLatency = state.CurrentLatency;
        }
        else
        {
            if (monitor.Id <= 0)
            {
                monitor.StatusStr = "Up";
                monitor.CurrentLatency = 140 + Math.Abs((long)monitor.Id % 80);
            }
        }
    }

    private static string FormatStatus(string status) 
    {
        if (string.IsNullOrWhiteSpace(status)) return "Unknown";
        return status.Length > 1 
            ? char.ToUpper(status[0]) + status.Substring(1).ToLower() 
            : status;
    }

    public async Task<Result> DeleteMonitorAsync(int id, CancellationToken ct = default)
    {
        var monitorResult = await GetVisibleMonitorForMutationAsync(id, ct);
        if (monitorResult.IsFailed) return Result.Fail(monitorResult.Errors);
        var monitor = monitorResult.Value;

        if (id > 0)
        {
            var organizationId = await GetMonitorOrganizationIdAsync(monitor, ct);
            var providerResult = await GetConfiguredMonitoringProviderAsync(organizationId, ct, monitor.Provider);
            if (providerResult.IsFailed) return Result.Fail(providerResult.Errors);
            await providerResult.Value.DeleteMonitorAsync(organizationId, monitor.ExternalId);
        }

        dbContext.Monitors.Remove(monitor);
        await dbContext.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> PauseMonitorAsync(int id, CancellationToken ct = default)
    {
        var monitorResult = await GetVisibleMonitorForMutationAsync(id, ct);
        if (monitorResult.IsFailed) return Result.Fail(monitorResult.Errors);
        var monitor = monitorResult.Value;

        if (id > 0)
        {
            var organizationId = await GetMonitorOrganizationIdAsync(monitor, ct);
            var providerResult = await GetConfiguredMonitoringProviderAsync(organizationId, ct, monitor.Provider);
            if (providerResult.IsFailed) return Result.Fail(providerResult.Errors);
            await providerResult.Value.PauseMonitorAsync(organizationId, monitor.ExternalId);
        }
        
        monitor.UptimeMonitorEnabled = false;
        await dbContext.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> StartMonitorAsync(int id, CancellationToken ct = default)
    {
        var monitorResult = await GetVisibleMonitorForMutationAsync(id, ct);
        if (monitorResult.IsFailed) return Result.Fail(monitorResult.Errors);
        var monitor = monitorResult.Value;

        if (id > 0)
        {
            var organizationId = await GetMonitorOrganizationIdAsync(monitor, ct);
            var providerResult = await GetConfiguredMonitoringProviderAsync(organizationId, ct, monitor.Provider);
            if (providerResult.IsFailed) return Result.Fail(providerResult.Errors);
            await providerResult.Value.StartMonitorAsync(organizationId, monitor.ExternalId);
        }
        
        monitor.UptimeMonitorEnabled = true;
        await dbContext.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result<IEnumerable<ResponseTime>>> GetAggregatedLatencyAsync(Guid tenantId, int id, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var visibleTenantIds = await GetVisibleTenantIdsAsync(ct);
        if (visibleTenantIds is not null && !visibleTenantIds.Contains(tenantId))
            return Result.Fail<IEnumerable<ResponseTime>>(new ScopeDeniedError("the requested tenant", "the current scope"));

        var monitorExists = await dbContext.Monitors
            .AsNoTracking()
            .AnyAsync(m => m.TenantId == tenantId && m.Id == id, ct);

        if (!monitorExists)
        {
            return Result.Fail<IEnumerable<ResponseTime>>(new NotFoundError("monitor", id));
        }

        var responseTimes = await dbContext.ResponseTimes
            .AsNoTracking()
            .Where(rt => rt.MonitorId == id)
            .Where(rt => rt.Date >= from && rt.Date <= to)
            .OrderBy(rt => rt.Date)
            .ToListAsync(ct);
        return Result.Ok<IEnumerable<ResponseTime>>(responseTimes);
    }

    public async Task<Result<UptimeMonitor>> UpdateMonitorAsync(int id, string? name, string? url, string? type, double? uptimeSla, List<string>? tags, CancellationToken ct = default, int? latencyDegradedFloor = null)
    {
        var monitorResult = await GetVisibleMonitorForMutationAsync(id, ct);
        if (monitorResult.IsFailed) return Result.Fail<UptimeMonitor>(monitorResult.Errors);
        var monitor = monitorResult.Value;

        List<string>? cleanedTags = null;
        if (tags != null)
        {
            cleanedTags = tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .ToList();
        }

        bool nameChanged = name != null && name != monitor.Name;
        bool urlChanged = url != null && url != monitor.Url;
        var normalizedType = type is null ? null : UptimeMonitorTypes.Normalize(type);
        bool typeChanged = normalizedType != null && normalizedType != monitor.Type;
        bool tagsChanged = cleanedTags != null && !cleanedTags.SequenceEqual(monitor.Tags);

        if (id > 0 && (nameChanged || urlChanged || typeChanged || tagsChanged))
        {
            var organizationId = await GetMonitorOrganizationIdAsync(monitor, ct);
            var providerResult = await GetConfiguredMonitoringProviderAsync(organizationId, ct, monitor.Provider);
            if (providerResult.IsFailed) return Result.Fail<UptimeMonitor>(providerResult.Errors);
            await providerResult.Value.UpdateMonitorAsync(
                organizationId,
                monitor.ExternalId,
                nameChanged ? name : null,
                urlChanged ? url : null,
                typeChanged ? normalizedType : null,
                tagsChanged ? cleanedTags : null);
        }

        if (nameChanged)
        {
            monitor.Name = name!;
        }
        if (urlChanged)
        {
            monitor.Url = url!;
        }
        if (typeChanged)
        {
            monitor.Type = normalizedType!;
        }
        if (tagsChanged && cleanedTags != null)
        {
            monitor.Tags = cleanedTags;
        }

        if (uptimeSla.HasValue)
        {
            monitor.UptimeSla = uptimeSla;
        }

        if (latencyDegradedFloor.HasValue)
        {
            monitor.LatencyDegradedFloor = latencyDegradedFloor;
        }

        await dbContext.SaveChangesAsync(ct);

        HydrateLiveStatus(monitor);
        return Result.Ok(monitor);
    }

    private static double? CalculateGrowthPercentage(double? current, double? previous)
    {
        if (!current.HasValue || !previous.HasValue || previous == 0)
            return null;

        return Math.Round((current.Value - previous.Value) / previous.Value * 100, 2);
    }

    private static double? CalculatePercentile(List<double> values, double percentile)
    {
        if (values == null || values.Count == 0) return null;
        values.Sort();
        var idx = (int)Math.Round(percentile * (values.Count - 1));
        return values[idx];
    }
}
