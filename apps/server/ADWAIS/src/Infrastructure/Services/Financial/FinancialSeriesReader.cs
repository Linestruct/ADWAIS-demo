// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Application.Services;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Merges live order rows onto historical rollups, hourly or daily, on the
/// tenant or the global path.
/// </summary>
public sealed class FinancialSeriesReader(
    IApplicationDbContext dbContext,
    IReportingCalendar reportingCalendar) : IFinancialSeriesReader
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IReportingCalendar _reportingCalendar = reportingCalendar;

    public async Task<IReadOnlyList<FinancialSeriesRow>> ReadTenantSeriesAsync(
        DateTimeOffset start, DateTimeOffset end, bool isHourly,
        TenantSeriesFilter filter, CancellationToken ct = default)
    {
        filter.ThrowIfTenantOutsideScope();

        if (filter.TenantId.HasValue)
        {
            var tenantExists = await _dbContext.Tenants.AnyAsync(t => t.Id == filter.TenantId.Value, ct);
            if (!tenantExists) throw new KeyNotFoundException($"Tenant {filter.TenantId.Value} not found.");
        }

        if (isHourly)
        {
            var rows = await filter.ApplyToOrders(ApplyOrderScope(OrdersInRange(start, end), filter))
                .Select(o => new { o.CreatedDate, o.TenantId, o.TotalValueExcVat })
                .ToListAsync(ct);

            return rows.Select(x => new FinancialSeriesRow(
                x.CreatedDate,
                x.TenantId,
                x.TotalValueExcVat,
                1
            )).ToList();
        }

        var (currentDayStart, viewEnd) = await CurrentDayBoundsAsync(end, ct);

        var rawHist = await filter.ApplyToTenantRollups(ApplyRollupScope(
                _dbContext.DailyTenantRollups
                    .AsNoTracking()
                    .Where(r => r.CreatedDate >= start && r.CreatedDate < viewEnd),
                filter))
            .Select(r => new { r.CreatedDate, r.TenantId, r.Revenue, r.Volume })
            .ToListAsync(ct);

        var historical = rawHist.Select(r => new FinancialSeriesRow(r.CreatedDate, r.TenantId, r.Revenue, (int)r.Volume)).ToList();

        if (currentDayStart < end)
        {
            var freshRows = await filter.ApplyToOrders(ApplyOrderScope(OrdersInRange(currentDayStart, end), filter))
                .GroupBy(o => new { o.CreatedDate.Year, o.CreatedDate.Month, o.CreatedDate.Day, o.TenantId })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.Day,
                    g.Key.TenantId,
                    Revenue = g.Sum(o => o.TotalValueExcVat),
                    Volume = g.Count()
                })
                .ToListAsync(ct);

            historical.AddRange(freshRows.Select(x => new FinancialSeriesRow(
                new DateTimeOffset(x.Year, x.Month, x.Day, 0, 0, 0, TimeSpan.Zero),
                x.TenantId,
                x.Revenue,
                x.Volume
            )));
        }

        return historical;
    }

    public async Task<IReadOnlyList<FinancialSeriesRow>> ReadGlobalSeriesAsync(
        DateTimeOffset start, DateTimeOffset end, bool isHourly,
        Guid[]? visibleTenantIds = null, CancellationToken ct = default)
    {
        if (visibleTenantIds is not null)
        {
            // Scoped principals cannot read the deployment-wide rollup. Aggregate
            // the visible tenants from the tenant-level path instead.
            var scopedRows = await ReadTenantSeriesAsync(
                start, end, isHourly,
                TenantSeriesFilter.Create(null, null, visibleTenantIds), ct);
            return scopedRows
                .GroupBy(row => row.Timestamp)
                .Select(group => new FinancialSeriesRow(group.Key, null, group.Sum(row => row.Revenue), group.Sum(row => row.Volume)))
                .OrderBy(row => row.Timestamp)
                .ToList();
        }

        if (isHourly)
        {
            var rows = await OrdersInRange(start, end)
                .Where(o => o.Tenant == null || !o.Tenant.IsSystem)
                .Select(o => new { o.CreatedDate, o.TotalValueExcVat })
                .ToListAsync(ct);

            return rows.Select(x => new FinancialSeriesRow(
                x.CreatedDate,
                null,
                x.TotalValueExcVat,
                1
            )).ToList();
        }

        var (currentDayStart, viewEnd) = await CurrentDayBoundsAsync(end, ct);

        var rawHist = await _dbContext.DailyGlobalRollups
            .AsNoTracking()
            .Where(r => r.CreatedDate >= start && r.CreatedDate < viewEnd)
            .GroupBy(r => r.CreatedDate)
            .Select(g => new { CreatedDate = g.Key, GlobalRevenue = g.Sum(x => x.GlobalRevenue), GlobalVolume = g.Sum(x => x.GlobalVolume) })
            .ToListAsync(ct);

        var historical = rawHist.Select(r => new FinancialSeriesRow(r.CreatedDate, null, r.GlobalRevenue, (int)r.GlobalVolume)).ToList();

        if (currentDayStart < end)
        {
            var freshRows = await OrdersInRange(currentDayStart, end)
                .Where(o => o.Tenant == null || !o.Tenant.IsSystem)
                .GroupBy(o => new { o.CreatedDate.Year, o.CreatedDate.Month, o.CreatedDate.Day })
                .Select(g => new {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.Day,
                    Revenue = g.Sum(o => o.TotalValueExcVat),
                    Volume = g.Count()
                })
                .ToListAsync(ct);

            historical.AddRange(freshRows.Select(x => new FinancialSeriesRow(
                new DateTimeOffset(x.Year, x.Month, x.Day, 0, 0, 0, TimeSpan.Zero),
                null,
                x.Revenue,
                x.Volume
            )));
        }

        return historical;
    }

    private IQueryable<Order> OrdersInRange(DateTimeOffset start, DateTimeOffset end) =>
        _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderState != OrderState.Cancelled)
            .Where(o => o.CreatedDate >= start && o.CreatedDate < end);

    private static IQueryable<Order> ApplyOrderScope(IQueryable<Order> query, TenantSeriesFilter filter)
    {
        if (filter.TenantId.HasValue)
            return query.Where(o => o.TenantId == filter.TenantId.Value);

        // System tenants (unassigned buckets, legacy rows) drop out by flag.
        // Orders without a tenant lookup stay visible, as before.
        query = query.Where(o => o.Tenant == null || !o.Tenant.IsSystem);
        if (filter.HasTypeFilter)
            query = query.Where(o => o.Tenant != null && filter.TenantTypes.Contains(o.Tenant.Type));
        return query;
    }

    private IQueryable<DailyFinancialTenantRollup> ApplyRollupScope(
        IQueryable<DailyFinancialTenantRollup> query, TenantSeriesFilter filter)
    {
        if (filter.TenantId.HasValue)
            return query.Where(r => r.TenantId == filter.TenantId.Value);

        query = query.Where(r => !_dbContext.Tenants.Any(t => t.Id == r.TenantId && t.IsSystem));
        if (filter.HasTypeFilter)
        {
            query = query.Where(r => _dbContext.Tenants
                .Where(t => filter.TenantTypes.Contains(t.Type))
                .Select(t => t.Id)
                .Contains(r.TenantId));
        }
        return query;
    }

    private async Task<(DateTimeOffset CurrentDayStart, DateTimeOffset ViewEnd)> CurrentDayBoundsAsync(
        DateTimeOffset end, CancellationToken ct)
    {
        var timeZone = await _reportingCalendar.GetTimeZoneAsync(ct);
        var currentDayStart = _reportingCalendar.GetStartOfDayUtc(DateTimeOffset.UtcNow, timeZone);
        return (currentDayStart, currentDayStart < end ? currentDayStart : end);
    }
}
