// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Common.Models;
using Adwais.Application.DTOs.Financial;
using Adwais.Application.Interfaces;
using Adwais.Application.Services;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using FluentResults;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// KPI and order-list endpoints.
/// </summary>
public class FinancialKpiService(
    IApplicationDbContext dbContext,
    ICurrentAccess currentAccess,
    IFinancialSeriesReader seriesReader) : IFinancialKpiService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ICurrentAccess _currentAccess = currentAccess;
    private readonly IFinancialSeriesReader _seriesReader = seriesReader;

    /// <inheritdoc />
    public async Task<Result<KpiDto>> GetKpisAsync(ResolvedPeriod period, Guid? tenantId = null, IReadOnlyCollection<TenantType>? tenantTypes = null, CancellationToken ct = default)
    {
        var currentStart = period.CurrentStart;
        var currentEnd = period.CurrentEnd;
        var previousStart = period.PreviousStart;
        var isHourly = period.IsHourly;

        var visibleTenantIds = await FinancialScope.ResolveVisibleTenantIdsAsync(_currentAccess, _dbContext, ct);
        var validation = await FinancialScope.ValidateExplicitTenantAsync(tenantId, visibleTenantIds, _dbContext, ct);
        if (validation.IsFailed) return Result.Fail<KpiDto>(validation.Errors);

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

        return Result.Ok(new KpiDto(currentRevenue, previousRevenue, growthPct, volume, volumeGrowthPct, aov, aovGrowthPct, activeTenants, activeTenantsGrowthPct, arpt, arptGrowthPct));
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(DateTimeOffset dateSince, DateTimeOffset dateUntil, int ceilingCount, CancellationToken ct)
    {
        var visibleTenantIds = await FinancialScope.ResolveVisibleTenantIdsAsync(_currentAccess, _dbContext, ct);
        var filter = TenantSeriesFilter.Create(null, null, visibleTenantIds);

        var query = _dbContext.Orders
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
}
