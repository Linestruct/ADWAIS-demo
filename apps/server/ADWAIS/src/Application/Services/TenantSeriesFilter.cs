// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Entities;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;

namespace Adwais.Application.Services;

/// <summary>
/// The tenant scoping for one financial query, built once per call. It
/// carries the explicit tenant, the normalized tenant-type set, and the
/// visible tenant ids for the current scope, and exposes ready-made
/// predicates so consumers stop rephrasing the same checks.
/// </summary>
public sealed record TenantSeriesFilter(
    Guid? TenantId,
    TenantType[] TenantTypes,
    Guid[]? VisibleTenantIds)
{
    public static TenantSeriesFilter Create(
        Guid? tenantId,
        IReadOnlyCollection<TenantType>? tenantTypes,
        Guid[]? visibleTenantIds) =>
        new(
            tenantId,
            !tenantId.HasValue && tenantTypes is { Count: > 0 }
                ? tenantTypes.Distinct().ToArray()
                : [],
            visibleTenantIds);

    public bool HasTenantFilter => TenantId.HasValue;

    public bool HasTypeFilter => TenantTypes.Length > 0;

    public bool IsRestricted => VisibleTenantIds is not null;

    /// <summary>
    /// Rejects an explicit tenant outside the current scope.
    /// </summary>
    public void ThrowIfTenantOutsideScope()
    {
        if (TenantId.HasValue && VisibleTenantIds is not null && !VisibleTenantIds.Contains(TenantId.Value))
            throw new UnauthorizedAccessException($"Tenant {TenantId.Value} is outside the current scope.");
    }

    public IQueryable<Order> ApplyToOrders(IQueryable<Order> query) =>
        VisibleTenantIds is not null
            ? query.Where(o => VisibleTenantIds.Contains(o.TenantId))
            : query;

    public IQueryable<DailyFinancialTenantRollup> ApplyToTenantRollups(IQueryable<DailyFinancialTenantRollup> query) =>
        VisibleTenantIds is not null
            ? query.Where(r => VisibleTenantIds.Contains(r.TenantId))
            : query;

    public IQueryable<Tenant> ApplyToTenants(IQueryable<Tenant> query) =>
        VisibleTenantIds is not null
            ? query.Where(t => VisibleTenantIds.Contains(t.Id))
            : query;
}
