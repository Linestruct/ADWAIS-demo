// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Models;
using Adwais.Application.Common.Access;
using Adwais.Application.Interfaces;
using Adwais.Application.Services;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Adwais.Tests.Services;

public class FinancialServiceTests : IDisposable
{
    private readonly AnalyticsDbContext _dbContext;
    private readonly FinancialService _service;
    private readonly Mock<ICurrentAccess> _currentAccessMock;
    private readonly ResolvedPeriod _period;
    private readonly Guid _b2bTenantId = Guid.NewGuid();
    private readonly Guid _b2cTenantId = Guid.NewGuid();
    private readonly Guid _defaultOrgId = Guid.NewGuid();

    public FinancialServiceTests()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AnalyticsDbContext(options);
        _currentAccessMock = new Mock<ICurrentAccess>();
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(null, null, [UserRole.Admin]));
        _service = new FinancialService(_dbContext, Mock.Of<IReportingCalendar>(), _currentAccessMock.Object);

        var currentStart = DateTimeOffset.UtcNow.AddHours(-2);
        _period = new ResolvedPeriod(
            currentStart,
            currentStart.AddHours(2),
            currentStart.AddHours(-2),
            currentStart,
            2,
            isHourly: true,
            includeActualTime: true);

        _dbContext.Tenants.AddRange(
            new Tenant { Id = _b2bTenantId, OrganizationId = _defaultOrgId, Name = "Wholesale", Type = TenantType.B2B },
            new Tenant { Id = _b2cTenantId, OrganizationId = _defaultOrgId, Name = "Retail", Type = TenantType.B2C });
        AddOrder(_b2bTenantId, _period.CurrentStart.AddMinutes(30), 100m, "b2b-current");
        AddOrder(_b2bTenantId, _period.PreviousStart.AddMinutes(30), 50m, "b2b-previous");
        AddOrder(_b2cTenantId, _period.CurrentStart.AddMinutes(30), 300m, "b2c-current");
        AddOrder(_b2cTenantId, _period.PreviousStart.AddMinutes(30), 100m, "b2c-previous");
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetKpisAsync_WithTenantTypes_RecalculatesPortfolioFromMatchingTenants()
    {
        var result = await _service.GetKpisAsync(
            _period,
            tenantTypes: [TenantType.B2B],
            ct: CancellationToken.None);

        Assert.Equal(100m, result.CurrentRevenue);
        Assert.Equal(50m, result.PreviousRevenue);
        Assert.Equal(100m, result.RevenueGrowthPercentage);
        Assert.Equal(1, result.TransactionVolume);
        Assert.Equal(1, result.ActiveTenants);
        Assert.Equal(100m, result.AverageOrderValue);
    }

    [Fact]
    public async Task PortfolioCharts_WithTenantTypes_RecalculateReferenceValuesAndSeries()
    {
        var accumulated = await _service.GetAccumulatedRevenueAsync(
            _period,
            tenantTypes: [TenantType.B2C],
            ct: CancellationToken.None);
        var efficiency = await _service.GetRevenueEfficiencyAsync(
            _period,
            [TenantType.B2B],
            CancellationToken.None);
        var portfolioImpact = await _service.GetPortfolioImpactAsync(
            _period,
            [TenantType.B2B],
            CancellationToken.None);

        var distribution = await _service.GetCrossSegmentDistributionAsync(
            _period,
            [TenantType.B2B],
            CancellationToken.None);

        Assert.Equal(300m, accumulated[^1].CurrentAccumulated);
        Assert.Single(efficiency.Tenants);
        Assert.Equal(_b2bTenantId, efficiency.Tenants[0].TenantId);
        Assert.Equal(100m, efficiency.GlobalAverageOrderValue);
        Assert.Equal(100m, efficiency.Tenants[0].PortfolioSharePercentage);
        Assert.Single(portfolioImpact.Tenants);
        Assert.Equal(100m, portfolioImpact.GlobalGrowthPercentage);
        Assert.Equal(50m, portfolioImpact.MedianBaselineRevenue);
        Assert.Equal(100m, portfolioImpact.Tenants[0].PortfolioSharePercentage);
        Assert.Equal(100m, portfolioImpact.MedianPortfolioShare);

        Assert.Single(distribution.Cohorts);
        Assert.Equal(TenantType.B2B, distribution.Cohorts[0].Type);
        Assert.Equal(100m, distribution.Cohorts[0].MedianAov);
        Assert.Single(distribution.Tenants);
        Assert.Equal(_b2bTenantId, distribution.Tenants[0].TenantId);
        Assert.Equal(100m, distribution.Tenants[0].PeriodRevenue);
    }

    [Fact]
    public async Task GetNetGrowthAdditionAsync_WithoutTenant_ReturnsPortfolioSeries()
    {
        AddOrder(_b2bTenantId, _period.CurrentStart.AddHours(-0.5), 40m, "b2b-lookback");
        AddOrder(_b2cTenantId, _period.CurrentStart.AddHours(-0.25), 10m, "b2c-lookback");
        AddOrder(_b2bTenantId, _period.CurrentStart.AddHours(1.5), 160m, "b2b-current-2");
        _dbContext.SaveChanges();

        var result = await _service.GetNetGrowthAdditionAsync(_period);
        var b2bResult = await _service.GetNetGrowthAdditionAsync(
            _period,
            tenantTypes: [TenantType.B2B]);

        Assert.Collection(
            result,
            point => Assert.Equal(350m, point.NetGrowthAddition),
            point => Assert.Equal(-240m, point.NetGrowthAddition));
        Assert.Collection(
            b2bResult,
            point => Assert.Equal(60m, point.NetGrowthAddition),
            point => Assert.Equal(60m, point.NetGrowthAddition));
    }

    [Fact]
    public async Task GetNetGrowthAdditionAsync_UsesResolvedHourlyBinWidth()
    {
        var start = DateTimeOffset.UtcNow.AddDays(-7);
        var period = new ResolvedPeriod(
            start,
            start.AddDays(7),
            start.AddDays(-7),
            start,
            42,
            isHourly: true,
            includeActualTime: false);

        AddOrder(_b2bTenantId, start.AddHours(-2), 25m, "lookback-bin");
        AddOrder(_b2bTenantId, start.AddHours(1), 100m, "first-bin");
        AddOrder(_b2bTenantId, start.AddHours(5), 140m, "second-bin");
        _dbContext.SaveChanges();

        var result = await _service.GetNetGrowthAdditionAsync(period, _b2bTenantId);

        Assert.Equal(42, result.Count);
        Assert.Equal(75m, result[0].NetGrowthAddition);
        Assert.Equal(40m, result[1].NetGrowthAddition);
        Assert.Equal(start.AddHours(4), result[1].Timestamp);
    }

    [Fact]
    public async Task GetKpisAsync_OrgScope_OnlyIncludesOrgTenants()
    {
        var otherOrgId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = otherTenantId, OrganizationId = otherOrgId, Name = "Other Org Store", Type = TenantType.B2C });
        AddOrder(otherTenantId, _period.CurrentStart.AddMinutes(30), 500m, "other-org-order");
        await _dbContext.SaveChangesAsync();

        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var result = await _service.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.Equal(400m, result.CurrentRevenue);
        Assert.Equal(2, result.TransactionVolume);
    }

    [Fact]
    public async Task GetKpisAsync_TenantScope_RestrictsToThatTenant()
    {
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, _b2bTenantId, [UserRole.TenantViewer]));

        var result = await _service.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.Equal(100m, result.CurrentRevenue);
        Assert.Equal(1, result.TransactionVolume);
    }

    [Fact]
    public async Task GetKpisAsync_RequestedTenantOutsideScope_ThrowsKeyNotFound()
    {
        var otherOrgId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = otherTenantId, OrganizationId = otherOrgId, Name = "Other Org Store", Type = TenantType.B2C });
        await _dbContext.SaveChangesAsync();

        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetKpisAsync(_period, tenantId: otherTenantId, ct: CancellationToken.None));
    }

    [Fact]
    public async Task GetKpisAsync_NullScope_ReturnsNoRevenue()
    {
        _currentAccessMock.Setup(access => access.Scope)
            .Returns((AccessScope?)null);

        var result = await _service.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.Equal(0m, result.CurrentRevenue);
        Assert.Equal(0, result.TransactionVolume);
    }

    [Fact]
    public async Task GetOrdersAsync_OrgScope_FiltersToOrgTenants()
    {
        var otherOrgId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = otherTenantId, OrganizationId = otherOrgId, Name = "Other Org Store", Type = TenantType.B2C });
        AddOrder(otherTenantId, DateTimeOffset.UtcNow.AddMinutes(-5), 500m, "other-org-order");
        await _dbContext.SaveChangesAsync();

        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var orders = await _service.GetOrdersAsync(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            100,
            CancellationToken.None);

        Assert.All(orders, order => Assert.NotEqual(otherTenantId, order.AdwaisTenantId));
        Assert.Equal(4, orders.Count);
    }

    private void AddOrder(Guid tenantId, DateTimeOffset createdDate, decimal value, string litiumOrderId)
    {
        _dbContext.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNumber = litiumOrderId,
            CreatedDate = createdDate,
            TotalValueIncVat = value,
            TotalValueExcVat = value,
            OrderState = OrderState.Completed,
        });
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
