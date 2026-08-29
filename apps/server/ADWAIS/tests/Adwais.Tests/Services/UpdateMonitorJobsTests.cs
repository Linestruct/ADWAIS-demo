// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Infrastructure.Jobs.Monitor;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace Adwais.Tests.Services;

public class UpdateMonitorJobsTests
{
    private static (AnalyticsDbContext DbContext, Mock<IDbContextFactory<AnalyticsDbContext>> Factory) CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AnalyticsDbContext(options);
        var factoryMock = new Mock<IDbContextFactory<AnalyticsDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AnalyticsDbContext(options));
        return (dbContext, factoryMock);
    }

    [Fact]
    public async Task UpdateMonitorUptimeJob_WhenSavingAvailability_MarksOrgDirty()
    {
        var (dbContext, factoryMock) = CreateDatabase();
        var orgId = Guid.NewGuid();
        var monitorId = 1;
        var monitor = new UptimeMonitor
        {
            Id = monitorId,
            Name = "Test Monitor",
            Provider = "uptimerobot",
            ExternalId = "ext-1",
            Url = "https://example.com",
            UptimeMonitorEnabled = true,
            Tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Tenant" }
        };
        dbContext.Monitors.Add(monitor);
        await dbContext.SaveChangesAsync();

        var providerMock = new Mock<IMonitoringProvider>();
        providerMock.SetupGet(p => p.Provider).Returns("uptimerobot");
        providerMock.Setup(p => p.GetUptimeAsync(
                orgId, "ext-1", It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<string?>()))
            .ReturnsAsync(99.5);

        var trackerMock = new Mock<IViewRefreshTracker>();
        var job = new UpdateMonitorUptimeJob(
            factoryMock.Object,
            new[] { providerMock.Object },
            new Mock<ISystemEventService>().Object,
            trackerMock.Object);

        await job.ExecuteAsync(
            orgId,
            monitorId,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 23, 59, 59, TimeSpan.Zero));

        trackerMock.Verify(
            tracker => tracker.MarkDirtyAsync(orgId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateMonitorLatencyJob_WhenSavingResponseTime_MarksOrgDirty()
    {
        var (dbContext, factoryMock) = CreateDatabase();
        var orgId = Guid.NewGuid();
        var monitorId = 1;
        var monitor = new UptimeMonitor
        {
            Id = monitorId,
            Name = "Test Monitor",
            Provider = "uptimerobot",
            ExternalId = "ext-1",
            Url = "https://example.com",
            UptimeMonitorEnabled = true,
            Tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Tenant" }
        };
        dbContext.Monitors.Add(monitor);
        await dbContext.SaveChangesAsync();

        var providerMock = new Mock<IMonitoringProvider>();
        providerMock.SetupGet(p => p.Provider).Returns("uptimerobot");
        providerMock.Setup(p => p.GetResponseTimeAsync(
                orgId, "ext-1", It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<string?>()))
            .ReturnsAsync((120, 90, 150));

        var trackerMock = new Mock<IViewRefreshTracker>();
        var job = new UpdateMonitorLatencyJob(
            factoryMock.Object,
            new[] { providerMock.Object },
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ISystemEventService>().Object,
            trackerMock.Object);

        await job.ExecuteAsync(
            orgId,
            monitorId,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 0, 10, 0, TimeSpan.Zero));

        trackerMock.Verify(
            tracker => tracker.MarkDirtyAsync(orgId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateMonitorLatencyJob_WhenNoResponseTime_DoesNotMarkDirty()
    {
        var (dbContext, factoryMock) = CreateDatabase();
        var orgId = Guid.NewGuid();
        var monitorId = 1;
        var monitor = new UptimeMonitor
        {
            Id = monitorId,
            Name = "Test Monitor",
            Provider = "uptimerobot",
            ExternalId = "ext-1",
            Url = "https://example.com",
            UptimeMonitorEnabled = true,
            Tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "Tenant" }
        };
        dbContext.Monitors.Add(monitor);
        await dbContext.SaveChangesAsync();

        var providerMock = new Mock<IMonitoringProvider>();
        providerMock.SetupGet(p => p.Provider).Returns("uptimerobot");
        providerMock.Setup(p => p.GetResponseTimeAsync(
                orgId, "ext-1", It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<string?>()))
            .ReturnsAsync(((int?)null, (int?)null, (int?)null));

        var trackerMock = new Mock<IViewRefreshTracker>();
        var job = new UpdateMonitorLatencyJob(
            factoryMock.Object,
            new[] { providerMock.Object },
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ISystemEventService>().Object,
            trackerMock.Object);

        await job.ExecuteAsync(
            orgId,
            monitorId,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 0, 10, 0, TimeSpan.Zero));

        trackerMock.Verify(
            tracker => tracker.MarkDirtyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
