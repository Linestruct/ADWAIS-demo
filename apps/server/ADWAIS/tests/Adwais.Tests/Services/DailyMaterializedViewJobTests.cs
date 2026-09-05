// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Infrastructure.Jobs.MaterializedViews;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Adwais.Tests.Services;

public class DailyMaterializedViewJobTests
{
    [Fact]
    public async Task FinancialJob_AfterRefresh_ClearsDirtyState()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AnalyticsDbContext(options);
        var factoryMock = new Mock<IDbContextFactory<AnalyticsDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AnalyticsDbContext(options));

        var tracker = new ViewRefreshTracker(dbContext);
        await tracker.MarkDirtyAsync(Guid.NewGuid());
        var job = new RefreshFinancialMaterializedViewJob(factoryMock.Object, tracker);

        await job.RefreshAsync();

        Assert.False(await tracker.IsDirtyAsync());
    }

    [Fact]
    public async Task MonitoringJob_AfterRefresh_ClearsDirtyState()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AnalyticsDbContext(options);
        var factoryMock = new Mock<IDbContextFactory<AnalyticsDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AnalyticsDbContext(options));

        var tracker = new ViewRefreshTracker(dbContext);
        await tracker.MarkDirtyAsync(Guid.NewGuid());
        var job = new RefreshMonitoringMaterializedViewJob(factoryMock.Object, tracker);

        await job.RefreshAsync();

        Assert.False(await tracker.IsDirtyAsync());
    }
}