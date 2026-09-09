// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Models;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
using Adwais.Application.Interfaces;
using Adwais.Application.Services;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Adwais.Tests.Services;

public class FinancialServiceTests : IDisposable
{
    private readonly AnalyticsDbContext _dbContext;
    private readonly FinancialKpiService _kpiService;
    private readonly FinancialSeriesService _seriesService;
    private readonly FinancialDistributionService _distributionService;
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
        var reportingCalendarMock = new Mock<IReportingCalendar>();
        reportingCalendarMock.Setup(calendar => calendar.GetTimeZoneAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeZoneInfo.Utc);
        reportingCalendarMock.Setup(calendar => calendar.GetStartOfDayUtc(It.IsAny<DateTimeOffset>(), It.IsAny<TimeZoneInfo>()))
            .Returns((DateTimeOffset instant, TimeZoneInfo _) => new DateTimeOffset(instant.Date, TimeSpan.Zero));
        var reader = new FinancialSeriesReader(_dbContext, reportingCalendarMock.Object);
        _kpiService = new FinancialKpiService(_dbContext, _currentAccessMock.Object, reader);
        _seriesService = new FinancialSeriesService(_dbContext, _currentAccessMock.Object, reader);
        _distributionService = new FinancialDistributionService(
            _dbContext, reportingCalendarMock.Object, _currentAccessMock.Object, reader);

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
        var result = await _kpiService.GetKpisAsync(
            _period,
            tenantTypes: [TenantType.B2B],
            ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.CurrentRevenue);
        Assert.Equal(50m, result.Value.PreviousRevenue);
        Assert.Equal(100m, result.Value.RevenueGrowthPercentage);
        Assert.Equal(1, result.Value.TransactionVolume);
        Assert.Equal(1, result.Value.ActiveTenants);
        Assert.Equal(100m, result.Value.AverageOrderValue);
    }

    [Fact]
    public async Task PortfolioCharts_WithTenantTypes_RecalculateReferenceValuesAndSeries()
    {
        var accumulated = await _seriesService.GetAccumulatedRevenueAsync(
            _period,
            tenantTypes: [TenantType.B2C],
            ct: CancellationToken.None);
        var efficiency = await _seriesService.GetRevenueEfficiencyAsync(
            _period,
            [TenantType.B2B],
            CancellationToken.None);
        var portfolioImpact = await _distributionService.GetPortfolioImpactAsync(
            _period,
            [TenantType.B2B],
            CancellationToken.None);

        var distribution = await _distributionService.GetCrossSegmentDistributionAsync(
            _period,
            [TenantType.B2B],
            CancellationToken.None);

        Assert.True(accumulated.IsSuccess);
        Assert.Equal(300m, accumulated.Value[^1].CurrentAccumulated);
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

        var result = await _seriesService.GetNetGrowthAdditionAsync(_period);
        var b2bResult = await _seriesService.GetNetGrowthAdditionAsync(
            _period,
            tenantTypes: [TenantType.B2B]);

        Assert.True(result.IsSuccess);
        Assert.True(b2bResult.IsSuccess);
        Assert.Collection(
            result.Value,
            point => Assert.Equal(350m, point.NetGrowthAddition),
            point => Assert.Equal(-240m, point.NetGrowthAddition));
        Assert.Collection(
            b2bResult.Value,
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

        var result = await _seriesService.GetNetGrowthAdditionAsync(period, _b2bTenantId);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value.Count);
        Assert.Equal(75m, result.Value[0].NetGrowthAddition);
        Assert.Equal(40m, result.Value[1].NetGrowthAddition);
        Assert.Equal(start.AddHours(4), result.Value[1].Timestamp);
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

        var result = await _kpiService.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(400m, result.Value.CurrentRevenue);
        Assert.Equal(2, result.Value.TransactionVolume);
    }

    [Fact]
    public async Task GetKpisAsync_TenantScope_RestrictsToThatTenant()
    {
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, _b2bTenantId, [UserRole.TenantViewer]));

        var result = await _kpiService.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.CurrentRevenue);
        Assert.Equal(1, result.Value.TransactionVolume);
    }

    [Fact]
    public async Task GetKpisAsync_RequestedTenantOutsideScope_ReturnsScopeDenied()
    {
        var otherOrgId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = otherTenantId, OrganizationId = otherOrgId, Name = "Other Org Store", Type = TenantType.B2C });
        await _dbContext.SaveChangesAsync();

        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var result = await _kpiService.GetKpisAsync(
            _period, tenantId: otherTenantId, ct: CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task FinancialSeries_RequestedTenantOutsideScope_ReturnsScopeDenied()
    {
        var otherTenantId = await SeedOtherOrgTenantWithOrderAsync(500m);
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var accumulated = await _seriesService.GetAccumulatedRevenueAsync(_period, otherTenantId);
        var netGrowth = await _seriesService.GetNetGrowthAdditionAsync(_period, otherTenantId);
        var cumulative = await _seriesService.GetCumulativeGrowthDeltaAsync(_period, otherTenantId);

        Assert.IsType<ScopeDeniedError>(Assert.Single(accumulated.Errors));
        Assert.IsType<ScopeDeniedError>(Assert.Single(netGrowth.Errors));
        Assert.IsType<ScopeDeniedError>(Assert.Single(cumulative.Errors));
    }

    [Fact]
    public async Task GetNetGrowthAdditionAsync_PlatformDaily_SumsOrganizationRollupRowsOnce()
    {
        // Arrange
        var currentStart = DateTimeOffset.UtcNow.Date.AddDays(-2);
        var currentEnd = DateTimeOffset.UtcNow.Date;
        var dailyPeriod = new ResolvedPeriod(
            currentStart,
            currentEnd,
            currentStart.AddDays(-1),
            currentStart,
            2,
            isHourly: false,
            includeActualTime: false);
        var otherOrgId = Guid.NewGuid();
        _dbContext.DailyGlobalRollups.AddRange(
            new DailyFinancialGlobalRollup { CreatedDate = currentStart, OrganizationId = _defaultOrgId, GlobalRevenue = 100, GlobalVolume = 10 },
            new DailyFinancialGlobalRollup { CreatedDate = currentStart, OrganizationId = otherOrgId, GlobalRevenue = 50, GlobalVolume = 5 },
            new DailyFinancialGlobalRollup { CreatedDate = currentStart.AddDays(1), OrganizationId = _defaultOrgId, GlobalRevenue = 30, GlobalVolume = 3 });
        await _dbContext.SaveChangesAsync();

        // Act
        var points = await _seriesService.GetNetGrowthAdditionAsync(dailyPeriod, ct: CancellationToken.None);

        // Assert
        Assert.True(points.IsSuccess);
        Assert.Equal(2, points.Value.Count);
        Assert.Equal(150m, points.Value[0].NetGrowthAddition);
        Assert.Equal(-120m, points.Value[1].NetGrowthAddition);
    }

    [Fact]
    public async Task GetKpisAsync_NullScope_ReturnsNoRevenue()
    {
        _currentAccessMock.Setup(access => access.Scope)
            .Returns((AccessScope?)null);

        var result = await _kpiService.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.CurrentRevenue);
        Assert.Equal(0, result.Value.TransactionVolume);
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

        var orders = await _kpiService.GetOrdersAsync(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            100,
            CancellationToken.None);

        Assert.All(orders, order => Assert.NotEqual(otherTenantId, order.AdwaisTenantId));
        Assert.Equal(4, orders.Count);
    }

    private async Task<Guid> SeedOtherOrgTenantWithOrderAsync(decimal value)
    {
        var otherOrgId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant
        {
            Id = otherTenantId,
            OrganizationId = otherOrgId,
            Name = "Other Org Store",
            Type = TenantType.B2C
        });
        AddOrder(otherTenantId, _period.CurrentStart.AddMinutes(30), value, $"other-{value}");
        await _dbContext.SaveChangesAsync();
        return otherTenantId;
    }

    [Fact]
    public async Task GetAccumulatedRevenueAsync_OrgScope_ExcludesOtherOrgRevenue()
    {
        // Arrange: fixture own-org revenue is 400 across the current period.
        await SeedOtherOrgTenantWithOrderAsync(500m);
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        // Act
        var points = await _seriesService.GetAccumulatedRevenueAsync(_period, ct: CancellationToken.None);

        // Assert
        Assert.True(points.IsSuccess);
        Assert.NotEmpty(points.Value);
        Assert.Equal(400m, points.Value.Sum(point => point.CurrentRevenue));
    }

    [Fact]
    public async Task GetRevenueEfficiencyAsync_OrgScope_ExcludesOtherOrgTenants()
    {
        // Arrange
        var otherTenantId = await SeedOtherOrgTenantWithOrderAsync(500m);
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        // Act
        var result = await _seriesService.GetRevenueEfficiencyAsync(_period, ct: CancellationToken.None);

        // Assert
        Assert.All(result.Tenants, tenant => Assert.NotEqual(otherTenantId, tenant.TenantId));
        Assert.Equal(2, result.Tenants.Count);
        Assert.Equal(200m, result.GlobalAverageOrderValue);
    }

    [Fact]
    public async Task GetCrossSegmentDistributionAsync_OrgScope_ExcludesOtherOrgTenants()
    {
        // Arrange
        var otherTenantId = await SeedOtherOrgTenantWithOrderAsync(500m);
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        // Act
        var result = await _distributionService.GetCrossSegmentDistributionAsync(_period, ct: CancellationToken.None);

        // Assert
        Assert.All(result.Tenants, tenant => Assert.NotEqual(otherTenantId, tenant.TenantId));
    }

    [Fact]
    public async Task GetPortfolioImpactAsync_OrgScope_ExcludesOtherOrgTenants()
    {
        // Arrange
        var otherTenantId = await SeedOtherOrgTenantWithOrderAsync(500m);
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        // Act
        var result = await _distributionService.GetPortfolioImpactAsync(_period, ct: CancellationToken.None);

        // Assert
        Assert.All(result.Tenants, tenant => Assert.NotEqual(otherTenantId, tenant.TenantId));
    }

    [Fact]
    public async Task GetCumulativeGrowthDeltaAsync_OrgScope_ExcludesOtherOrgRevenue()
    {
        // Arrange
        await SeedOtherOrgTenantWithOrderAsync(500m);
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        // Act
        var points = await _seriesService.GetCumulativeGrowthDeltaAsync(_period, ct: CancellationToken.None);

        // Assert
        Assert.True(points.IsSuccess);
        Assert.NotEmpty(points.Value);
        Assert.Equal(400m, points.Value[^1].CurrentCumulative);
    }

    [Fact]
    public async Task GetOrderDistributionAsync_CrossOrgTenant_ReturnsScopeDenied()
    {
        // Arrange
        var otherTenantId = await SeedOtherOrgTenantWithOrderAsync(500m);
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var result = await _distributionService.GetOrderDistributionAsync(_period, otherTenantId, ct: CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task GetTransactionDensityAsync_CrossOrgTenant_ReturnsScopeDenied()
    {
        // Arrange
        var otherTenantId = await SeedOtherOrgTenantWithOrderAsync(500m);
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var result = await _distributionService.GetTransactionDensityAsync(TransactionDensityPeriod.Auto, tenantId: otherTenantId, ct: CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
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
