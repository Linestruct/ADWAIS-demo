// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Jobs.MaterializedViews;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Adwais.Tests.Services;

public class RefreshMaterializedViewsJobTests
{
    private sealed class TestableRefreshMaterializedViewsJob(
        IDbContextFactory<AnalyticsDbContext> dbContextFactory,
        IViewRefreshTracker tracker) : RefreshMaterializedViewsJob(dbContextFactory, tracker)
    {
        public int RefreshCallCount { get; private set; }

        protected override Task RefreshViewsAsync(AnalyticsDbContext dbContext, CancellationToken ct)
        {
            RefreshCallCount++;
            return Task.CompletedTask;
        }
    }

    private static (TestableRefreshMaterializedViewsJob Job, AnalyticsDbContext DbContext) CreateJob()
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AnalyticsDbContext(options);
        var factoryMock = new Mock<IDbContextFactory<AnalyticsDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AnalyticsDbContext(options));
        var tracker = new ViewRefreshTracker(dbContext);
        var job = new TestableRefreshMaterializedViewsJob(factoryMock.Object, tracker);
        return (job, dbContext);
    }

    [Fact]
    public async Task ExecuteAsync_WhenClean_SkipsRefresh()
    {
        var (job, _) = CreateJob();

        await job.ExecuteAsync();

        Assert.Equal(0, job.RefreshCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDirty_RefreshesOnceAndClears()
    {
        var (job, dbContext) = CreateJob();
        var tracker = new ViewRefreshTracker(dbContext);
        await tracker.MarkDirtyAsync(Guid.NewGuid());

        await job.ExecuteAsync();

        Assert.Equal(1, job.RefreshCallCount);
        Assert.False(await tracker.IsDirtyAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenMultipleOrgsDirty_CoalescesIntoOneRefresh()
    {
        var (job, dbContext) = CreateJob();
        var tracker = new ViewRefreshTracker(dbContext);
        await tracker.MarkDirtyAsync(Guid.NewGuid());
        await tracker.MarkDirtyAsync(Guid.NewGuid());
        await tracker.MarkDirtyAsync(Guid.NewGuid());

        await job.ExecuteAsync();

        Assert.Equal(1, job.RefreshCallCount);
        Assert.False(await tracker.IsDirtyAsync());
    }

    [Fact]
    public async Task ExecuteAsync_AfterClear_SecondRunSkips()
    {
        var (job, dbContext) = CreateJob();
        var tracker = new ViewRefreshTracker(dbContext);
        await tracker.MarkDirtyAsync(Guid.NewGuid());

        await job.ExecuteAsync();
        await job.ExecuteAsync();

        Assert.Equal(1, job.RefreshCallCount);
    }
}