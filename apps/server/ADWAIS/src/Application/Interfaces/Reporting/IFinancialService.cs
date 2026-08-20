// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Models;
using Adwais.Application.DTOs.Financial;
using Adwais.Domain.Enums;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.OrderData;

namespace Adwais.Application.Interfaces;

public interface IFinancialService
{
    /// <summary>
    /// Calculates key performance indicators (KPIs) for the specified timeframe and tenant.
    /// </summary>
    Task<KpiDto> GetKpisAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);
    
    /// <summary>
    /// Retrieves running accumulated revenue for current and previous periods.
    /// </summary>
    Task<IReadOnlyList<AccumulatedRevenuePointDto>> GetAccumulatedRevenueAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);
    /// <summary>
    /// Analyzes revenue efficiency across all tenants, returning AOV, portfolio share, and growth velocity.
    /// </summary>
    Task<RevenueEfficiencyDto> GetRevenueEfficiencyAsync(ResolvedPeriod period, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);

    /// <summary>
    /// Analyzes cross-segment distribution stats (AOV, Volume, Revenue, Q1/Q2/Q3) grouped by business model cohort.
    /// </summary>
    Task<CrossSegmentDistributionDto> GetCrossSegmentDistributionAsync(ResolvedPeriod period, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);
    /// <summary>
    /// Analyzes revenue growth velocity and portfolio share for the Portfolio Impact Matrix across all tenants.
    /// </summary>
    Task<PortfolioImpactDto> GetPortfolioImpactAsync(ResolvedPeriod period, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);
    
    /// <summary>
    /// Calculates the change in revenue from one time bucket to the next.
    /// Scopes to a tenant when provided, otherwise returns portfolio-wide values.
    /// </summary>
    Task<IReadOnlyList<NetGrowthAdditionPointDto>> GetNetGrowthAdditionAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);

    /// <summary>
    /// Generates a distribution histogram of order values for a specific tenant.
    /// </summary>
    Task<IReadOnlyList<OrderBinDto>> GetOrderDistributionAsync(ResolvedPeriod period, Guid tenantId, int? binCount = null, CancellationToken ct = default);

    /// <summary>
    /// Analyzes transaction density by day of week and hour of day.
    /// </summary>
    Task<TransactionDensityDto> GetTransactionDensityAsync(TransactionDensityPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);

    /// <summary>
    /// Calculates the cumulative growth delta for the specified timeframe.
    /// </summary>
    Task<IReadOnlyList<CumulativeGrowthDeltaPointDto>> GetCumulativeGrowthDeltaAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);

    Task<IReadOnlyList<OrderDto>> GetOrdersAsync(DateTimeOffset dateSince, DateTimeOffset dateUntil, int ceilingCount, CancellationToken ct);
}
