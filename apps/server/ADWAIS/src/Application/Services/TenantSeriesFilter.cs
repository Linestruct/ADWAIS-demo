// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Entities;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;

namespace Adwais.Application.Services;

/// <summary>
/// Tenant scoping for one financial query: explicit tenant, normalized
/// type set, and visible tenant ids, with predicates that apply them.
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

    public bool IsExplicitTenantOutsideScope =>
        TenantId is { } tenantId && VisibleTenantIds is not null && !VisibleTenantIds.Contains(tenantId);

    /// <summary>
    /// Rejects an explicit tenant outside the current scope.
    /// </summary>
    public void ThrowIfTenantOutsideScope()
    {
        if (TenantId is { } tenantId && VisibleTenantIds is not null && !VisibleTenantIds.Contains(tenantId))
            throw new UnauthorizedAccessException($"Tenant {tenantId} is outside the current scope.");
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
