// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
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
/// System tenants (per-org unassigned buckets and the legacy sentinel row)
/// must stay out of the financial endpoints. The flag is Tenant.IsSystem,
/// never the tenant id.
/// </summary>
public class FinancialSystemTenantTests : IDisposable
{
    private readonly AnalyticsDbContext _dbContext;
    private readonly FinancialKpiService _kpiService;
    private readonly FinancialSeriesService _seriesService;
    private readonly FinancialDistributionService _distributionService;
    private readonly Mock<ICurrentAccess> _currentAccessMock;
    private readonly ResolvedPeriod _period;
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _normalTenantId = Guid.NewGuid();
    private readonly Guid _bucketTenantId = Guid.NewGuid();

    public FinancialSystemTenantTests()
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
            new Tenant { Id = _normalTenantId, OrganizationId = _orgA, Name = "Normal", Type = TenantType.B2B },
            new Tenant { Id = _bucketTenantId, OrganizationId = _orgA, Name = "Unassigned", Type = TenantType.B2B, IsSystem = true },
            new Tenant { Id = IApplicationDbContext.SystemTenantGuid, OrganizationId = _orgA, Name = "Legacy", Type = TenantType.Mixed, IsSystem = true });
        AddOrder(_normalTenantId, _period.CurrentStart.AddMinutes(30), 100m, "sys-normal");
        AddOrder(_bucketTenantId, _period.CurrentStart.AddMinutes(30), 500m, "sys-bucket");
        AddOrder(IApplicationDbContext.SystemTenantGuid, _period.CurrentStart.AddMinutes(30), 700m, "sys-legacy");
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetKpisAsync_PlatformCaller_ExcludesSystemTenants()
    {
        var result = await _kpiService.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.Equal(100m, result.CurrentRevenue);
        Assert.Equal(1, result.TransactionVolume);
        Assert.Equal(1, result.ActiveTenants);
    }

    [Fact]
    public async Task GetKpisAsync_OrgCaller_ExcludesSystemTenants()
    {
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(_orgA, null, [UserRole.Admin]));

        var result = await _kpiService.GetKpisAsync(_period, ct: CancellationToken.None);

        Assert.Equal(100m, result.CurrentRevenue);
    }

    [Fact]
    public async Task GetRevenueEfficiencyAsync_PlatformCaller_ExcludesSystemTenantsFromTotals()
    {
        var result = await _seriesService.GetRevenueEfficiencyAsync(_period, ct: CancellationToken.None);

        Assert.Single(result.Tenants);
        Assert.Equal(_normalTenantId, result.Tenants[0].TenantId);
        Assert.Equal(100m, result.GlobalAverageOrderValue);
    }

    [Fact]
    public async Task GetTransactionDensityAsync_ExcludesSystemTenants()
    {
        var result = await _distributionService.GetTransactionDensityAsync(
            TransactionDensityPeriod.Auto, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetKpisAsync_DailyCaller_ExcludesSystemTenantRollups()
    {
        var today = DateTimeOffset.UtcNow.Date;
        var currentStart = new DateTimeOffset(today.AddDays(-2), TimeSpan.Zero);
        var dailyPeriod = new ResolvedPeriod(
            currentStart,
            DateTimeOffset.UtcNow.AddHours(1),
            new DateTimeOffset(today.AddDays(-4), TimeSpan.Zero),
            currentStart,
            4,
            isHourly: false,
            includeActualTime: false);
        var twoDaysAgo = new DateTimeOffset(today.AddDays(-2), TimeSpan.Zero);
        _dbContext.DailyTenantRollups.AddRange(
            new DailyFinancialTenantRollup { CreatedDate = twoDaysAgo, OrganizationId = _orgA, TenantId = _normalTenantId, Volume = 10, Revenue = 1000m },
            new DailyFinancialTenantRollup { CreatedDate = twoDaysAgo, OrganizationId = _orgA, TenantId = _bucketTenantId, Volume = 90, Revenue = 9000m });
        AddOrder(_normalTenantId, DateTimeOffset.UtcNow, 100m, "sys-normal-daily");
        _dbContext.SaveChanges();

        var result = await _kpiService.GetKpisAsync(dailyPeriod, ct: CancellationToken.None);

        // 1000 from the normal tenant rollup plus the 100 same-day live order.
        // The bucket rollup and the bucket live order stay out.
        Assert.Equal(1100m, result.CurrentRevenue);
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
