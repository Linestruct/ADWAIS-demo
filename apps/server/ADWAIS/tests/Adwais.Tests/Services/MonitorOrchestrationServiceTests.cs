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
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Application.Services;
using Adwais.Application.Common.Models;
using Adwais.Application.DTOs.Monitoring;

namespace Adwais.Tests.Services;

public class MonitorOrchestrationServiceTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly AnalyticsDbContext _dbContext;
    private readonly Mock<IMonitoringProvider> _uptimeRobotServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly MonitorOrchestrationService _service;

    public MonitorOrchestrationServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AnalyticsDbContext(_dbOptions);

        _uptimeRobotServiceMock = new Mock<IMonitoringProvider>();
        _uptimeRobotServiceMock.SetupGet(provider => provider.Provider).Returns("uptimerobot");
        _cacheServiceMock = new Mock<ICacheService>();
        _dbContext.GlobalConfigs.Add(new GlobalConfig { Id = 1, OrderFetchIntervalMinutes = 60 });
        _dbContext.SaveChanges();

        _service = new MonitorOrchestrationService(
            _dbContext,
            new[] { _uptimeRobotServiceMock.Object },
            _cacheServiceMock.Object
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
        Assert.NotNull(result);
        Assert.Single(result.LatencyPoints);
        var point = result.LatencyPoints.First();

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

        Assert.Collection(
            result.LatencyPoints,
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
        Assert.NotNull(result);
        var matchingPoint = result.LatencyPoints.FirstOrDefault(p => p.Timestamp.Date == date.Date);
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

        Assert.NotNull(result);
        Assert.Equal(150, result.Kpis.AverageLatency);

        var excludingDev = await _service.GetAnalyticsAsync(
            period,
            tenantId,
            null,
            ["prod"],
            null,
            CancellationToken.None,
            excludedTags: ["dev"]);

        Assert.Equal(200, excludingDev.Kpis.AverageLatency);
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

        var monitor = Assert.Single(monitors);
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

        Assert.Equal(3, result.Points.Count);
        Assert.Equal(result.Points[0].Date, result.Points[0].EndDate);
        Assert.Equal(99.5, result.Points[0].UptimePercentage);
        Assert.Equal(99, result.Points[0].LowestMonitorUptimePercentage);
        Assert.Equal(2, result.Points[0].MonitorCount);
        Assert.Null(result.Points[1].UptimePercentage);
        Assert.Equal(0, result.Points[1].MonitorCount);
        Assert.Equal(98, result.Points[2].UptimePercentage);
        Assert.True(result.Points[2].IsPartial);
        Assert.Equal(99, result.AverageUptimePercentage);
        Assert.Equal(98, result.LowestUptimePercentage);
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

        Assert.Equal(14, result.Points.Count);
        Assert.All(result.Points, point => Assert.InRange(
            point.EndDate.DayNumber - point.Date.DayNumber + 1,
            1,
            7));
        Assert.Equal(periodStart.Date.AddDays(6), result.Points[0].EndDate.ToDateTime(TimeOnly.MinValue));
        Assert.Equal(99, result.AverageUptimePercentage);
    }

    [Fact]
    public async Task CreateMonitorAsync_ShouldInvokeUptimeRobot_AndAddToDatabase()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant" });
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

        _uptimeRobotServiceMock.Setup(s => s.CreateMonitorAsync("New Monitor", "https://new.com", "PING"))
            .ReturnsAsync(remoteMonitor);

        // Act
        var result = await _service.CreateMonitorAsync(tenantId, "New Monitor", "https://new.com", "ping", 99.5, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("9876", result.ExternalId);
        Assert.Equal("uptimerobot", result.Provider);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal("PING", result.Type);
        Assert.Equal(99.5, result.UptimeSla);

        var dbMonitor = await _dbContext.Monitors.FindAsync(result.Id);
        Assert.NotNull(dbMonitor);
        Assert.Equal("New Monitor", dbMonitor.Name);
    }

    [Fact]
    public async Task CreateMonitorAsync_ShouldDefaultTypeToHttp_WhenOmitted()
    {
        var tenantId = Guid.NewGuid();
        _dbContext.Tenants.Add(new Tenant { Id = tenantId, Name = "Tenant" });
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
            .Setup(service => service.CreateMonitorAsync("Default Monitor", "https://default.com", "HTTP"))
            .ReturnsAsync(remoteMonitor);

        var result = await _service.CreateMonitorAsync(
            tenantId,
            "Default Monitor",
            "https://default.com",
            null,
            null,
            CancellationToken.None);

        Assert.Equal("HTTP", result.Type);
        _uptimeRobotServiceMock.Verify(
            service => service.CreateMonitorAsync("Default Monitor", "https://default.com", "HTTP"),
            Times.Once);
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
        await _service.AssignMonitorAsync(50, tenantId, CancellationToken.None);

        // Assert
        var updated = await _dbContext.Monitors.FindAsync(50);
        Assert.NotNull(updated);
        Assert.Equal(tenantId, updated.TenantId);
    }

    [Fact]
    public async Task ReassignAllTenantMonitorsToSystemAsync_ShouldReassignMatchingMonitors()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
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
        Assert.Equal(IApplicationDbContext.SystemTenantGuid, updated1.TenantId);
        Assert.Equal(IApplicationDbContext.SystemTenantGuid, updated2.TenantId);
    }

    [Fact]
    public async Task PauseMonitorAsync_ShouldCallPause_AndSetDisabledInDb()
    {
        // Arrange
        var monitor = new UptimeMonitor { Id = 70, ExternalId = "70", TenantId = Guid.NewGuid(), Name = "M", Url = "https://url.com", UptimeMonitorEnabled = true };
        _dbContext.Monitors.Add(monitor);
        await _dbContext.SaveChangesAsync();

        _uptimeRobotServiceMock.Setup(s => s.PauseMonitorAsync("70"))
            .Returns(Task.CompletedTask);

        // Act
        await _service.PauseMonitorAsync(70, CancellationToken.None);

        // Assert
        var updated = await _dbContext.Monitors.FindAsync(70);
        Assert.NotNull(updated);
        Assert.False(updated.UptimeMonitorEnabled);
        _uptimeRobotServiceMock.Verify(s => s.PauseMonitorAsync("70"), Times.Once);
    }

    [Fact]
    public async Task UpdateMonitorAsync_ShouldPatchUptimeRobot_AndModifyDbFields()
    {
        // Arrange
        var monitor = new UptimeMonitor 
        { 
            Id = 80, 
            ExternalId = "80",
            TenantId = Guid.NewGuid(), 
            Name = "Old Name", 
            Url = "https://old.com", 
            Type = "HTTP",
            UptimeSla = 99.0, 
            Tags = new List<string> { "tag1" } 
        };
        _dbContext.Monitors.Add(monitor);
        await _dbContext.SaveChangesAsync();

        _uptimeRobotServiceMock.Setup(s => s.UpdateMonitorAsync("80", "New Name", "https://new.com", "PING", It.IsAny<List<string>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateMonitorAsync(80, "New Name", "https://new.com", "ping", 99.9, new List<string> { "tag2" }, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("https://new.com", result.Url);
        Assert.Equal("PING", result.Type);
        Assert.Equal(99.9, result.UptimeSla);
        Assert.Contains("tag2", result.Tags);
        _uptimeRobotServiceMock.Verify(s => s.UpdateMonitorAsync("80", "New Name", "https://new.com", "PING", It.IsAny<List<string>>()), Times.Once);
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
        await _service.DeleteMonitorAsync(tenantId, -3, CancellationToken.None);

        Assert.Equal("Updated store", updated.Name);
        Assert.Equal("https://new.example", updated.Url);
        Assert.True((await _dbContext.Monitors.FindAsync(-2))!.UptimeMonitorEnabled);
        Assert.Null(await _dbContext.Monitors.FindAsync(-3));
        _uptimeRobotServiceMock.Verify(service => service.UpdateMonitorAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<string>?>()), Times.Never);
        _uptimeRobotServiceMock.Verify(service => service.PauseMonitorAsync(It.IsAny<string>()), Times.Never);
        _uptimeRobotServiceMock.Verify(service => service.StartMonitorAsync(It.IsAny<string>()), Times.Never);
        _uptimeRobotServiceMock.Verify(service => service.DeleteMonitorAsync(It.IsAny<string>()), Times.Never);
    }
}
