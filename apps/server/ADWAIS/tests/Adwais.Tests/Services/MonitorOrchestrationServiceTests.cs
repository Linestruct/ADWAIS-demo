// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Application.Services;
using Adwais.Application.Common.Models;
using Adwais.Application.DTOs.Monitoring;
using Adwais.Application.DTOs.Monitoring.Upstream;
using Adwais.Application.DTOs.GlobalConfig;

namespace Adwais.Tests.Services;

public class MonitorOrchestrationServiceTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly AnalyticsDbContext _dbContext;
    private readonly Mock<IMonitoringProvider> _uptimeRobotServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ICurrentAccess> _currentAccessMock;
    private readonly MonitorOrchestrationService _service;
    private readonly Guid _defaultOrgId = Guid.NewGuid();

    public MonitorOrchestrationServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AnalyticsDbContext(_dbOptions);

        _uptimeRobotServiceMock = new Mock<IMonitoringProvider>();
        _uptimeRobotServiceMock.SetupGet(provider => provider.Provider).Returns("uptimerobot");
        _uptimeRobotServiceMock.Setup(provider => provider.IsConfigured(It.IsAny<string?>())).Returns(true);
        _cacheServiceMock = new Mock<ICacheService>();
        _currentAccessMock = new Mock<ICurrentAccess>();
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new Adwais.Application.Common.Access.AccessScope(null, null, [UserRole.Admin]));
        _dbContext.GlobalConfigs.Add(new GlobalConfig { Id = 1 });
        _dbContext.OrganizationConfigs.Add(new OrganizationConfig { OrganizationId = _defaultOrgId });
        _dbContext.SaveChanges();

        var organizationConfigServiceMock = new Mock<IOrganizationConfigService>();
        organizationConfigServiceMock.Setup(service => service.GetConfigAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrganizationConfigDto(
                WeatherLocation: null,
                WeatherFetchIntervalMinutes: 15,
                ReportingTimeZoneId: "Europe/Stockholm",
                MonitoringProvider: "uptimerobot",
                MonitoringProviderSettings: new Dictionary<string, string?>(),
                MonitoringProviderConfiguredSecretKeys: [],
                OrderFetchEnabled: true,
                MonitoringFetchEnabled: true,
                OrderFetchIntervalMinutes: 60,
                UptimeFetchIntervalMinutes: 60,
                LatencyFetchIntervalMinutes: 10,
                UserStatsFetchIntervalMinutes: 60,
                FeedFetchIntervalHours: 2,
                MonitorsCount: null,
                MonitorsLimit: null,
                ActiveSubscription: null,
                LastSyncError: null));

        _service = new MonitorOrchestrationService(
            _dbContext,
            new[] { _uptimeRobotServiceMock.Object },
            _cacheServiceMock.Object,
            _currentAccessMock.Object,
            organizationConfigServiceMock.Object
        );
    }

    [Fact]
    public async Task GetAnalyticsAsync_Hourly_ShouldCalculateInMemoryPercentiles()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Type = TenantType.B2C };
        var monitor = new UptimeMonitor { Id = 1, TenantId = tenantId, Name = "Test Monitor", Url = "https://test.com", UptimeMonitorEnabled = true };
        
        _dbContext.Tenants.Add(tenant);
        _dbContext.Monitors.Add(monitor);

        // Add 10 response time data points for the same hour to calculate exact P10/P90 percentiles
        var baseDate = DateTimeOffset.UtcNow.Date.AddHours(12);
        var values = new double[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }; // Sorted values
        foreach (var val in values)
        {
            _dbContext.ResponseTimes.Add(new ResponseTime
            {
                Id = Guid.NewGuid(),
                MonitorId = 1,
                Date = baseDate.AddMinutes(5),
                Average = val,
                Lowest = val - 2,
                Highest = val + 2
            });
        }
        await _dbContext.SaveChangesAsync();

        var period = new ResolvedPeriod(
            currentStart: baseDate,
            currentEnd: baseDate.AddHours(1),
            previousStart: baseDate.AddHours(-1),
            previousEnd: baseDate,
            stepsInPeriod: 1,
            isHourly: true,
            includeActualTime: false
        );

        // Act
        var result = await _service.GetAnalyticsAsync(period, tenantId, null, null, null, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.LatencyPoints);
        var point = result.Value.LatencyPoints.First();

        // 10 values: indices 0 to 9.
        // P10 index = Math.Round(0.10 * 9) = 1. Value at index 1 is 20.
        // P90 index = Math.Round(0.90 * 9) = 8. Value at index 8 is 90.
        Assert.Equal(20, point.Lowest);
        Assert.Equal(90, point.Highest);
        Assert.Equal(55, point.Average); // Average of 10..100 is 55
    }

    [Fact]
    public async Task GetAnalyticsAsync_HalfHourBins_ShouldRetainSamplesFromBothHalvesOfHour()
    {
        var tenantId = Guid.NewGuid();
        var periodStart = new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);
        _dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Binning Tenant",
            Type = TenantType.B2C
        });
        _dbContext.Monitors.Add(new UptimeMonitor
        {
            Id = 1,
            TenantId = tenantId,
            Name = "Binning Monitor",
            Url = "https://binning.test",
            UptimeMonitorEnabled = true
        });
        _dbContext.ResponseTimes.AddRange(
            new ResponseTime { Id = Guid.NewGuid(), MonitorId = 1, Date = periodStart.AddMinutes(15), Average = 100 },
            new ResponseTime { Id = Guid.NewGuid(), MonitorId = 1, Date = periodStart.AddMinutes(45), Average = 200 });
        await _dbContext.SaveChangesAsync();

        var period = new ResolvedPeriod(
            periodStart,
            periodStart.AddHours(1),
            periodStart.AddHours(-1),
            periodStart,
            2,
            true,
            true);

        var result = await _service.GetAnalyticsAsync(period, tenantId, ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value.LatencyPoints,
            point =>
            {
                Assert.Equal(100, point.Average);
                Assert.Equal(LatencySampleState.Observed, point.CurrentState);
            },
            point =>
            {
                Assert.Equal(200, point.Average);
                Assert.Equal(LatencySampleState.Observed, point.CurrentState);
            });
    }

    [Fact]
    public async Task GetAnalyticsAsync_DailyHistorical_ShouldAggregateMaterializedPercentiles()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Type = TenantType.B2C };
        var monitor = new UptimeMonitor { Id = 1, TenantId = tenantId, Name = "Test Monitor", Url = "https://test.com", UptimeMonitorEnabled = true };
        
        _dbContext.Tenants.Add(tenant);
        _dbContext.Monitors.Add(monitor);

        var date = DateTimeOffset.UtcNow.Date.AddDays(-5);
        _dbContext.DailyLatencyMonitorRollups.Add(new DailyLatencyMonitorRollup
        {
            MonitorId = 1,
            Date = date,
            OrganizationId = tenantId,
            Average = 150,
            P10 = 120,
            P90 = 250
        });
        await _dbContext.SaveChangesAsync();

        var period = new ResolvedPeriod(
            currentStart: date.AddDays(-1),
            currentEnd: date.AddDays(2),
            previousStart: date.AddDays(-4),
            previousEnd: date.AddDays(-1),
            stepsInPeriod: 3,
            isHourly: false,
            includeActualTime: false
        );

        // Act
        var result = await _service.GetAnalyticsAsync(period, tenantId, null, null, null, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var matchingPoint = result.Value.LatencyPoints.FirstOrDefault(p => p.Timestamp.Date == date.Date);
        Assert.NotNull(matchingPoint);
        Assert.Equal(120, matchingPoint.Lowest);  // Maps to P10
        Assert.Equal(250, matchingPoint.Highest); // Maps to P90
        Assert.Equal(150, matchingPoint.Average);
    }

    [Fact]
    public async Task GetAnalyticsAsync_MultipleTags_ShouldMatchAnySelectedTag()
    {
        var tenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Tag Filter Tenant",
            Type = TenantType.B2C
        });
        _dbContext.Monitors.AddRange(
            new UptimeMonitor
            {
                Id = 1,
                TenantId = tenantId,
                Name = "Prod and Dev",
                Url = "https://both.test",
                Tags = new List<string> { "prod", "dev" }
            },
            new UptimeMonitor
            {
                Id = 2,
                TenantId = tenantId,
                Name = "Prod Only",
                Url = "https://prod.test",
                Tags = new List<string> { "prod" }
            },
            new UptimeMonitor
            {
                Id = 3,
                TenantId = tenantId,
                Name = "QA Only",
                Url = "https://qa.test",
                Tags = new List<string> { "qa" }
            });
        var now = DateTimeOffset.UtcNow;
        _dbContext.ResponseTimes.AddRange(
            new ResponseTime { Id = Guid.NewGuid(), MonitorId = 1, Date = now.AddHours(-1), Average = 100 },
            new ResponseTime { Id = Guid.NewGuid(), MonitorId = 2, Date = now.AddHours(-1), Average = 200 },
            new ResponseTime { Id = Guid.NewGuid(), MonitorId = 3, Date = now.AddHours(-1), Average = 900 });
        await _dbContext.SaveChangesAsync();

        var period = new ResolvedPeriod(
            currentStart: now.AddDays(-1),
            currentEnd: now,
            previousStart: now.AddDays(-2),
            previousEnd: now.AddDays(-1),
            stepsInPeriod: 24,
            isHourly: true,
            includeActualTime: false);

        var result = await _service.GetAnalyticsAsync(
            period,
            tenantId,
            null,
            new[] { "prod", "dev" },
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(150, result.Value.Kpis.AverageLatency);

        var excludingDev = await _service.GetAnalyticsAsync(
            period,
            tenantId,
            null,
            ["prod"],
            null,
            CancellationToken.None,
            excludedTags: ["dev"]);

        Assert.True(excludingDev.IsSuccess);
        Assert.Equal(200, excludingDev.Value.Kpis.AverageLatency);
    }

    [Fact]
    public async Task GetAnalyticsAsync_MonitorOutsideScope_ReturnsScopeDenied()
    {
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = otherTenantId, OrganizationId = Guid.NewGuid(), Name = "Other" });
        _dbContext.Monitors.Add(new UptimeMonitor { Id = 99, TenantId = otherTenantId, Name = "Other", Url = "https://other.example" });
        await _dbContext.SaveChangesAsync();
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new Adwais.Application.Common.Access.AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var result = await _service.GetAnalyticsAsync(
            CreateDefaultPeriod(), monitorId: 99, ct: CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task GetMonitorsAsync_ShouldHydrateAllMonitorsInScope()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.AddRange(
            new Tenant { Id = tenantId, Name = "Tenant A", Type = TenantType.B2C },
            new Tenant { Id = otherTenantId, Name = "Tenant B", Type = TenantType.B2C });
        _dbContext.Monitors.AddRange(
            new UptimeMonitor { Id = 11, TenantId = tenantId, Name = "A", Url = "https://a.test" },
            new UptimeMonitor { Id = 12, TenantId = otherTenantId, Name = "B", Url = "https://b.test" });

        var now = DateTimeOffset.UtcNow;
        var utcToday = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        _dbContext.MonitorAvailabilities.AddRange(
            new MonitorAvailability { Id = Guid.NewGuid(), MonitorId = 11, Date = utcToday, UptimePercentage = 99.5 },
            new MonitorAvailability { Id = Guid.NewGuid(), MonitorId = 12, Date = utcToday, UptimePercentage = 98.5 });
        await _dbContext.SaveChangesAsync();

        var period = new ResolvedPeriod(
            now.AddDays(-1), now.AddHours(1), now.AddDays(-2), now.AddDays(-1), 24, true, false);

        var monitors = await _service.GetMonitorsAsync(period, tenantId, CancellationToken.None);

        Assert.True(monitors.IsSuccess);
        var monitor = Assert.Single(monitors.Value);
        Assert.Equal(11, monitor.Id);
        Assert.Equal(99.5, monitor.CurrentUptimePercentage);
    }

    [Fact]
    public async Task GetAvailabilitySeriesAsync_ShouldAggregateMonitorsAndPreserveMissingDays()
    {
        var tenantId = Guid.NewGuid();
        _dbContext.Monitors.AddRange(
            new UptimeMonitor { Id = 21, TenantId = tenantId, Name = "One", Url = "https://one.test" },
            new UptimeMonitor { Id = 22, TenantId = tenantId, Name = "Two", Url = "https://two.test" });

        var dayOne = new DateTimeOffset(2026, 7, 20, 0, 0, 0, TimeSpan.Zero);
        _dbContext.MonitorAvailabilities.AddRange(
            new MonitorAvailability { Id = Guid.NewGuid(), MonitorId = 21, Date = dayOne, UptimePercentage = 100, IsFinalized = true },
            new MonitorAvailability { Id = Guid.NewGuid(), MonitorId = 22, Date = dayOne, UptimePercentage = 99, IsFinalized = true },
            new MonitorAvailability { Id = Guid.NewGuid(), MonitorId = 21, Date = dayOne.AddDays(2), UptimePercentage = 98 });
        await _dbContext.SaveChangesAsync();

        var period = new ResolvedPeriod(
            dayOne,
            dayOne.AddDays(2).AddHours(12),
            dayOne.AddDays(-3),
            dayOne,
            3,
            false,
            false);

        var result = await _service.GetAvailabilitySeriesAsync(
            period,
            TimeZoneInfo.Utc,
            tenantId,
            ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Points.Count);
        Assert.Equal(result.Value.Points[0].Date, result.Value.Points[0].EndDate);
        Assert.Equal(99.5, result.Value.Points[0].UptimePercentage);
        Assert.Equal(99, result.Value.Points[0].LowestMonitorUptimePercentage);
        Assert.Equal(2, result.Value.Points[0].MonitorCount);
        Assert.Null(result.Value.Points[1].UptimePercentage);
        Assert.Equal(0, result.Value.Points[1].MonitorCount);
        Assert.Equal(98, result.Value.Points[2].UptimePercentage);
        Assert.True(result.Value.Points[2].IsPartial);
        Assert.Equal(99, result.Value.AverageUptimePercentage);
        Assert.Equal(98, result.Value.LowestUptimePercentage);
    }

    [Fact]
    public async Task GetAvailabilitySeriesAsync_ShouldUseSevenDayBucketsForPeriodsOverNinetyDays()
    {
        var tenantId = Guid.NewGuid();
        _dbContext.Monitors.Add(new UptimeMonitor
        {
            Id = 23,
            TenantId = tenantId,
            Name = "Long period",
            Url = "https://long-period.test"
        });

        var periodStart = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var day = 0; day < 92; day++)
        {
            _dbContext.MonitorAvailabilities.Add(new MonitorAvailability
            {
                Id = Guid.NewGuid(),
                MonitorId = 23,
                Date = periodStart.AddDays(day),
                UptimePercentage = 99,
                IsFinalized = true
            });
        }
        await _dbContext.SaveChangesAsync();

        var period = new ResolvedPeriod(
            periodStart,
            periodStart.AddDays(91).AddHours(12),
            periodStart.AddDays(-91),
            periodStart,
            92,
            false,
            false);

        var result = await _service.GetAvailabilitySeriesAsync(
            period,
            TimeZoneInfo.Utc,
            tenantId,
            ct: CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value.Points.Count);
        Assert.All(result.Value.Points, point => Assert.InRange(
            point.EndDate.DayNumber - point.Date.DayNumber + 1,
            1,
            7));
        Assert.Equal(periodStart.Date.AddDays(6), result.Value.Points[0].EndDate.ToDateTime(TimeOnly.MinValue));
        Assert.Equal(99, result.Value.AverageUptimePercentage);
    }

    [Fact]
    public async Task GetAvailabilitySeriesAsync_MonitorOutsideScope_ReturnsScopeDenied()
    {
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = otherTenantId, OrganizationId = Guid.NewGuid(), Name = "Other" });
        _dbContext.Monitors.Add(new UptimeMonitor { Id = 100, TenantId = otherTenantId, Name = "Other", Url = "https://other.example" });
        await _dbContext.SaveChangesAsync();
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new Adwais.Application.Common.Access.AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var result = await _service.GetAvailabilitySeriesAsync(
            CreateDefaultPeriod(), TimeZoneInfo.Utc, monitorId: 100, ct: CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task CreateMonitorAsync_ShouldInvokeUptimeRobot_AndAddToDatabase()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant", OrganizationId = _defaultOrgId });
        await _dbContext.SaveChangesAsync();
        var remoteMonitor = new Adwais.Application.DTOs.Monitoring.Upstream.MonitoringProviderMonitor(
            ExternalId: "9876",
            Type: "PING",
            Name: "New Monitor",
            Url: "https://new.com",
            Status: "up",
            CreatedDate: DateTimeOffset.UtcNow,
            UpdateInterval: 300,
            Tags: new List<string>()
        );

        _uptimeRobotServiceMock.Setup(s => s.CreateMonitorAsync(_defaultOrgId, "New Monitor", "https://new.com", "PING"))
            .ReturnsAsync(remoteMonitor);

        // Act
        var result = await _service.CreateMonitorAsync(tenantId, "New Monitor", "https://new.com", "ping", 99.5, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Id > 0);
        Assert.Equal("9876", result.Value.ExternalId);
        Assert.Equal("uptimerobot", result.Value.Provider);
        Assert.Equal(tenantId, result.Value.TenantId);
        Assert.Equal("PING", result.Value.Type);
        Assert.Equal(99.5, result.Value.UptimeSla);

        var dbMonitor = await _dbContext.Monitors.FindAsync(result.Value.Id);
        Assert.NotNull(dbMonitor);
        Assert.Equal("New Monitor", dbMonitor.Name);
    }

    [Fact]
    public async Task CreateMonitorAsync_ShouldDefaultTypeToHttp_WhenOmitted()
    {
        var tenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant", OrganizationId = _defaultOrgId });
        await _dbContext.SaveChangesAsync();
        var remoteMonitor = new Adwais.Application.DTOs.Monitoring.Upstream.MonitoringProviderMonitor(
            ExternalId: "9877",
            Type: "HTTP",
            Name: "Default Monitor",
            Url: "https://default.com",
            Status: "up",
            CreatedDate: DateTimeOffset.UtcNow,
            UpdateInterval: 300,
            Tags: new List<string>());

        _uptimeRobotServiceMock
            .Setup(service => service.CreateMonitorAsync(_defaultOrgId, "Default Monitor", "https://default.com", "HTTP"))
            .ReturnsAsync(remoteMonitor);

        var result = await _service.CreateMonitorAsync(
            tenantId,
            "Default Monitor",
            "https://default.com",
            null,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("HTTP", result.Value.Type);
        _uptimeRobotServiceMock.Verify(
            service => service.CreateMonitorAsync(_defaultOrgId, "Default Monitor", "https://default.com", "HTTP"),
            Times.Once);
    }

    [Fact]
    public async Task CreateMonitorAsync_WithMissingProviderSettings_ReturnsConfigurationError()
    {
        var tenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant", OrganizationId = _defaultOrgId });
        await _dbContext.SaveChangesAsync();
        _uptimeRobotServiceMock.Setup(provider => provider.IsConfigured(It.IsAny<string?>())).Returns(false);

        var result = await _service.CreateMonitorAsync(
            tenantId, "New Monitor", "https://new.com", "HTTP", null, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ConfigurationError>(Assert.Single(result.Errors));
        _uptimeRobotServiceMock.Verify(
            provider => provider.CreateMonitorAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task AssignMonitorAsync_ShouldUpdateTenantId_WhenValid()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Type = TenantType.B2C };
        var monitor = new UptimeMonitor { Id = 50, TenantId = Guid.NewGuid(), Name = "Monitor", Url = "https://url.com" };
        
        _dbContext.Tenants.Add(tenant);
        _dbContext.Monitors.Add(monitor);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.AssignMonitorAsync(50, tenantId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await _dbContext.Monitors.FindAsync(50);
        Assert.NotNull(updated);
        Assert.Equal(tenantId, updated.TenantId);
    }

    [Fact]
    public async Task ReassignAllTenantMonitorsToSystemAsync_ShouldReassignMatchingMonitors()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var bucketId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant", OrganizationId = _defaultOrgId });
        _dbContext.Tenants.Add(new Tenant { Id = bucketId, Name = "Bucket", OrganizationId = _defaultOrgId, IsSystem = true });
        var monitor1 = new UptimeMonitor { Id = 60, TenantId = tenantId, Name = "M1", Url = "https://url.com" };
        var monitor2 = new UptimeMonitor { Id = 61, TenantId = tenantId, Name = "M2", Url = "https://url.com" };

        _dbContext.Monitors.AddRange(monitor1, monitor2);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.ReassignAllTenantMonitorsToSystemAsync(tenantId, CancellationToken.None);

        // Assert
        var updated1 = await _dbContext.Monitors.FindAsync(60);
        var updated2 = await _dbContext.Monitors.FindAsync(61);
        Assert.NotNull(updated1);
        Assert.NotNull(updated2);
        Assert.Equal(bucketId, updated1.TenantId);
        Assert.Equal(bucketId, updated2.TenantId);
    }

    [Fact]
    public async Task UnassignMonitorAsync_ShouldMoveMonitorToItsOrganizationBucket()
    {
        // Arrange
        var otherOrgId = Guid.NewGuid();
        var ownBucketId = Guid.NewGuid();
        var otherBucketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant", OrganizationId = _defaultOrgId });
        _dbContext.Tenants.Add(new Tenant { Id = ownBucketId, Name = "Own Bucket", OrganizationId = _defaultOrgId, IsSystem = true });
        _dbContext.Tenants.Add(new Tenant { Id = otherBucketId, Name = "Other Bucket", OrganizationId = otherOrgId, IsSystem = true });
        var monitor = new UptimeMonitor { Id = 62, TenantId = tenantId, Name = "M", Url = "https://url.com" };
        _dbContext.Monitors.Add(monitor);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.UnassignMonitorAsync(62, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await _dbContext.Monitors.FindAsync(62);
        Assert.NotNull(updated);
        Assert.Equal(ownBucketId, updated.TenantId);
    }

    [Fact]
    public async Task GetUnassignedMonitorsAsync_ShouldReturnOnlyCallerOrganizationBucket()
    {
        // Arrange
        var otherOrgId = Guid.NewGuid();
        var ownBucketId = Guid.NewGuid();
        var otherBucketId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = ownBucketId, Name = "Own Bucket", OrganizationId = _defaultOrgId, IsSystem = true });
        _dbContext.Tenants.Add(new Tenant { Id = otherBucketId, Name = "Other Bucket", OrganizationId = otherOrgId, IsSystem = true });
        _dbContext.Monitors.AddRange(
            new UptimeMonitor { Id = 63, TenantId = ownBucketId, Name = "Own", Url = "https://url.com" },
            new UptimeMonitor { Id = 64, TenantId = otherBucketId, Name = "Other", Url = "https://url.com" });
        await _dbContext.SaveChangesAsync();

        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new Adwais.Application.Common.Access.AccessScope(_defaultOrgId, null, [UserRole.Admin]));

        // Act
        var result = await _service.GetUnassignedMonitorsAsync(
            new ResolvedPeriod(DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-23), 7, false, false),
            CancellationToken.None);

        // Assert
        var names = result.Select(m => m.Name).ToList();
        Assert.Contains("Own", names);
        Assert.DoesNotContain("Other", names);
    }

    [Fact]
    public async Task GetMonitorsAsync_FleetListing_ExcludesUnassignedBuckets()
    {
        // Arrange
        var bucketId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = bucketId, Name = "Bucket", OrganizationId = _defaultOrgId, IsSystem = true });
        _dbContext.Monitors.Add(new UptimeMonitor { Id = 65, TenantId = bucketId, Name = "BucketMonitor", Url = "https://url.com" });
        await _dbContext.SaveChangesAsync();

        var period = new ResolvedPeriod(
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(-30),
            DateTimeOffset.UtcNow.AddDays(-23),
            7,
            false,
            false);

        // Act
        var fleet = await _service.GetMonitorsAsync(period, ct: CancellationToken.None);
        var unassigned = await _service.GetUnassignedMonitorsAsync(period, CancellationToken.None);

        // Assert
        Assert.True(fleet.IsSuccess);
        Assert.DoesNotContain(fleet.Value, m => m.Name == "BucketMonitor");
        Assert.Contains(unassigned, m => m.Name == "BucketMonitor");
    }

    [Fact]
    public async Task CreateUnassignedMonitorAsync_OrgScope_CreatesInOwnOrganizationBucket()
    {
        // Arrange
        var bucketId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = bucketId, Name = "Bucket", OrganizationId = _defaultOrgId, IsSystem = true });
        await _dbContext.SaveChangesAsync();
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new Adwais.Application.Common.Access.AccessScope(_defaultOrgId, null, [UserRole.Admin]));
        _uptimeRobotServiceMock.Setup(s => s.CreateMonitorAsync(_defaultOrgId, "New", "https://new.example.com", "HTTP(S)"))
            .ReturnsAsync(new MonitoringProviderMonitor("ext-1", "HTTP(s)", "New", "https://new.example.com", "UP", DateTimeOffset.UtcNow, 300, []));

        // Act
        var monitor = await _service.CreateUnassignedMonitorAsync("New", "https://new.example.com", "HTTP(S)", null, CancellationToken.None);

        // Assert
        Assert.True(monitor.IsSuccess);
        Assert.Equal(bucketId, monitor.Value.TenantId);
    }

    [Fact]
    public async Task CreateUnassignedMonitorAsync_PlatformScope_UsesDefaultOrganizationBucket()
    {
        // Arrange
        var bucketId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant
        {
            Id = bucketId,
            Name = "Bucket",
            OrganizationId = Adwais.Application.Common.Interfaces.IApplicationDbContext.DefaultOrganizationGuid,
            IsSystem = true
        });
        await _dbContext.SaveChangesAsync();
        _uptimeRobotServiceMock.Setup(s => s.CreateMonitorAsync(
                Adwais.Application.Common.Interfaces.IApplicationDbContext.DefaultOrganizationGuid,
                "New",
                "https://new.example.com",
                "HTTP(S)"))
            .ReturnsAsync(new MonitoringProviderMonitor("ext-2", "HTTP(S)", "New", "https://new.example.com", "UP", DateTimeOffset.UtcNow, 300, []));

        // Act
        var monitor = await _service.CreateUnassignedMonitorAsync("New", "https://new.example.com", "HTTP(S)", null, CancellationToken.None);

        // Assert
        Assert.True(monitor.IsSuccess);
        Assert.Equal(bucketId, monitor.Value.TenantId);
    }

    [Fact]
    public async Task CreateUnassignedMonitorAsync_DeniedScope_ReturnsScopeDenied()
    {
        _currentAccessMock.Setup(access => access.Scope).Returns((Adwais.Application.Common.Access.AccessScope?)null);

        var result = await _service.CreateUnassignedMonitorAsync("New", "https://new.example.com", "HTTP(S)", null, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task PauseMonitorAsync_ShouldCallPause_AndSetDisabledInDb()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var monitor = new UptimeMonitor { Id = 70, ExternalId = "70", TenantId = tenantId, Name = "M", Url = "https://url.com", UptimeMonitorEnabled = true };
        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant", OrganizationId = _defaultOrgId });
        _dbContext.Monitors.Add(monitor);
        await _dbContext.SaveChangesAsync();

        _uptimeRobotServiceMock.Setup(s => s.PauseMonitorAsync(_defaultOrgId, "70"))
            .Returns(Task.CompletedTask);

        // Act
        await _service.PauseMonitorAsync(70, CancellationToken.None);

        // Assert
        var updated = await _dbContext.Monitors.FindAsync(70);
        Assert.NotNull(updated);
        Assert.False(updated.UptimeMonitorEnabled);
        _uptimeRobotServiceMock.Verify(s => s.PauseMonitorAsync(_defaultOrgId, "70"), Times.Once);
    }

    [Fact]
    public async Task UpdateMonitorAsync_ShouldPatchUptimeRobot_AndModifyDbFields()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var monitor = new UptimeMonitor 
        { 
            Id = 80, 
            ExternalId = "80",
            TenantId = tenantId, 
            Name = "Old Name", 
            Url = "https://old.com", 
            Type = "HTTP",
            UptimeSla = 99.0, 
            Tags = new List<string> { "tag1" } 
        };
        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant", OrganizationId = _defaultOrgId });
        _dbContext.Monitors.Add(monitor);
        await _dbContext.SaveChangesAsync();

        _uptimeRobotServiceMock.Setup(s => s.UpdateMonitorAsync(_defaultOrgId, "80", "New Name", "https://new.com", "PING", It.IsAny<List<string>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateMonitorAsync(80, "New Name", "https://new.com", "ping", 99.9, new List<string> { "tag2" }, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value.Name);
        Assert.Equal("https://new.com", result.Value.Url);
        Assert.Equal("PING", result.Value.Type);
        Assert.Equal(99.9, result.Value.UptimeSla);
        Assert.Contains("tag2", result.Value.Tags);
        _uptimeRobotServiceMock.Verify(s => s.UpdateMonitorAsync(_defaultOrgId, "80", "New Name", "https://new.com", "PING", It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task DemoMonitorMutations_ShouldRemainLocal()
    {
        var tenantId = Guid.NewGuid();
        _dbContext.Monitors.AddRange(
            new UptimeMonitor { Id = -1, TenantId = tenantId, Name = "Store", Url = "https://store.example", UptimeMonitorEnabled = true },
            new UptimeMonitor { Id = -2, TenantId = tenantId, Name = "Account", Url = "https://account.example", UptimeMonitorEnabled = true },
            new UptimeMonitor { Id = -3, TenantId = tenantId, Name = "Checkout", Url = "https://checkout.example", UptimeMonitorEnabled = true });
        await _dbContext.SaveChangesAsync();

        var updated = await _service.UpdateMonitorAsync(
            -1,
            "Updated store",
            "https://new.example",
            "http",
            99.9,
            ["PROD"],
            CancellationToken.None);
        await _service.PauseMonitorAsync(-2, CancellationToken.None);
        await _service.StartMonitorAsync(-2, CancellationToken.None);
        var deleted = await _service.DeleteMonitorAsync(-3, CancellationToken.None);

        Assert.True(updated.IsSuccess);
        Assert.True(deleted.IsSuccess);
        Assert.Equal("Updated store", updated.Value.Name);
        Assert.Equal("https://new.example", updated.Value.Url);
        Assert.True((await _dbContext.Monitors.FindAsync(-2))!.UptimeMonitorEnabled);
        Assert.Null(await _dbContext.Monitors.FindAsync(-3));
        _uptimeRobotServiceMock.Verify(service => service.UpdateMonitorAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>()), Times.Never);
        _uptimeRobotServiceMock.Verify(service => service.PauseMonitorAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        _uptimeRobotServiceMock.Verify(service => service.StartMonitorAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        _uptimeRobotServiceMock.Verify(service => service.DeleteMonitorAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetMonitorsAsync_OrgScope_OnlyReturnsOrgTenantMonitors()
    {
        var orgTenantId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.AddRange(
            new Tenant { Id = orgTenantId, OrganizationId = _defaultOrgId, Name = "Org Store" },
            new Tenant { Id = otherTenantId, OrganizationId = otherOrgId, Name = "Other Org Store" });
        _dbContext.Monitors.AddRange(
            new UptimeMonitor { Id = -11, TenantId = orgTenantId, Name = "Own", Url = "https://own.example", UptimeMonitorEnabled = true },
            new UptimeMonitor { Id = -12, TenantId = otherTenantId, Name = "Other", Url = "https://other.example", UptimeMonitorEnabled = true });
        await _dbContext.SaveChangesAsync();

        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new Adwais.Application.Common.Access.AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var monitors = await _service.GetMonitorsAsync(CreateDefaultPeriod(), ct: CancellationToken.None);

        Assert.True(monitors.IsSuccess);
        Assert.Single(monitors.Value);
        Assert.Equal(-11, monitors.Value[0].Id);
    }

    [Fact]
    public async Task GetMonitorsAsync_RequestedTenantOutsideScope_ReturnsScopeDenied()
    {
        var orgTenantId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.AddRange(
            new Tenant { Id = orgTenantId, OrganizationId = _defaultOrgId, Name = "Org Store" },
            new Tenant { Id = otherTenantId, OrganizationId = otherOrgId, Name = "Other Org Store" });
        await _dbContext.SaveChangesAsync();

        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new Adwais.Application.Common.Access.AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var result = await _service.GetMonitorsAsync(CreateDefaultPeriod(), otherTenantId, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task AssignMonitorAsync_TargetTenantOutsideScope_ReturnsScopeDenied()
    {
        var otherOrgId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = otherTenantId, OrganizationId = otherOrgId, Name = "Other Org Store" });
        await _dbContext.SaveChangesAsync();

        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new Adwais.Application.Common.Access.AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var result = await _service.AssignMonitorAsync(-1, otherTenantId, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task GetMonitorsAsync_TenantScope_RestrictsToThatTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        _dbContext.Tenants.AddRange(
            new Tenant { Id = tenantA, OrganizationId = _defaultOrgId, Name = "Store A" },
            new Tenant { Id = tenantB, OrganizationId = _defaultOrgId, Name = "Store B" });
        _dbContext.Monitors.AddRange(
            new UptimeMonitor { Id = -21, TenantId = tenantA, Name = "A", Url = "https://a.example", UptimeMonitorEnabled = true },
            new UptimeMonitor { Id = -22, TenantId = tenantB, Name = "B", Url = "https://b.example", UptimeMonitorEnabled = true });
        await _dbContext.SaveChangesAsync();

        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new Adwais.Application.Common.Access.AccessScope(_defaultOrgId, tenantA, [UserRole.TenantViewer]));

        var monitors = await _service.GetMonitorsAsync(CreateDefaultPeriod(), ct: CancellationToken.None);

        Assert.True(monitors.IsSuccess);
        Assert.Single(monitors.Value);
        Assert.Equal(-21, monitors.Value[0].Id);
    }

    [Fact]
    public async Task GetMonitorAsync_OutsideScope_ReturnsScopeDenied()
    {
        var otherTenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = otherTenantId, OrganizationId = Guid.NewGuid(), Name = "Other" });
        await _dbContext.SaveChangesAsync();
        _currentAccessMock.Setup(access => access.Scope)
            .Returns(new Adwais.Application.Common.Access.AccessScope(_defaultOrgId, null, [UserRole.Employee]));

        var result = await _service.GetMonitorAsync(otherTenantId, 1, CreateDefaultPeriod(), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task GetAggregatedLatencyAsync_MissingMonitor_ReturnsNotFound()
    {
        var tenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = tenantId, OrganizationId = _defaultOrgId, Name = "Tenant" });
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetAggregatedLatencyAsync(
            tenantId, 1, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<NotFoundError>(Assert.Single(result.Errors));
    }

    private static Adwais.Application.Common.Models.ResolvedPeriod CreateDefaultPeriod()
    {
        var start = DateTimeOffset.UtcNow.AddHours(-2);
        return new Adwais.Application.Common.Models.ResolvedPeriod(
            start,
            start.AddHours(2),
            start.AddHours(-2),
            start,
            2,
            isHourly: true,
            includeActualTime: true);
    }
}
