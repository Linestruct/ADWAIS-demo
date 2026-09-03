// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Collections.Immutable;
using System.Text.Json;
using Adwais.Application.Common.Models;
using Adwais.Application.DTOs.Financial;
using Adwais.Application.Interfaces;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Common.Access;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Application.Services;

/// <summary>
/// Provides financial analytics and KPI calculations by merging historical rollup data with real-time order data.
/// </summary>
public class FinancialService(
    IApplicationDbContext dbContext,
    IReportingCalendar reportingCalendar,
    ICurrentAccess currentAccess,
    IFinancialSeriesReader seriesReader) : IFinancialService
{
    private const int DensityBucketCount = 7 * 24;
    private const int SparseDensityThreshold = DensityBucketCount * 5;
    private const int StableDensityThreshold = DensityBucketCount * 20;
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IReportingCalendar _reportingCalendar = reportingCalendar;
    private readonly ICurrentAccess _currentAccess = currentAccess;
    private readonly IFinancialSeriesReader _seriesReader = seriesReader;

    /// <summary>
    /// The tenant ids the current scope may see. Null means no restriction
    /// (platform admin). An empty array means no access.
    /// </summary>
    private async Task<Guid[]?> GetVisibleTenantIdsAsync(IApplicationDbContext context, CancellationToken ct)
    {
        var filter = OrganizationFilter.From(_currentAccess.Scope);
        return await TenantVisibility.ResolveAsync(filter, context.Tenants, ct);
    }

    #region Widget Implementations

    /// <inheritdoc />
    public async Task<KpiDto> GetKpisAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var previousStart = period.PreviousStart;
        var isHourly = period.IsHourly;
        var context = _dbContext;

        var visibleTenantIds = await GetVisibleTenantIdsAsync(context, ct);

        var currentRows = await _seriesReader.ReadTenantSeriesAsync(currentStart, currentEnd, isHourly, TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds), ct);
        var previousRows = await _seriesReader.ReadTenantSeriesAsync(previousStart, period.PreviousEnd, isHourly, TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds), ct);

        var currentRevenue = currentRows.Sum(r => r.Revenue);
        var previousRevenue = previousRows.Sum(r => r.Revenue);
        var volume = currentRows.Sum(r => r.Volume);
        var previousVolume = previousRows.Sum(r => r.Volume);
        
        var activeTenants = currentRows.Where(r => r.Revenue > 0 && r.TenantId.HasValue).Select(r => r.TenantId!.Value).Distinct().Count();
        var prevActiveTenants = previousRows.Where(r => r.Revenue > 0 && r.TenantId.HasValue).Select(r => r.TenantId!.Value).Distinct().Count();

        var growthPct = FinancialMath.CalculateGrowthPercentage(currentRevenue, previousRevenue);
        var volumeGrowthPct = FinancialMath.CalculateGrowthPercentage(volume, previousVolume);
        var activeTenantsGrowthPct = FinancialMath.CalculateGrowthPercentage(activeTenants, prevActiveTenants);
        
        var aov = volume > 0 ? Math.Round(currentRevenue / volume, 2) : 0m;
        var previousAov = previousVolume > 0 ? Math.Round(previousRevenue / previousVolume, 2) : 0m;
        var aovGrowthPct = FinancialMath.CalculateGrowthPercentage(aov, previousAov);

        var arpt = activeTenants > 0 ? Math.Round(currentRevenue / activeTenants, 2) : 0m;
        var prevArpt = prevActiveTenants > 0 ? Math.Round(previousRevenue / prevActiveTenants, 2) : 0m;
        var arptGrowthPct = FinancialMath.CalculateGrowthPercentage(arpt, prevArpt);

        return new KpiDto(currentRevenue, previousRevenue, growthPct, volume, volumeGrowthPct, aov, aovGrowthPct, activeTenants, activeTenantsGrowthPct, arpt, arptGrowthPct);
    }
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<AccumulatedRevenuePointDto>> GetAccumulatedRevenueAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var previousStart = period.PreviousStart;
        var steps = period.StepsInPeriod;
        var isHourly = period.IsHourly;
        var includeActualTime = period.IncludeActualTime;
        var context = _dbContext;

        var visibleTenantIds = await GetVisibleTenantIdsAsync(context, ct);

        var currentRows = await _seriesReader.ReadTenantSeriesAsync(currentStart, currentEnd, isHourly, TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds), ct);
        var previousRows = await _seriesReader.ReadTenantSeriesAsync(previousStart, period.PreviousEnd, isHourly, TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds), ct);
        var tenantTypeMap = await context.Tenants.ToDictionaryAsync(t => t.Id, t => t.Type, ct);

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
        var context = _dbContext;

        var visibleTenantIds = await GetVisibleTenantIdsAsync(context, ct);

        var currentRows = await _seriesReader.ReadTenantSeriesAsync(currentStart, currentEnd, isHourly, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);
        var previousRows = await _seriesReader.ReadTenantSeriesAsync(previousStart, period.PreviousEnd, isHourly, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);

        var filter = TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds);
        var tenantDetails = await filter.ApplyToTenants(context.Tenants
            .AsNoTracking()
            .Where(t => !t.IsSystem))
            .Select(t => new { t.Id, t.Name, t.Type, t.OrderProviderSettings })
            .ToDictionaryAsync(t => t.Id, ct);

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

                return new RevenueEfficiencyTenantDto(tid, details.Name, details.Type, aov, curVol, share, growth, GetEndpoint(details.OrderProviderSettings));
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
    public async Task<CrossSegmentDistributionDto> GetCrossSegmentDistributionAsync(ResolvedPeriod period, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var isHourly = period.IsHourly;
        var context = _dbContext;

        var visibleTenantIds = await GetVisibleTenantIdsAsync(context, ct);

        var currentRows = await _seriesReader.ReadTenantSeriesAsync(currentStart, currentEnd, isHourly, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);
        var filter = TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds);
        var tenantDetails = await filter.ApplyToTenants(context.Tenants
            .AsNoTracking()
            .Where(t => !t.IsSystem))
            .Select(t => new { t.Id, t.Name, t.Type, t.OrderProviderSettings })
            .ToDictionaryAsync(t => t.Id, ct);

        var currentByTenant = currentRows
            .Where(r => r.TenantId.HasValue && r.TenantId.Value != IApplicationDbContext.SystemTenantGuid)
            .GroupBy(r => r.TenantId!.Value)
            .ToDictionary(g => g.Key, g => new { Revenue = g.Sum(r => r.Revenue), Volume = g.Sum(r => r.Volume) });

        var totalCurrentRevenue = currentByTenant.Values.Sum(x => x.Revenue);

        var rawTenants = currentByTenant.Keys
            .Where(tid => tenantDetails.ContainsKey(tid))
            .Select(tid =>
            {
                var current = currentByTenant[tid];
                var curRev = current.Revenue;
                var curVol = current.Volume;
                var aov = curVol > 0 ? Math.Round(curRev / curVol, 2) : 0m;
                var share = totalCurrentRevenue > 0 ? Math.Round((curRev / totalCurrentRevenue) * 100, 2) : 0m;
                var details = tenantDetails[tid];

                return new
                {
                    TenantId = tid,
                    Name = details.Name,
                    Type = details.Type,
                    Aov = aov,
                    Volume = curVol,
                    Revenue = curRev,
                    Share = share,
                    OrderProviderEndpoint = GetEndpoint(details.OrderProviderSettings)
                };
            })
            .ToList();

        var cohortTenantLists = rawTenants.GroupBy(t => t.Type).ToDictionary(
            g => g.Key,
            g => new
            {
                Aovs = g.Select(t => t.Aov).OrderBy(v => v).ToList(),
                Volumes = g.Select(t => (decimal)t.Volume).OrderBy(v => v).ToList(),
                Revenues = g.Select(t => t.Revenue).OrderBy(v => v).ToList()
            });

        var tenants = rawTenants.Select(t =>
        {
            var cohortData = cohortTenantLists.GetValueOrDefault(t.Type);
            var aovRank = cohortData != null ? FinancialMath.CalculatePercentileRank(cohortData.Aovs, t.Aov) : 50;
            var volRank = cohortData != null ? FinancialMath.CalculatePercentileRank(cohortData.Volumes, t.Volume) : 50;
            var revRank = cohortData != null ? FinancialMath.CalculatePercentileRank(cohortData.Revenues, t.Revenue) : 50;

            return new CrossSegmentCohortTenantDto(
                t.TenantId,
                t.Name,
                t.Type,
                t.Aov,
                t.Volume,
                t.Revenue,
                t.Share,
                aovRank,
                volRank,
                revRank,
                t.OrderProviderEndpoint);
        }).ToList();

        var cohortGroups = tenants
            .GroupBy(t => t.Type)
            .Select(g =>
            {
                var groupList = g.ToList();
                var aovs = groupList.Select(t => t.AverageOrderValue).OrderBy(v => v).ToList();
                var volumes = groupList.Select(t => (decimal)t.OrderVolume).OrderBy(v => v).ToList();
                var revenues = groupList.Select(t => t.PeriodRevenue).OrderBy(v => v).ToList();

                var (q1Aov, medianAov, q3Aov) = FinancialMath.CalculateQuartiles(aovs);
                var (q1Vol, medianVol, q3Vol) = FinancialMath.CalculateQuartiles(volumes);
                var (q1Rev, medianRev, q3Rev) = FinancialMath.CalculateQuartiles(revenues);

                return new CrossSegmentCohortGroupDto(
                    g.Key,
                    groupList.Count,
                    medianAov, q1Aov, q3Aov,
                    medianVol, q1Vol, q3Vol,
                    medianRev, q1Rev, q3Rev);
            })
            .ToList();

        return new CrossSegmentDistributionDto(cohortGroups, tenants);
    }

    /// <inheritdoc />
    public async Task<PortfolioImpactDto> GetPortfolioImpactAsync(ResolvedPeriod period, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var previousStart = period.PreviousStart;
        var isHourly = period.IsHourly;
        var context = _dbContext;

        var visibleTenantIds = await GetVisibleTenantIdsAsync(context, ct);

        var currentRows = await _seriesReader.ReadTenantSeriesAsync(currentStart, currentEnd, isHourly, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);
        var previousRows = await _seriesReader.ReadTenantSeriesAsync(previousStart, period.PreviousEnd, isHourly, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);
        var filter = TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds);
        var tenantDetails = await filter.ApplyToTenants(context.Tenants
            .AsNoTracking()
            .Where(t => !t.IsSystem))
            .Select(t => new { t.Id, t.Name, t.Type, t.OrderProviderSettings })
            .ToDictionaryAsync(t => t.Id, ct);

        var currentByTenant = currentRows
            .GroupBy(r => r.TenantId!.Value)
            .ToDictionary(g => g.Key, g => new { Revenue = g.Sum(r => r.Revenue), Volume = g.Sum(r => r.Volume) });

        var previousByTenant = previousRows
            .GroupBy(r => r.TenantId!.Value)
            .ToDictionary(g => g.Key, g => new { Revenue = g.Sum(r => r.Revenue), Volume = g.Sum(r => r.Volume) });

        var totalCurrentRevenue = currentByTenant.Values.Sum(value => value.Revenue);
        var totalPreviousRevenue = previousByTenant.Values.Sum(value => value.Revenue);
        var globalGrowthPct = FinancialMath.CalculateGrowthPercentage(totalCurrentRevenue, totalPreviousRevenue);

        var allTenantIds = currentByTenant.Keys.Union(previousByTenant.Keys).Where(tid => tenantDetails.ContainsKey(tid)).ToList();

        var tenants = allTenantIds
            .Select(tid =>
            {
                var current = currentByTenant.GetValueOrDefault(tid);
                var cur = current?.Revenue ?? 0m;
                var volume = current?.Volume ?? 0m;
                var previous = previousByTenant.GetValueOrDefault(tid);
                var prev = previous?.Revenue ?? 0m;
                var prevVolume = previous?.Volume ?? 0m;
                var growth = FinancialMath.CalculateGrowthPercentage(cur, prev);
                var volumeGrowth = FinancialMath.CalculateGrowthPercentage(volume, prevVolume);
                var share = totalCurrentRevenue > 0 ? Math.Round((cur / totalCurrentRevenue) * 100, 2) : 0m;
                var details = tenantDetails[tid];

                return new PortfolioImpactTenantDto(tid, details.Name, details.Type, prev, growth, cur, volume, volumeGrowth, share, GetEndpoint(details.OrderProviderSettings));
            })
            .ToList();

        var medianBaselineRevenue = tenants.Count > 0 
            ? FinancialMath.CalculateMedian(tenants.Select(t => t.BaselineRevenue).OrderBy(r => r).ToList()) 
            : 0m;

        var medianPortfolioShare = tenants.Count > 0
            ? FinancialMath.CalculateMedian(tenants.Select(t => t.PortfolioSharePercentage).OrderBy(r => r).ToList())
            : 0m;

        return new PortfolioImpactDto(medianBaselineRevenue, globalGrowthPct, medianPortfolioShare, tenants);
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
        var context = _dbContext;

        var roundedTotalHours = Math.Ceiling((currentEnd - currentStart).TotalHours);
        var binSizeHours = isHourly ? roundedTotalHours / steps : 24d;
        var lookbackStart = currentStart.AddHours(-binSizeHours);

        var visibleTenantIds = await GetVisibleTenantIdsAsync(context, ct);
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
    public async Task<IReadOnlyList<OrderBinDto>> GetOrderDistributionAsync(ResolvedPeriod period, Guid tenantId, int? binCount = null, CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var context = _dbContext;

        var visibleTenantIds = await GetVisibleTenantIdsAsync(context, ct);
        TenantSeriesFilter.Create(tenantId, null, visibleTenantIds).ThrowIfTenantOutsideScope();

        var orderValues = await context.Orders
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId)
            .Where(o => o.CreatedDate >= currentStart && o.CreatedDate < currentEnd)
            .Select(o => o.TotalValueExcVat)
            .ToListAsync(ct);

        if (orderValues.Count == 0)
            return Array.Empty<OrderBinDto>();

        orderValues.Sort();

        var effectiveBinCount = binCount ?? FinancialMath.CalculateAdaptiveBinCount(orderValues);
        effectiveBinCount = Math.Clamp(effectiveBinCount, 5, 30);

        var min = orderValues[0];
        
        // Cap the max bin range at the 99th percentile to prevent extreme outliers from creating long tails of empty bins.
        var p99Index = (int)Math.Floor(orderValues.Count * 0.99);
        var max = p99Index > 0 ? orderValues[p99Index] : orderValues[^1];

        if (min == max)
        {
            return new[]
            {
                new OrderBinDto(
                    FormatBinLabel(min, max),
                    min,
                    max,
                    orderValues.Count,
                    100m,
                    orderValues.Count)
            };
        }

        var binWidth = (max - min) / effectiveBinCount;

        // --- KDE & CDF Calculations ---
        var n = orderValues.Count;
        var mean = orderValues.Average();
        var variance = orderValues.Select(v => (double)(v - mean)).Select(v => v * v).Average();
        var stdDev = Math.Sqrt(variance);
        if (stdDev == 0) stdDev = 1;
        // Silverman's Rule of Thumb for bandwidth (h)
        var h = 1.06 * stdDev * Math.Pow(n, -0.2);

        // Scale KDE so that it visually matches the histogram bars. 
        // Histogram bar area = count. KDE density integral = 1. Scaled KDE = density * n * binWidth.
        var scaleFactor = n * (double)binWidth;

        var bins = new List<OrderBinDto>(effectiveBinCount);
        var runningSum = 0;

        for (var i = 0; i < effectiveBinCount; i++)
        {
            var binMin = min + i * binWidth;
            var binMax = i == effectiveBinCount - 1 ? max : min + (i + 1) * binWidth;

            var count = i == effectiveBinCount - 1
                ? orderValues.Count(v => v >= binMin) // Catch all values >= max boundary
                : orderValues.Count(v => v >= binMin && v < binMax);

            runningSum += count;
            var cumulativePercentage = (decimal)runningSum / n * 100m;

            // KDE evaluation at the midpoint of the bin
            var midpoint = (double)(binMin + binMax) / 2.0;
            double densitySum = 0;
            foreach (var value in orderValues)
            {
                var u = (midpoint - (double)value) / h;
                var k = 0.3989422804 * Math.Exp(-0.5 * u * u);
                densitySum += k;
            }
            var kdeValue = (densitySum / (n * h)) * scaleFactor;

            var label = i == effectiveBinCount - 1 
                ? $"{Math.Round(binMin, 0):N0}+ SEK" 
                : FormatBinLabel(binMin, binMax);

            bins.Add(new OrderBinDto(
                label,
                Math.Round(binMin, 2),
                Math.Round(binMax, 2),
                count,
                Math.Round(cumulativePercentage, 2),
                (decimal)Math.Round(kdeValue, 2)));
        }

        return bins;
    }

    /// <inheritdoc />
    public async Task<TransactionDensityDto> GetTransactionDensityAsync(TransactionDensityPeriod requestedPeriod, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default)
    {
        var context = _dbContext;
        var currentEnd = DateTimeOffset.UtcNow;
        var timeZone = await _reportingCalendar.GetTimeZoneAsync(ct);

        var visibleTenantIds = await GetVisibleTenantIdsAsync(context, ct);
        var filter = TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds);
        filter.ThrowIfTenantOutsideScope();

        var query = context.Orders
            .AsNoTracking()
            .Where(o => o.OrderState != OrderState.Cancelled);

        if (tenantId.HasValue)
            query = query.Where(o => o.TenantId == tenantId.Value);
        else
        {
            query = query.Where(o => o.TenantId != IApplicationDbContext.SystemTenantGuid);
            if (filter.HasTypeFilter)
            {
                query = query.Where(o => o.Tenant != null && filter.TenantTypes.Contains(o.Tenant.Type));
            }
        }
        query = filter.ApplyToOrders(query);

        var starts = new Dictionary<TransactionDensityPeriod, DateTimeOffset>
        {
            [TransactionDensityPeriod.T30] = GetDensityPeriodStart(currentEnd, 30, timeZone),
            [TransactionDensityPeriod.T90] = GetDensityPeriodStart(currentEnd, 90, timeZone),
            [TransactionDensityPeriod.T180] = GetDensityPeriodStart(currentEnd, 180, timeZone),
            [TransactionDensityPeriod.T365] = GetDensityPeriodStart(currentEnd, 365, timeZone)
        };

        var effectivePeriod = requestedPeriod;
        if (requestedPeriod == TransactionDensityPeriod.Auto)
        {
            var oldestStart = starts[TransactionDensityPeriod.T365];
            var sampleCounts = await query
                .Where(o => o.CreatedDate >= oldestStart && o.CreatedDate < currentEnd)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    T30 = group.Count(o => o.CreatedDate >= starts[TransactionDensityPeriod.T30]),
                    T90 = group.Count(o => o.CreatedDate >= starts[TransactionDensityPeriod.T90]),
                    T180 = group.Count(o => o.CreatedDate >= starts[TransactionDensityPeriod.T180]),
                    T365 = group.Count()
                })
                .SingleOrDefaultAsync(ct);

            effectivePeriod = sampleCounts switch
            {
                { T30: >= StableDensityThreshold } => TransactionDensityPeriod.T30,
                { T90: >= StableDensityThreshold } => TransactionDensityPeriod.T90,
                { T180: >= StableDensityThreshold } => TransactionDensityPeriod.T180,
                _ => TransactionDensityPeriod.T365
            };
        }

        var currentStart = starts[effectivePeriod];
        var utcHourlyData = await query
            .Where(o => o.CreatedDate >= currentStart && o.CreatedDate < currentEnd)
            .GroupBy(o => new
            {
                o.CreatedDate.Year,
                o.CreatedDate.Month,
                o.CreatedDate.Day,
                o.CreatedDate.Hour
            })
            .Select(g => new {
                g.Key.Year,
                g.Key.Month,
                g.Key.Day,
                g.Key.Hour,
                Count = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalValueExcVat)
            })
            .ToListAsync(ct);

        var dataMap = utcHourlyData
            .Select(point =>
            {
                var utcHour = new DateTimeOffset(point.Year, point.Month, point.Day, point.Hour, 0, 0, TimeSpan.Zero);
                var localHour = TimeZoneInfo.ConvertTime(utcHour, timeZone);
                return new
                {
                    DayOfWeek = (int)localHour.DayOfWeek,
                    localHour.Hour,
                    point.Count,
                    point.TotalRevenue
                };
            })
            .GroupBy(point => (point.DayOfWeek, point.Hour))
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Count = group.Sum(point => point.Count),
                    TotalRevenue = group.Sum(point => point.TotalRevenue)
                });
        var result = new List<TransactionDensityPointDto>(168);

        var days = new[] { 1, 2, 3, 4, 5, 6, 0 }; // 0 is Sunday in DayOfWeek

        foreach (var dow in days)
        {
            for (var h = 0; h < 24; h++)
            {
                if (dataMap.TryGetValue((dow, h), out var point))
                {
                    result.Add(new TransactionDensityPointDto(dow == 0 ? 7 : dow, h, point.Count, point.TotalRevenue));
                }
                else
                {
                    result.Add(new TransactionDensityPointDto(dow == 0 ? 7 : dow, h, 0, 0m));
                }
            }
        }

        var totalCount = result.Sum(point => point.Count);
        var sampleQuality = totalCount < SparseDensityThreshold
            ? TransactionDensitySampleQuality.Sparse
            : totalCount < StableDensityThreshold
                ? TransactionDensitySampleQuality.Indicative
                : TransactionDensitySampleQuality.Stable;

        return new TransactionDensityDto(
            totalCount,
            result.Count == 0 ? 0 : result.Min(point => point.Count),
            result.Count == 0 ? 0 : result.Max(point => point.Count),
            totalCount / (double)DensityBucketCount,
            sampleQuality,
            requestedPeriod,
            effectivePeriod,
            timeZone.Id,
            currentStart,
            currentEnd,
            result);
    }

    private static DateTimeOffset GetDensityPeriodStart(DateTimeOffset utcNow, int days, TimeZoneInfo timeZone)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var localStart = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Unspecified)
            .AddDays(-(days - 1));
        return TimeframeResolver.ConvertLocalToUtc(localStart, timeZone);
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
        var context = _dbContext;

        var visibleTenantIds = await GetVisibleTenantIdsAsync(context, ct);

        IReadOnlyList<FinancialSeriesRow> currentRows, previousRows;

        if (tenantId.HasValue || tenantTypes is { Count: > 0 } || visibleTenantIds is not null)
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

    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(DateTimeOffset dateSince, DateTimeOffset dateUntil, int ceilingCount, CancellationToken ct)
    {
        var context = _dbContext;
        var visibleTenantIds = await GetVisibleTenantIdsAsync(context, ct);
        var filter = TenantSeriesFilter.Create(null, null, visibleTenantIds);

        var query = context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedDate >= dateSince
                        && o.CreatedDate <= dateUntil
                        && o.OrderState != OrderState.Cancelled
                        && o.TotalValueExcVat > 0m);
        query = filter.ApplyToOrders(query);

        return await query
            .OrderByDescending(o => o.CreatedDate)
            .Take(ceilingCount)
            .Select(p => new OrderDto(
                AdwaisOrderId: p.Id,
                OrderNumber: p.OrderNumber,
                AdwaisTenantId: p.TenantId,
                OrderState: p.OrderState,
                CreatedDate: p.CreatedDate,
                TotalValueIncVat: p.TotalValueIncVat,
                TotalValueExcVat: p.TotalValueExcVat,
                Currency: p.Currency,
                TenantName: p.Tenant != null ? p.Tenant.Name : null,
                Provider: p.Provider,
                ExternalId: p.ExternalId
                ))
            .ToListAsync(ct);
    }

    #endregion

    #region Helpers

    private static string FormatBinLabel(decimal min, decimal max)
    {
        return $"{min:N0}–{max:N0} SEK";
    }

    private static string? GetEndpoint(string? settings)
    {
        if (string.IsNullOrWhiteSpace(settings)) return null;
        try
        {
            using var document = JsonDocument.Parse(settings);
            return document.RootElement.TryGetProperty("endpointUrl", out var endpoint)
                ? endpoint.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    #endregion
}



