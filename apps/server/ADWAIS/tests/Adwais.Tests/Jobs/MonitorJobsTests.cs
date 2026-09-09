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
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;
using Hangfire;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Infrastructure.Jobs.Monitor;
using Adwais.Infrastructure.Persistence;

namespace Adwais.Tests.Jobs;

public class MonitorJobsTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly Mock<IDbContextFactory<AnalyticsDbContext>> _dbContextFactoryMock;
    private readonly Mock<IMonitoringProvider> _uptimeRobotServiceMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ISystemEventService> _eventServiceMock;
    private readonly Mock<IRecurringJobManager> _recurringJobManagerMock;
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public MonitorJobsTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContextFactoryMock = new Mock<IDbContextFactory<AnalyticsDbContext>>();
        _dbContextFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AnalyticsDbContext(_dbOptions));

        _uptimeRobotServiceMock = new Mock<IMonitoringProvider>();
        _uptimeRobotServiceMock.SetupGet(provider => provider.Provider).Returns("uptimerobot");
        _cache = new MemoryCache(new MemoryCacheOptions());
        _eventServiceMock = new Mock<ISystemEventService>();
        _recurringJobManagerMock = new Mock<IRecurringJobManager>();

        // Seed common config
        using var db = new AnalyticsDbContext(_dbOptions);
        db.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = _orgId,
            MonitoringProviderSettings = "{\"apiKey\":\"api-key\"}",
            LatencyFetchIntervalMinutes = 10
        });
        db.Tenants.Add(new Tenant
        {
            Id = _tenantId,
            OrganizationId = _orgId,
            Name = "Test Tenant",
            OrderProvider = "litium",
            OrderProviderSettings = "{}"
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task UpdateMonitorLatencyJob_ShouldSaveResponseTime_WhenSuccessful()
    {
        // Arrange
        using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Monitors.Add(new UptimeMonitor
            {
                Id = 100,
                ExternalId = "100",
                TenantId = _tenantId,
                Name = "Test Monitor",
                Url = "https://test.com",
                UptimeMonitorEnabled = true
            });
            db.SaveChanges();
        }

        _uptimeRobotServiceMock.Setup(s => s.GetResponseTimeAsync(_orgId, "100", It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), "Test Monitor"))
            .ReturnsAsync((150, 100, 200));

        var job = new UpdateMonitorLatencyJob(
            _dbContextFactoryMock.Object,
            new[] { _uptimeRobotServiceMock.Object },
            _cache,
            _eventServiceMock.Object,
            new Mock<IViewRefreshTracker>().Object
        );

        // Act
        var now = DateTimeOffset.UtcNow;
        await job.ExecuteAsync(_orgId, 100, now.AddHours(-1), now);

        // Assert
        using (var db = new AnalyticsDbContext(_dbOptions))
        {
            var saved = await db.ResponseTimes.SingleOrDefaultAsync(rt => rt.MonitorId == 100);
            Assert.NotNull(saved);
            Assert.Equal(150, saved.Average);
            Assert.Equal(100, saved.Lowest);
            Assert.Equal(200, saved.Highest);

            var updatedMonitor = await db.Monitors.FindAsync(100);
            Assert.NotNull(updatedMonitor);
            Assert.Equal(now, updatedMonitor.LastLatencyUpdate);
            Assert.Null(updatedMonitor.LastSyncError);
        }
    }

    [Fact]
    public async Task UpdateMonitorLatencyJob_ShouldNeverCallUpstream_ForNegativeDemoMonitorId()
    {
        using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Monitors.Add(new UptimeMonitor
            {
                Id = -1,
                TenantId = _tenantId,
                Name = "Demo storefront",
                Url = "https://example.com",
                UptimeMonitorEnabled = true
            });
            db.SaveChanges();
        }

        var job = new UpdateMonitorLatencyJob(
            _dbContextFactoryMock.Object,
            new[] { _uptimeRobotServiceMock.Object },
            _cache,
            _eventServiceMock.Object,
            new Mock<IViewRefreshTracker>().Object);

        var now = DateTimeOffset.UtcNow;
        await job.ExecuteAsync(_orgId, -1, now.AddHours(-1), now);

        _uptimeRobotServiceMock.Verify(service => service.GetResponseTimeAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMonitorLatencyJob_ShouldLogErrorAndSetSyncError_WhenExceptionThrown()
    {
        // Arrange
        using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Monitors.Add(new UptimeMonitor
            {
                Id = 101,
                ExternalId = "101",
                TenantId = _tenantId,
                Name = "Test Monitor",
                Url = "https://test.com",
                UptimeMonitorEnabled = true
            });
            db.SaveChanges();
        }

        _uptimeRobotServiceMock.Setup(s => s.GetResponseTimeAsync(_orgId, "101", It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), "Test Monitor"))
            .ThrowsAsync(new HttpRequestException("API is down"));

        var job = new UpdateMonitorLatencyJob(
            _dbContextFactoryMock.Object,
            new[] { _uptimeRobotServiceMock.Object },
            _cache,
            _eventServiceMock.Object,
            new Mock<IViewRefreshTracker>().Object
        );

        // Act & Assert
        var now = DateTimeOffset.UtcNow;
        await Assert.ThrowsAsync<HttpRequestException>(() => job.ExecuteAsync(_orgId, 101, now.AddHours(-1), now));

        using (var db = new AnalyticsDbContext(_dbOptions))
        {
            var updatedMonitor = await db.Monitors.FindAsync(101);
            Assert.NotNull(updatedMonitor);
            Assert.NotNull(updatedMonitor.LastSyncError);
            Assert.Contains("Failed during step", updatedMonitor.LastSyncError);
        }

        _eventServiceMock.Verify(s => s.LogErrorAsync(
            nameof(UpdateMonitorLatencyJob),
            It.Is<string>(m => m.Contains("Failed during step")),
            It.IsAny<Exception>(),
            null
        ), Times.Once);
    }
}
