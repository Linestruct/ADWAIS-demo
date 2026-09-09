// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Models;
using Adwais.Application.DTOs.Financial;
using Adwais.Domain.Enums;
using FluentResults;

namespace Adwais.Application.Interfaces;

public interface IFinancialDistributionService
{
    /// <summary>
    /// Analyzes cross-segment distribution stats (AOV, Volume, Revenue, Q1/Q2/Q3) grouped by business model cohort.
    /// </summary>
    Task<CrossSegmentDistributionDto> GetCrossSegmentDistributionAsync(ResolvedPeriod period, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);

    /// <summary>
    /// Analyzes revenue growth velocity and portfolio share for the Portfolio Impact Matrix across all tenants.
    /// </summary>
    Task<PortfolioImpactDto> GetPortfolioImpactAsync(ResolvedPeriod period, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);

    /// <summary>
    /// Generates a distribution histogram of order values for a specific tenant.
    /// </summary>
    Task<Result<IReadOnlyList<OrderBinDto>>> GetOrderDistributionAsync(ResolvedPeriod period, Guid tenantId, int? binCount = null, CancellationToken ct = default);

    /// <summary>
    /// Analyzes transaction density by day of week and hour of day.
    /// </summary>
    Task<Result<TransactionDensityDto>> GetTransactionDensityAsync(TransactionDensityPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);
}
