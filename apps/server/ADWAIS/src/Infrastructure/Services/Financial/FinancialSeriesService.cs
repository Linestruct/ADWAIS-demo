// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Common.Models;
using Adwais.Application.DTOs.Financial;
using Adwais.Application.Interfaces;
using Adwais.Application.Services;
using Adwais.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Time-series endpoints: accumulated revenue, revenue efficiency, net
/// growth addition, cumulative growth delta.
/// </summary>
public class FinancialSeriesService(
    IApplicationDbContext dbContext,
    ICurrentAccess currentAccess,
    IFinancialSeriesReader seriesReader) : IFinancialSeriesService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ICurrentAccess _currentAccess = currentAccess;
    private readonly IFinancialSeriesReader _seriesReader = seriesReader;

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccumulatedRevenuePointDto>> GetAccumulatedRevenueAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var previousStart = period.PreviousStart;
        var steps = period.StepsInPeriod;
        var isHourly = period.IsHourly;
        var includeActualTime = period.IncludeActualTime;

        var visibleTenantIds = await FinancialScope.ResolveVisibleTenantIdsAsync(_currentAccess, _dbContext, ct);

        var currentRows = await _seriesReader.ReadTenantSeriesAsync(currentStart, currentEnd, isHourly, TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds), ct);
        var previousRows = await _seriesReader.ReadTenantSeriesAsync(previousStart, period.PreviousEnd, isHourly, TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds), ct);
        var tenantTypeMap = await _dbContext.Tenants.ToDictionaryAsync(t => t.Id, t => t.Type, ct);

        var binnedSteps = steps;
        var roundedTotalHours = Math.Ceiling((currentEnd - currentStart).TotalHours);
        var binSizeHours = isHourly
            ? roundedTotalHours / binnedSteps
            : 24;

        var currentByStep = currentRows
            .GroupBy(r => (int)((r.Timestamp - currentStart).TotalHours / binSizeHours))
            .ToDictionary(g => g.Key, g => new {
                Total = g.Sum(r => r.Revenue),
                B2C = g.Where(r => r.TenantId.HasValue && tenantTypeMap.GetValueOrDefault(r.TenantId.Value) == TenantType.B2C).Sum(r => r.Revenue),
                B2B = g.Where(r => r.TenantId.HasValue && tenantTypeMap.GetValueOrDefault(r.TenantId.Value) == TenantType.B2B).Sum(r => r.Revenue),
                Mixed = g.Where(r => r.TenantId.HasValue && tenantTypeMap.GetValueOrDefault(r.TenantId.Value) == TenantType.Mixed).Sum(r => r.Revenue)
            });

        var previousByStep = previousRows
            .GroupBy(r => (int)((r.Timestamp - previousStart).TotalHours / binSizeHours))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Revenue));

        var points = new List<AccumulatedRevenuePointDto>(binnedSteps);
        decimal runningCur = 0;
        decimal runningPrev = 0;

        for (var i = 0; i < binnedSteps; i++)
        {
            var timestamp = isHourly 
                ? currentStart.AddHours((i + 1) * binSizeHours) 
                : currentStart.AddDays(i);

            if (isHourly && i == binnedSteps - 1)
            {
                timestamp = currentEnd;
            }
            
            var curRev = currentByStep.GetValueOrDefault(i);
            var totalRev = curRev?.Total ?? 0m;
            var prevRev = previousByStep.GetValueOrDefault(i, 0m);
            
            runningCur += totalRev;
            runningPrev += prevRev;

            points.Add(new AccumulatedRevenuePointDto(
                timestamp,
                totalRev,
                curRev?.B2C ?? 0m,
                curRev?.B2B ?? 0m,
                curRev?.Mixed ?? 0m,
                prevRev,
                runningCur,
                runningPrev));
        }

        return points;
    }
    
    /// <inheritdoc />
    public async Task<RevenueEfficiencyDto> GetRevenueEfficiencyAsync(ResolvedPeriod period, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var previousStart = period.PreviousStart;
        var isHourly = period.IsHourly;

        var visibleTenantIds = await FinancialScope.ResolveVisibleTenantIdsAsync(_currentAccess, _dbContext, ct);

        var currentRows = await _seriesReader.ReadTenantSeriesAsync(currentStart, currentEnd, isHourly, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);
        var previousRows = await _seriesReader.ReadTenantSeriesAsync(previousStart, period.PreviousEnd, isHourly, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);

        var tenantDetails = await FinancialTenantDetails.ReadAsync(
            _dbContext, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);

        var currentByTenant = currentRows
            .Where(r => r.TenantId.HasValue && r.TenantId.Value != IApplicationDbContext.SystemTenantGuid)
            .GroupBy(r => r.TenantId!.Value)
            .ToDictionary(g => g.Key, g => new { Revenue = g.Sum(r => r.Revenue), Volume = g.Sum(r => r.Volume) });

        var previousByTenant = previousRows
            .Where(r => r.TenantId.HasValue && r.TenantId.Value != IApplicationDbContext.SystemTenantGuid)
            .GroupBy(r => r.TenantId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Revenue));

        var totalRevenue = currentByTenant.Values.Sum(x => x.Revenue);
        var totalVolume = currentByTenant.Values.Sum(x => x.Volume);
        var globalAov = totalVolume > 0 ? Math.Round(totalRevenue / totalVolume, 2) : 0m;

        var tenants = currentByTenant.Keys.Union(previousByTenant.Keys)
            .Where(tid => tenantDetails.ContainsKey(tid))
            .Select(tid =>
            {
                var current = currentByTenant.GetValueOrDefault(tid);
                var curRev = current?.Revenue ?? 0m;
                var curVol = current?.Volume ?? 0;
                var prevRev = previousByTenant.GetValueOrDefault(tid, 0m);

                var aov = curVol > 0 ? Math.Round(curRev / curVol, 2) : 0m;
                var share = totalRevenue > 0 ? Math.Round((curRev / totalRevenue) * 100, 2) : 0m;
                var growth = FinancialMath.CalculateGrowthPercentage(curRev, prevRev);
                var details = tenantDetails[tid];

                return new RevenueEfficiencyTenantDto(tid, details.Name, details.Type, aov, curVol, share, growth, details.OrderProviderEndpoint);
            })
            .ToList();

        var activeOrderVolumes = tenants
            .Where(tenant => tenant.OrderVolume > 0)
            .Select(tenant => tenant.OrderVolume)
            .OrderBy(volume => volume)
            .ToList();
        var medianOrderVolume = activeOrderVolumes.Count > 0
            ? FinancialMath.CalculateMedian(activeOrderVolumes)
            : 0m;
        var medianPortfolioShare = tenants.Count > 0 
            ? FinancialMath.CalculateMedian(tenants.Select(t => t.PortfolioSharePercentage).OrderBy(r => r).ToList()) 
            : 0m;

        return new RevenueEfficiencyDto(globalAov, medianOrderVolume, medianPortfolioShare, tenants);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NetGrowthAdditionPointDto>> GetNetGrowthAdditionAsync(
        ResolvedPeriod period,
        Guid? tenantId = null,
        IReadOnlyCollection<TenantType>? tenantTypes = null,
        CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var steps = period.StepsInPeriod;
        var isHourly = period.IsHourly;

        var roundedTotalHours = Math.Ceiling((currentEnd - currentStart).TotalHours);
        var binSizeHours = isHourly ? roundedTotalHours / steps : 24d;
        var lookbackStart = currentStart.AddHours(-binSizeHours);

        var visibleTenantIds = await FinancialScope.ResolveVisibleTenantIdsAsync(_currentAccess, _dbContext, ct);
        var scopeFilter = TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds);
        scopeFilter.ThrowIfTenantOutsideScope();

        IReadOnlyList<FinancialSeriesRow> currentRows, beforeStartRows;
        if (scopeFilter.HasTenantFilter || scopeFilter.HasTypeFilter || scopeFilter.IsRestricted)
        {
            currentRows = await _seriesReader.ReadTenantSeriesAsync(currentStart, currentEnd, isHourly, TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds), ct);
            beforeStartRows = await _seriesReader.ReadTenantSeriesAsync(lookbackStart, currentStart, isHourly, TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds), ct);
        }
        else
        {
            currentRows = await _seriesReader.ReadGlobalSeriesAsync(currentStart, currentEnd, isHourly, ct: ct);
            beforeStartRows = await _seriesReader.ReadGlobalSeriesAsync(lookbackStart, currentStart, isHourly, ct: ct);
        }

        var previousValue = beforeStartRows.Sum(r => r.Revenue);

        var currentByStep = currentRows
            .GroupBy(r => (int)((r.Timestamp - currentStart).TotalHours / binSizeHours))
            .Where(g => g.Key >= 0 && g.Key < steps)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Revenue));

        var points = new List<NetGrowthAdditionPointDto>(steps);

        for (var i = 0; i < steps; i++)
        {
            var timestamp = currentStart.AddHours(i * binSizeHours);
            var cur = currentByStep.GetValueOrDefault(i, 0m);
            var delta = cur - previousValue;
            points.Add(new NetGrowthAdditionPointDto(timestamp, delta));
            previousValue = cur;
        }

        return points;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CumulativeGrowthDeltaPointDto>> GetCumulativeGrowthDeltaAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var previousStart = period.PreviousStart;
        var steps = period.StepsInPeriod;
        var isHourly = period.IsHourly;
        var includeActualTime = period.IncludeActualTime;

        var visibleTenantIds = await FinancialScope.ResolveVisibleTenantIdsAsync(_currentAccess, _dbContext, ct);
        var scopeFilter = TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds);
        scopeFilter.ThrowIfTenantOutsideScope();

        IReadOnlyList<FinancialSeriesRow> currentRows, previousRows;

        if (scopeFilter.HasTenantFilter || scopeFilter.HasTypeFilter || scopeFilter.IsRestricted)
        {
            currentRows = await _seriesReader.ReadTenantSeriesAsync(currentStart, currentEnd, isHourly, TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds), ct);
            previousRows = await _seriesReader.ReadTenantSeriesAsync(previousStart, period.PreviousEnd, isHourly, TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds), ct);
        }
        else
        {
            currentRows = await _seriesReader.ReadGlobalSeriesAsync(currentStart, currentEnd, isHourly, ct: ct);
            previousRows = await _seriesReader.ReadGlobalSeriesAsync(previousStart, period.PreviousEnd, isHourly, ct: ct);
        }

        var currentByStep = currentRows
            .GroupBy(r => isHourly ? (int)(r.Timestamp - currentStart).TotalHours : (int)(r.Timestamp - currentStart).TotalDays)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Revenue));

        var previousByStep = previousRows
            .GroupBy(r => isHourly ? (int)(r.Timestamp - previousStart).TotalHours : (int)(r.Timestamp - previousStart).TotalDays)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Revenue));

        var points = new List<CumulativeGrowthDeltaPointDto>(steps);
        decimal runningSum = 0;
        decimal runningCur = 0;
        decimal runningPrev = 0;

        for (var i = 0; i < steps; i++)
        {
            var timestamp = isHourly ? currentStart.AddHours(i) : currentStart.AddDays(i);

            var cur = currentByStep.GetValueOrDefault(i, 0m);
            var prev = previousByStep.GetValueOrDefault(i, 0m);
            var variance = cur - prev;
            runningSum += variance;
            runningCur += cur;
            runningPrev += prev;

            points.Add(new CumulativeGrowthDeltaPointDto(timestamp, runningCur, runningPrev, runningSum));
        }

        return points;
    }
}
