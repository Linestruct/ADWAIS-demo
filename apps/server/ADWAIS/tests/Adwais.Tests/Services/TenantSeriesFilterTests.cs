// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Services;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;

namespace Adwais.Tests.Services;

public class TenantSeriesFilterTests
{
    [Fact]
    public void Create_NormalizesTypeSetOnlyWithoutExplicitTenant()
    {
        var withTenant = TenantSeriesFilter.Create(Guid.NewGuid(), [TenantType.B2B, TenantType.B2B], null);
        var withoutTenant = TenantSeriesFilter.Create(null, [TenantType.B2B, TenantType.B2B], null);
        var empty = TenantSeriesFilter.Create(null, [], null);
        var missing = TenantSeriesFilter.Create(null, null, null);

        Assert.Empty(withTenant.TenantTypes);
        Assert.False(withTenant.HasTypeFilter);
        Assert.Equal([TenantType.B2B], withoutTenant.TenantTypes);
        Assert.True(withoutTenant.HasTypeFilter);
        Assert.Empty(empty.TenantTypes);
        Assert.Empty(missing.TenantTypes);
    }

    [Fact]
    public void ThrowIfTenantOutsideScope_RejectsOnlyForeignExplicitTenants()
    {
        var foreign = Guid.NewGuid();
        var own = Guid.NewGuid();

        Assert.Throws<UnauthorizedAccessException>(() =>
            TenantSeriesFilter.Create(foreign, null, [own]).ThrowIfTenantOutsideScope());
        TenantSeriesFilter.Create(own, null, [own]).ThrowIfTenantOutsideScope();
        TenantSeriesFilter.Create(foreign, null, null).ThrowIfTenantOutsideScope();
        TenantSeriesFilter.Create(null, null, [own]).ThrowIfTenantOutsideScope();
    }

    [Fact]
    public void ApplyToOrders_FiltersByVisibleTenantsOnlyWhenRestricted()
    {
        var own = Guid.NewGuid();
        var orders = new List<Order>
        {
            new() { Id = Guid.NewGuid(), TenantId = own, OrderNumber = "1" },
            new() { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), OrderNumber = "2" },
        }.AsQueryable();

        Assert.Equal(2, TenantSeriesFilter.Create(null, null, null).ApplyToOrders(orders).Count());
        Assert.Single(TenantSeriesFilter.Create(null, null, [own]).ApplyToOrders(orders));
    }

    [Fact]
    public void ApplyToTenantRollups_FiltersByVisibleTenantsOnlyWhenRestricted()
    {
        var own = Guid.NewGuid();
        var rows = new List<DailyFinancialTenantRollup>
        {
            new() { TenantId = own },
            new() { TenantId = Guid.NewGuid() },
        }.AsQueryable();

        Assert.Equal(2, TenantSeriesFilter.Create(null, null, null).ApplyToTenantRollups(rows).Count());
        Assert.Single(TenantSeriesFilter.Create(null, null, [own]).ApplyToTenantRollups(rows));
    }

    [Fact]
    public void ApplyToTenants_FiltersByVisibleTenantsOnlyWhenRestricted()
    {
        var own = Guid.NewGuid();
        var tenants = new List<Tenant>
        {
            new() { Id = own, Name = "Own" },
            new() { Id = Guid.NewGuid(), Name = "Other" },
        }.AsQueryable();

        Assert.Equal(2, TenantSeriesFilter.Create(null, null, null).ApplyToTenants(tenants).Count());
        Assert.Single(TenantSeriesFilter.Create(null, null, [own]).ApplyToTenants(tenants));
    }
}
