// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Common.Models;
using Adwais.Application.DTOs.Financial;
using Adwais.Application.Interfaces;
using Adwais.Application.Services;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using FluentResults;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Distribution endpoints: cross-segment distribution, portfolio impact,
/// order distribution, transaction density.
/// </summary>
public class FinancialDistributionService(
    IApplicationDbContext dbContext,
    IReportingCalendar reportingCalendar,
    ICurrentAccess currentAccess,
    IFinancialSeriesReader seriesReader) : IFinancialDistributionService
{
    private const int DensityBucketCount = 7 * 24;
    private const int SparseDensityThreshold = DensityBucketCount * 5;
    private const int StableDensityThreshold = DensityBucketCount * 20;
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IReportingCalendar _reportingCalendar = reportingCalendar;
    private readonly ICurrentAccess _currentAccess = currentAccess;
    private readonly IFinancialSeriesReader _seriesReader = seriesReader;

    /// <inheritdoc />
    public async Task<CrossSegmentDistributionDto> GetCrossSegmentDistributionAsync(ResolvedPeriod period, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var isHourly = period.IsHourly;

        var visibleTenantIds = await FinancialScope.ResolveVisibleTenantIdsAsync(_currentAccess, _dbContext, ct);

        var currentRows = await _seriesReader.ReadTenantSeriesAsync(currentStart, currentEnd, isHourly, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);
        var tenantDetails = await FinancialTenantDetails.ReadAsync(
            _dbContext, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);

        var currentByTenant = currentRows
            .Where(r => r.TenantId.HasValue)
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
                    OrderProviderEndpoint = details.OrderProviderEndpoint
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

        var visibleTenantIds = await FinancialScope.ResolveVisibleTenantIdsAsync(_currentAccess, _dbContext, ct);

        var currentRows = await _seriesReader.ReadTenantSeriesAsync(currentStart, currentEnd, isHourly, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);
        var previousRows = await _seriesReader.ReadTenantSeriesAsync(previousStart, period.PreviousEnd, isHourly, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);
        var tenantDetails = await FinancialTenantDetails.ReadAsync(
            _dbContext, TenantSeriesFilter.Create(null, tenantTypes, visibleTenantIds), ct);

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

                return new PortfolioImpactTenantDto(tid, details.Name, details.Type, prev, growth, cur, volume, volumeGrowth, share, details.OrderProviderEndpoint);
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
    public async Task<Result<IReadOnlyList<OrderBinDto>>> GetOrderDistributionAsync(ResolvedPeriod period, Guid tenantId, int? binCount = null, CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;

        var visibleTenantIds = await FinancialScope.ResolveVisibleTenantIdsAsync(_currentAccess, _dbContext, ct);
        if (TenantSeriesFilter.Create(tenantId, null, visibleTenantIds).IsExplicitTenantOutsideScope)
            return Result.Fail<IReadOnlyList<OrderBinDto>>(new ScopeDeniedError("the requested tenant", "the current scope"));

        var orderValues = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId)
            .Where(o => o.CreatedDate >= currentStart && o.CreatedDate < currentEnd)
            .Select(o => o.TotalValueExcVat)
            .ToListAsync(ct);

        if (orderValues.Count == 0)
            return Result.Ok<IReadOnlyList<OrderBinDto>>(Array.Empty<OrderBinDto>());

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

        return Result.Ok<IReadOnlyList<OrderBinDto>>(bins);
    }

    /// <inheritdoc />
    public async Task<Result<TransactionDensityDto>> GetTransactionDensityAsync(TransactionDensityPeriod requestedPeriod, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default)
    {
        var currentEnd = DateTimeOffset.UtcNow;
        var timeZone = await _reportingCalendar.GetTimeZoneAsync(ct);

        var visibleTenantIds = await FinancialScope.ResolveVisibleTenantIdsAsync(_currentAccess, _dbContext, ct);
        var filter = TenantSeriesFilter.Create(tenantId, tenantTypes, visibleTenantIds);
        if (filter.IsExplicitTenantOutsideScope)
            return Result.Fail<TransactionDensityDto>(new ScopeDeniedError("the requested tenant", "the current scope"));

        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderState != OrderState.Cancelled);

        if (tenantId.HasValue)
            query = query.Where(o => o.TenantId == tenantId.Value);
        else
        {
            // System tenants (unassigned buckets, legacy rows) drop out by flag.
            // Orders without a tenant lookup stay visible, as before.
            query = query.Where(o => o.Tenant == null || !o.Tenant.IsSystem);
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

        return Result.Ok(new TransactionDensityDto(
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
            result));
    }

    private static DateTimeOffset GetDensityPeriodStart(DateTimeOffset utcNow, int days, TimeZoneInfo timeZone)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var localStart = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Unspecified)
            .AddDays(-(days - 1));
        return TimeframeResolver.ConvertLocalToUtc(localStart, timeZone);
    }

    private static string FormatBinLabel(decimal min, decimal max)
    {
        return $"{min:N0}–{max:N0} SEK";
    }
}
