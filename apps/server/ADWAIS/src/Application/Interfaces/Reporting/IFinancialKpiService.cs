// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Models;
using Adwais.Application.DTOs.Financial;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;
using FluentResults;

namespace Adwais.Application.Interfaces;

public interface IFinancialKpiService
{
    /// <summary>
    /// Calculates key performance indicators (KPIs) for the specified timeframe and tenant.
    /// </summary>
    Task<Result<KpiDto>> GetKpisAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default);

    Task<IReadOnlyList<OrderDto>> GetOrdersAsync(DateTimeOffset dateSince, DateTimeOffset dateUntil, int ceilingCount, CancellationToken ct);
}
