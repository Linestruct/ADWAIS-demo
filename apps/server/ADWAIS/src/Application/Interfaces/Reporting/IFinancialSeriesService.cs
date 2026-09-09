// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Models;
using Adwais.Application.DTOs.Financial;
using Adwais.Domain.Enums;
using FluentResults;

namespace Adwais.Application.Interfaces;

public interface IFinancialSeriesService
{
    /// <summary>
    /// Retrieves running accumulated revenue for current and previous periods.
    /// </summary>
    Task<Result<IReadOnlyList<AccumulatedRevenuePointDto>>> GetAccumulatedRevenueAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);

    /// <summary>
    /// Analyzes revenue efficiency across all tenants, returning AOV, portfolio share, and growth velocity.
    /// </summary>
    Task<RevenueEfficiencyDto> GetRevenueEfficiencyAsync(ResolvedPeriod period, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);

    /// <summary>
    /// Calculates the change in revenue from one time bucket to the next.
    /// Scopes to a tenant when provided, otherwise returns portfolio-wide values.
    /// </summary>
    Task<Result<IReadOnlyList<NetGrowthAdditionPointDto>>> GetNetGrowthAdditionAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);

    /// <summary>
    /// Calculates the cumulative growth delta for the specified timeframe.
    /// </summary>
    Task<Result<IReadOnlyList<CumulativeGrowthDeltaPointDto>>> GetCumulativeGrowthDeltaAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);
}
