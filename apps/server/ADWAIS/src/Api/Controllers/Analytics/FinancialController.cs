// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Api.DTOs.Financial;
using Adwais.Api.Extensions;
using Adwais.Application.DTOs.Financial;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Analytics;

[ApiController]
[Route("api/financial")]
[Authorize(Policy = "KioskOrStaffAccess")]
public class FinancialController(
    IFinancialKpiService kpiService,
    IFinancialSeriesService seriesService,
    IFinancialDistributionService distributionService,
    IReportingCalendar reportingCalendar) : ControllerBase
{
    /// <summary>
    /// Revenue, growth %, transaction volume, AOV.
    /// Scopes to a single tenant if tenantId is provided, otherwise portfolio-wide.
    /// </summary>
    [HttpGet("kpis")]
    [ProducesResponseType(typeof(KpiResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KpiResponseDto>> GetKpis([FromQuery] FinancialRequestDto request, CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(request.Timeframe, request.Comparison, ct);
        var result = await kpiService.GetKpisAsync(period, request.TenantId, request.TenantTypes, ct);
        if (result.IsFailed) return result.ToProblem(HttpContext);
        var kpis = result.Value;
        return Ok(new KpiResponseDto(
            kpis.CurrentRevenue,
            kpis.PreviousRevenue,
            kpis.RevenueGrowthPercentage,
            kpis.TransactionVolume,
            kpis.VolumeGrowthPercentage,
            kpis.AverageOrderValue,
            kpis.AovGrowthPercentage,
            kpis.ActiveTenants,
            kpis.ActiveTenantsGrowthPercentage,
            kpis.AverageRevenuePerTenant,
            kpis.ArptGrowthPercentage));
    }

    /// <summary>
    /// Daily/Hourly time-series: current vs. previous period accumulated revenue.
    /// Scopes to a single tenant if tenantId is provided, otherwise portfolio-wide.
    /// </summary>
    [HttpGet("accumulated-revenue")]
    [ProducesResponseType(typeof(IEnumerable<AccumulatedRevenuePointResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<AccumulatedRevenuePointResponseDto>>> GetAccumulatedRevenue([FromQuery] FinancialRequestDto request, CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(request.Timeframe, request.Comparison, ct);
        var result = await seriesService.GetAccumulatedRevenueAsync(period, request.TenantId, request.TenantTypes, ct);
        if (result.IsFailed) return result.ToProblem(HttpContext);
        return Ok(result.Value.Select(v => new AccumulatedRevenuePointResponseDto(
                v.Timestamp,
                v.CurrentRevenue,
                v.CurrentRevenueB2C,
                v.CurrentRevenueB2B,
                v.CurrentRevenueMixed,
                v.PreviousRevenue,
                v.CurrentAccumulated,
                v.PreviousAccumulated)).ToList());
    }
    
    /// <summary>
    /// Scatter plot data: revenue efficiency across all tenants. X: order volume, Y: AOV, Bubble: portfolio revenue share.
    /// Portfolio view only.
    /// </summary>
    [HttpGet("revenue-efficiency")]
    public async Task<ActionResult<RevenueEfficiencyResponseDto>> GetRevenueEfficiency([FromQuery] PortfolioRequestDto request, CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(request.Timeframe, request.Comparison, ct);
        var result = await seriesService.GetRevenueEfficiencyAsync(period, request.TenantTypes, ct);
        return Ok(new RevenueEfficiencyResponseDto(
            result.GlobalAverageOrderValue,
            result.MedianOrderVolume,
            result.MedianPortfolioShare,
            result.Tenants.Select(r => new RevenueEfficiencyTenantResponseDto(
                r.TenantId,
                r.TenantName,
                r.Type,
                r.AverageOrderValue,
                r.OrderVolume,
                r.PortfolioSharePercentage,
                r.GrowthVelocity,
                r.OrderProviderEndpoint
            )).ToList()
        ));
    }

    /// <summary>
    /// Distribution metrics (AOV, Volume, Revenue, Q1/Q2/Q3 statistics) across business model cohorts.
    /// Portfolio view only.
    /// </summary>
    [HttpGet("cross-segment-distribution")]
    public async Task<ActionResult<CrossSegmentDistributionResponseDto>> GetCrossSegmentDistribution([FromQuery] PortfolioRequestDto request, CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(request.Timeframe, request.Comparison, ct);
        var result = await distributionService.GetCrossSegmentDistributionAsync(period, request.TenantTypes, ct);
        return Ok(new CrossSegmentDistributionResponseDto(
            result.Cohorts.Select(c => new CrossSegmentCohortGroupResponseDto(
                c.Type,
                c.TenantCount,
                c.MedianAov, c.Q1Aov, c.Q3Aov,
                c.MedianVolume, c.Q1Volume, c.Q3Volume,
                c.MedianRevenue, c.Q1Revenue, c.Q3Revenue
            )).ToList(),
            result.Tenants.Select(t => new CrossSegmentCohortTenantResponseDto(
                t.TenantId,
                t.TenantName,
                t.Type,
                t.AverageOrderValue,
                t.OrderVolume,
                t.PeriodRevenue,
                t.PortfolioSharePercentage,
                t.AovPercentileRank,
                t.VolumePercentileRank,
                t.RevenuePercentileRank,
                t.OrderProviderEndpoint
            )).ToList()
        ));
    }
    
    /// <summary>
    /// Scatter plot data for Portfolio Impact Matrix: Growth % across all tenants. X: Portfolio share %, Y: Revenue growth %, Bubble: Current revenue.
    /// Portfolio view only.
    /// </summary>
    [HttpGet("portfolio-impact")]
    public async Task<ActionResult<PortfolioImpactResponseDto>> GetPortfolioImpact([FromQuery] PortfolioRequestDto request, CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(request.Timeframe, request.Comparison, ct);
        var result = await distributionService.GetPortfolioImpactAsync(period, request.TenantTypes, ct);
        return Ok(new PortfolioImpactResponseDto(
            result.MedianBaselineRevenue,
            result.GlobalGrowthPercentage,
            result.MedianPortfolioShare,
            result.Tenants.Select(t => new PortfolioImpactTenantResponseDto(
                t.TenantId,
                t.TenantName,
                t.Type,
                t.BaselineRevenue,
                t.GrowthPercentage,
                t.CurrentRevenue,
                t.OrderVolume,
                t.VolumeGrowthPercentage,
                t.PortfolioSharePercentage,
                t.OrderProviderEndpoint)).ToList()));
    }

    /// <summary>
    /// Revenue change from one daily/hourly bucket to the next.
    /// Scopes to a single tenant if tenantId is provided, otherwise portfolio-wide.
    /// </summary>
    [HttpGet("daily-revenue-delta")]
    [ProducesResponseType(typeof(IEnumerable<NetGrowthAdditionPointResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<NetGrowthAdditionPointResponseDto>>> GetNetGrowthAddition([FromQuery] FinancialRequestDto request, CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(request.Timeframe, request.Comparison, ct);
        var result = await seriesService.GetNetGrowthAdditionAsync(period, request.TenantId, request.TenantTypes, ct);
        if (result.IsFailed) return result.ToProblem(HttpContext);
        return Ok(result.Value.Select(n => new NetGrowthAdditionPointResponseDto(
                n.Timestamp,
                n.NetGrowthAddition)).ToList());
    }

    /// <summary>
    /// Histogram of order values with adaptive binning. Drilldown view only.
    /// </summary>
    [HttpGet("order-distribution")]
    [ProducesResponseType(typeof(IEnumerable<OrderBinResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<OrderBinResponseDto>>> GetOrderDistribution([FromQuery] OrderDistributionRequestDto request, CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(request.Timeframe, request.Comparison, ct);
        var result = await distributionService.GetOrderDistributionAsync(period, request.TenantId, request.BinCount, ct);
        if (result.IsFailed) return result.ToProblem(HttpContext);
        return Ok(result.Value.Select(b => new OrderBinResponseDto(
            b.BinLabel,
            b.MinValue,
            b.MaxValue,
            b.OrderCount,
            b.CumulativePercentage,
            b.KdeDensity)));
    }

    /// <summary>
    /// Heatmap data: transaction count and revenue grouped by day of week and hour.
    /// Scopes to a single tenant if tenantId is provided, otherwise portfolio-wide.
    /// </summary>
    [HttpGet("transaction-density")]
    [ProducesResponseType(typeof(TransactionDensityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TransactionDensityResponseDto>> GetTransactionDensity([FromQuery] TransactionDensityRequestDto request, CancellationToken ct = default)
    {
        var result = await distributionService.GetTransactionDensityAsync(request.Period, request.TenantId, request.TenantTypes, ct);
        if (result.IsFailed) return result.ToProblem(HttpContext);
        var density = result.Value;
        return Ok(new TransactionDensityResponseDto(
            density.TotalCount,
            density.MinCount,
            density.MaxCount,
            density.AverageCountPerBucket,
            density.SampleQuality,
            density.RequestedPeriod,
            density.EffectivePeriod,
            density.TimeZoneId,
            density.PeriodStart,
            density.PeriodEnd,
            density.Points.Select(p => new TransactionDensityPointResponseDto(
                p.DayOfWeek,
                p.Hour,
                p.Count,
                p.TotalRevenue)).ToList()));
    }

    /// <summary>
    /// Line-chart: running sum of absolute daily variance between current and previous periods.
    /// Scopes to a single tenant if tenantId is provided, otherwise portfolio-wide.
    /// </summary>
    [HttpGet("cumulative-growth-delta")]
    [ProducesResponseType(typeof(IEnumerable<CumulativeGrowthDeltaPointResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CumulativeGrowthDeltaPointResponseDto>>> GetCumulativeGrowthDelta([FromQuery] FinancialRequestDto request, CancellationToken ct = default)
    {
        var period = await reportingCalendar.ResolvePeriodAsync(request.Timeframe, request.Comparison, ct);
        var result = await seriesService.GetCumulativeGrowthDeltaAsync(period, request.TenantId, request.TenantTypes, ct);
        if (result.IsFailed) return result.ToProblem(HttpContext);
        return Ok(result.Value.Select(p => new CumulativeGrowthDeltaPointResponseDto(
                p.Timestamp,
                p.CurrentCumulative,
                p.PreviousCumulative,
                p.CumulativeGrowthDelta)).ToList());
    }
    
    /// <summary>
    /// List of orders
    /// NOTE: DO NOT USE FOR LARGE BATCH EXPORTS. MAX 100 RECORDS.
    /// FOR LARGE BATCH EXPORTS USE /orders/export
    /// </summary>
    [HttpGet("orders")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders([FromQuery] OrderRequestDto request, CancellationToken ct = default)
    {
        var result = await kpiService.GetOrdersAsync(request.DateSince, request.DateUntil, request.CeilingCount, ct);
        return Ok(result.Select(p => new OrderDto(
            AdwaisOrderId: p.AdwaisOrderId,
            OrderNumber: p.OrderNumber,
            AdwaisTenantId: p.AdwaisTenantId,
            OrderState: p.OrderState,
            CreatedDate: p.CreatedDate,
            TotalValueIncVat: p.TotalValueIncVat,
            TotalValueExcVat: p.TotalValueExcVat,
            Currency: p.Currency,
            TenantName: p.TenantName,
            Provider: p.Provider,
            ExternalId: p.ExternalId)).ToList());
    }
}



