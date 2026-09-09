// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
using Adwais.Application.Common.Models;
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

/// <summary>
/// Characterization tests for the hourly financial endpoints, pinned
/// against a known fixture: mixed tenant types, a cancelled order, a
/// second organization, and scoped, denied, and platform callers.
/// </summary>
public class FinancialHourlyCharacterizationTests : IDisposable
{
    private readonly AnalyticsDbContext _dbContext;
    private readonly FinancialKpiService _kpiService;
    private readonly FinancialSeriesService _seriesService;
    private readonly FinancialDistributionService _distributionService;
    private readonly Mock<ICurrentAccess> _currentAccessMock;
    private readonly ResolvedPeriod _period;
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();
    private readonly Guid _b2bTenantId = Guid.NewGuid();
    private readonly Guid _b2cTenantId = Guid.NewGuid();
    private readonly Guid _mixedTenantId = Guid.NewGuid();
    private readonly Guid _otherTenantId = Guid.NewGuid();

    public FinancialHourlyCharacterizationTests()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AnalyticsDbContext(options);
        _currentAccessMock = new Mock<ICurrentAccess>();
        UsePlatformCaller();
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
            new Tenant { Id = _b2bTenantId, OrganizationId = _orgA, Name = "Wholesale", Type = TenantType.B2B },
            new Tenant { Id = _b2cTenantId, OrganizationId = _orgA, Name = "Retail", Type = TenantType.B2C },
            new Tenant { Id = _mixedTenantId, OrganizationId = _orgA, Name = "Marketplace", Type = TenantType.Mixed },
            new Tenant { Id = _otherTenantId, OrganizationId = _orgB, Name = "Other", Type = TenantType.B2B });
        AddOrder(_b2bTenantId, _period.CurrentStart.AddMinutes(30), 100m, "h-b2b-cur");
        AddOrder(_b2bTenantId, _period.PreviousStart.AddMinutes(30), 50m, "h-b2b-prev");
        AddCancelledOrder(_b2bTenantId, _period.CurrentStart.AddMinutes(60), 999m, "h-b2b-cancelled");
        AddOrder(_b2cTenantId, _period.CurrentStart.AddMinutes(30), 300m, "h-b2c-cur");
        AddOrder(_b2cTenantId, _period.PreviousStart.AddMinutes(30), 100m, "h-b2c-prev");
        AddOrder(_mixedTenantId, _period.CurrentStart.AddMinutes(90), 200m, "h-mixed-cur");
        AddOrder(_otherTenantId, _period.CurrentStart.AddMinutes(30), 1000m, "h-other-cur");
        AddOrder(_otherTenantId, _period.PreviousStart.AddMinutes(30), 500m, "h-other-prev");
        _dbContext.SaveChanges();
    }

    private void UsePlatformCaller() =>
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(null, null, [UserRole.Admin]));

    private void UseOrgACaller() =>
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_orgA, null, [UserRole.Admin]));

    private void UseDeniedCaller() =>
        _currentAccessMock.Setup(access => access.Scope)
            .Returns((AccessScope?)null);

    [Fact]
    public async Task GetKpisAsync_PlatformCaller_SeesBothOrganizationsWithoutCancelledOrders()
    {
        var result = await _kpiService.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1600m, result.Value.CurrentRevenue);
        Assert.Equal(650m, result.Value.PreviousRevenue);
        Assert.Equal(146.15m, result.Value.RevenueGrowthPercentage);
        Assert.Equal(4, result.Value.TransactionVolume);
        Assert.Equal(400m, result.Value.AverageOrderValue);
        Assert.Equal(4, result.Value.ActiveTenants);
    }

    [Fact]
    public async Task GetKpisAsync_OrgCaller_SeesOwnOrganizationOnly()
    {
        UseOrgACaller();

        var result = await _kpiService.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(600m, result.Value.CurrentRevenue);
        Assert.Equal(150m, result.Value.PreviousRevenue);
        Assert.Equal(300m, result.Value.RevenueGrowthPercentage);
        Assert.Equal(3, result.Value.TransactionVolume);
        Assert.Equal(200m, result.Value.AverageOrderValue);
        Assert.Equal(3, result.Value.ActiveTenants);
    }

    [Fact]
    public async Task GetKpisAsync_DeniedCaller_SeesZeros()
    {
        UseDeniedCaller();

        var result = await _kpiService.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value.CurrentRevenue);
        Assert.Equal(0, result.Value.TransactionVolume);
    }

    [Fact]
    public async Task GetOrdersAsync_PlatformCaller_ExcludesCancelledAndOrdersByDate()
    {
        var result = await _kpiService.GetOrdersAsync(
            _period.PreviousStart, _period.CurrentEnd, 100, CancellationToken.None);

        Assert.Equal(7, result.Count);
        Assert.DoesNotContain(result, o => o.OrderState == OrderState.Cancelled);
        Assert.True(result.Select(o => o.CreatedDate).SequenceEqual(result.Select(o => o.CreatedDate).OrderByDescending(d => d)));
    }

    [Fact]
    public async Task GetOrdersAsync_OrgCallerAndCeiling_ApplyScopeAndLimit()
    {
        UseOrgACaller();

        var scoped = await _kpiService.GetOrdersAsync(
            _period.PreviousStart, _period.CurrentEnd, 100, CancellationToken.None);
        var capped = await _kpiService.GetOrdersAsync(
            _period.PreviousStart, _period.CurrentEnd, 2, CancellationToken.None);

        Assert.Equal(5, scoped.Count);
        Assert.Equal(2, capped.Count);
    }

    [Fact]
    public async Task GetAccumulatedRevenueAsync_PlatformCaller_BinsByHourWithTypeSplits()
    {
        var result = await _seriesService.GetAccumulatedRevenueAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(1400m, result.Value[0].CurrentRevenue);
        Assert.Equal(650m, result.Value[0].PreviousRevenue);
        Assert.Equal(1400m, result.Value[0].CurrentAccumulated);
        Assert.Equal(1100m, result.Value[0].CurrentRevenueB2B);
        Assert.Equal(300m, result.Value[0].CurrentRevenueB2C);
        Assert.Equal(0m, result.Value[0].CurrentRevenueMixed);
        Assert.Equal(200m, result.Value[1].CurrentRevenue);
        Assert.Equal(200m, result.Value[1].CurrentRevenueMixed);
        Assert.Equal(1600m, result.Value[1].CurrentAccumulated);
        Assert.Equal(650m, result.Value[1].PreviousAccumulated);
    }

    [Fact]
    public async Task GetNetGrowthAdditionAsync_PlatformCaller_AggregatesDeploymentWide()
    {
        var result = await _seriesService.GetNetGrowthAdditionAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(1400m, result.Value[0].NetGrowthAddition);
        Assert.Equal(-1200m, result.Value[1].NetGrowthAddition);
    }

    [Fact]
    public async Task GetNetGrowthAdditionAsync_OrgCallerWithTypeFilter_UsesTenantPath()
    {
        UseOrgACaller();

        var result = await _seriesService.GetNetGrowthAdditionAsync(
            _period, tenantTypes: [TenantType.B2B], ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(100m, result.Value[0].NetGrowthAddition);
        Assert.Equal(-100m, result.Value[1].NetGrowthAddition);
    }

    [Fact]
    public async Task GetCumulativeGrowthDeltaAsync_PlatformCaller_TracksRunningVariance()
    {
        var result = await _seriesService.GetCumulativeGrowthDeltaAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(1600m, result.Value[1].CurrentCumulative);
        Assert.Equal(650m, result.Value[1].PreviousCumulative);
        Assert.Equal(950m, result.Value[1].CumulativeGrowthDelta);
    }

    [Fact]
    public async Task GetRevenueEfficiencyAsync_OrgCaller_ComputesSharesAndMedians()
    {
        UseOrgACaller();

        var result = await _seriesService.GetRevenueEfficiencyAsync(_period, ct: CancellationToken.None);

        Assert.Equal(3, result.Tenants.Count);
        Assert.Equal(200m, result.GlobalAverageOrderValue);
        Assert.Equal(1m, result.MedianOrderVolume);
        Assert.Equal(33.33m, result.MedianPortfolioShare);
        var b2c = result.Tenants.Single(t => t.TenantId == _b2cTenantId);
        Assert.Equal(50m, b2c.PortfolioSharePercentage);
        Assert.Equal(200m, b2c.GrowthVelocity);
    }

    [Fact]
    public async Task GetCrossSegmentDistributionAsync_PlatformCaller_RanksWithinCohorts()
    {
        var result = await _distributionService.GetCrossSegmentDistributionAsync(_period, ct: CancellationToken.None);

        Assert.Equal(3, result.Cohorts.Count);
        var b2b = result.Cohorts.Single(c => c.Type == TenantType.B2B);
        Assert.Equal(2, b2b.TenantCount);
        Assert.Equal(550m, b2b.MedianAov);
        var wholesale = result.Tenants.Single(t => t.TenantId == _b2bTenantId);
        Assert.Equal(25, wholesale.AovPercentileRank);
    }

    [Fact]
    public async Task GetPortfolioImpactAsync_PlatformCaller_ComputesGrowthAndMedians()
    {
        var result = await _distributionService.GetPortfolioImpactAsync(_period, ct: CancellationToken.None);

        Assert.Equal(4, result.Tenants.Count);
        Assert.Equal(146.15m, result.GlobalGrowthPercentage);
        Assert.Equal(75m, result.MedianBaselineRevenue);
        Assert.Equal(15.625m, result.MedianPortfolioShare);
    }

    [Fact]
    public async Task GetOrderDistributionAsync_PlatformCaller_IncludesOnlyPeriodOrders()
    {
        AddOrder(_b2bTenantId, _period.CurrentStart.AddMinutes(40), 150m, "h-b2b-extra-1");
        AddOrder(_b2bTenantId, _period.CurrentStart.AddMinutes(50), 250m, "h-b2b-extra-2");
        AddOrder(_b2bTenantId, _period.CurrentStart.AddMinutes(70), 400m, "h-b2b-extra-3");
        _dbContext.SaveChanges();

        var result = await _distributionService.GetOrderDistributionAsync(
            _period, _b2bTenantId, binCount: 5, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
        Assert.Equal(5, result.Value.Sum(b => b.OrderCount));
    }

    [Fact]
    public async Task GetOrderDistributionAsync_OrgCaller_ReturnsScopeDeniedForForeignTenant()
    {
        UseOrgACaller();

        var result = await _distributionService.GetOrderDistributionAsync(_period, _otherTenantId, ct: CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task GetTransactionDensityAsync_AutoSelectsWidestPeriodForSmallSamples()
    {
        var result = await _distributionService.GetTransactionDensityAsync(
            TransactionDensityPeriod.Auto, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.TotalCount);
        Assert.Equal(168, result.Value.Points.Count);
        Assert.Equal(TransactionDensitySampleQuality.Sparse, result.Value.SampleQuality);
        Assert.Equal(TransactionDensityPeriod.T365, result.Value.EffectivePeriod);
    }

    private void AddOrder(Guid tenantId, DateTimeOffset createdDate, decimal value, string orderNumber)
    {
        _dbContext.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNumber = orderNumber,
            CreatedDate = createdDate,
            TotalValueIncVat = value,
            TotalValueExcVat = value,
            OrderState = OrderState.Completed,
        });
    }

    private void AddCancelledOrder(Guid tenantId, DateTimeOffset createdDate, decimal value, string orderNumber)
    {
        _dbContext.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNumber = orderNumber,
            CreatedDate = createdDate,
            TotalValueIncVat = value,
            TotalValueExcVat = value,
            OrderState = OrderState.Cancelled,
        });
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Characterization tests for the daily financial endpoints, pinned
/// against rollup history plus same-day live rows for platform and
/// scoped callers.
/// </summary>
public class FinancialDailyCharacterizationTests : IDisposable
{
    private readonly AnalyticsDbContext _dbContext;
    private readonly FinancialKpiService _kpiService;
    private readonly FinancialSeriesService _seriesService;
    private readonly FinancialDistributionService _distributionService;
    private readonly Mock<ICurrentAccess> _currentAccessMock;
    private readonly ResolvedPeriod _period;
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();
    private readonly Guid _b2bTenantId = Guid.NewGuid();
    private readonly Guid _b2cTenantId = Guid.NewGuid();
    private readonly Guid _mixedTenantId = Guid.NewGuid();
    private readonly Guid _otherTenantId = Guid.NewGuid();

    public FinancialDailyCharacterizationTests()
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

        var today = DateTimeOffset.UtcNow.Date;
        var currentStart = new DateTimeOffset(today.AddDays(-2), TimeSpan.Zero);
        _period = new ResolvedPeriod(
            currentStart,
            DateTimeOffset.UtcNow.AddHours(1),
            new DateTimeOffset(today.AddDays(-4), TimeSpan.Zero),
            currentStart,
            4,
            isHourly: false,
            includeActualTime: false);

        _dbContext.Tenants.AddRange(
            new Tenant { Id = _b2bTenantId, OrganizationId = _orgA, Name = "Wholesale", Type = TenantType.B2B },
            new Tenant { Id = _b2cTenantId, OrganizationId = _orgA, Name = "Retail", Type = TenantType.B2C },
            new Tenant { Id = _mixedTenantId, OrganizationId = _orgA, Name = "Marketplace", Type = TenantType.Mixed },
            new Tenant { Id = _otherTenantId, OrganizationId = _orgB, Name = "Other", Type = TenantType.B2B });

        var twoDaysAgo = new DateTimeOffset(today.AddDays(-2), TimeSpan.Zero);
        var oneDayAgo = new DateTimeOffset(today.AddDays(-1), TimeSpan.Zero);
        var threeDaysAgo = new DateTimeOffset(today.AddDays(-3), TimeSpan.Zero);
        _dbContext.DailyTenantRollups.AddRange(
            new DailyFinancialTenantRollup { CreatedDate = twoDaysAgo, OrganizationId = _orgA, TenantId = _b2bTenantId, Volume = 10, Revenue = 1000m },
            new DailyFinancialTenantRollup { CreatedDate = twoDaysAgo, OrganizationId = _orgA, TenantId = _b2cTenantId, Volume = 20, Revenue = 2000m },
            new DailyFinancialTenantRollup { CreatedDate = twoDaysAgo, OrganizationId = _orgB, TenantId = _otherTenantId, Volume = 90, Revenue = 9000m },
            new DailyFinancialTenantRollup { CreatedDate = oneDayAgo, OrganizationId = _orgA, TenantId = _mixedTenantId, Volume = 5, Revenue = 500m },
            new DailyFinancialTenantRollup { CreatedDate = threeDaysAgo, OrganizationId = _orgA, TenantId = _b2bTenantId, Volume = 4, Revenue = 400m },
            new DailyFinancialTenantRollup { CreatedDate = threeDaysAgo, OrganizationId = _orgA, TenantId = _b2cTenantId, Volume = 6, Revenue = 600m });
        _dbContext.DailyGlobalRollups.AddRange(
            new DailyFinancialGlobalRollup { CreatedDate = twoDaysAgo, OrganizationId = _orgA, GlobalVolume = 120, GlobalRevenue = 12000m },
            new DailyFinancialGlobalRollup { CreatedDate = threeDaysAgo, OrganizationId = _orgA, GlobalVolume = 10, GlobalRevenue = 1000m });

        var freshTimestamp = new DateTimeOffset(today.AddHours(1), TimeSpan.Zero);
        AddOrder(_b2bTenantId, freshTimestamp, 100m, "d-b2b-fresh");
        AddOrder(_b2cTenantId, freshTimestamp, 300m, "d-b2c-fresh");
        AddOrder(_otherTenantId, freshTimestamp, 700m, "d-other-fresh");
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetKpisAsync_PlatformCaller_MergesRollupHistoryWithFreshRows()
    {
        var result = await _kpiService.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(13600m, result.Value.CurrentRevenue);
        Assert.Equal(1000m, result.Value.PreviousRevenue);
        Assert.Equal(1260m, result.Value.RevenueGrowthPercentage);
        Assert.Equal(128, result.Value.TransactionVolume);
    }

    [Fact]
    public async Task GetKpisAsync_OrgCaller_ExcludesOtherOrganization()
    {
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_orgA, null, [UserRole.Admin]));

        var result = await _kpiService.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3900m, result.Value.CurrentRevenue);
        Assert.Equal(1000m, result.Value.PreviousRevenue);
        Assert.Equal(290m, result.Value.RevenueGrowthPercentage);
    }

    [Fact]
    public async Task GetAccumulatedRevenueAsync_PlatformCaller_BinsHistoryAndFreshRowsByDay()
    {
        var result = await _seriesService.GetAccumulatedRevenueAsync(_period, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Count);
        Assert.Equal(12000m, result.Value[0].CurrentRevenue);
        Assert.Equal(0m, result.Value[0].PreviousRevenue);
        Assert.Equal(500m, result.Value[1].CurrentRevenue);
        Assert.Equal(1000m, result.Value[1].PreviousRevenue);
        Assert.Equal(12500m, result.Value[1].CurrentAccumulated);
        Assert.Equal(1100m, result.Value[2].CurrentRevenue);
        Assert.Equal(13600m, result.Value[2].CurrentAccumulated);
        Assert.Equal(13600m, result.Value[3].CurrentAccumulated);
    }

    private void AddOrder(Guid tenantId, DateTimeOffset createdDate, decimal value, string orderNumber)
    {
        _dbContext.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderNumber = orderNumber,
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
