// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Threading.Tasks;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Adwais.Tests.Services;

public class ViewRefreshTrackerTests
{
    private static AnalyticsDbContext CreateContext(out string dbName)
    {
        dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AnalyticsDbContext(options);
    }

    [Fact]
    public async Task MarkDirtyAsync_CreatesRowForOrganization()
    {
        var dbContext = CreateContext(out _);
        var tracker = new ViewRefreshTracker(dbContext);
        var orgId = Guid.NewGuid();

        await tracker.MarkDirtyAsync(orgId);

        var row = await dbContext.MaterializedViewDirty.SingleAsync();
        Assert.Equal(orgId, row.OrganizationId);
    }

    [Fact]
    public async Task MarkDirtyAsync_IsIdempotentPerOrganization()
    {
        var dbContext = CreateContext(out _);
        var tracker = new ViewRefreshTracker(dbContext);
        var orgId = Guid.NewGuid();

        await tracker.MarkDirtyAsync(orgId);
        await tracker.MarkDirtyAsync(orgId);

        Assert.Equal(1, await dbContext.MaterializedViewDirty.CountAsync());
    }

    [Fact]
    public async Task MarkDirtyAsync_TracksMultipleOrganizations()
    {
        var dbContext = CreateContext(out _);
        var tracker = new ViewRefreshTracker(dbContext);

        await tracker.MarkDirtyAsync(Guid.NewGuid());
        await tracker.MarkDirtyAsync(Guid.NewGuid());

        Assert.Equal(2, await dbContext.MaterializedViewDirty.CountAsync());
    }

    [Fact]
    public async Task IsDirtyAsync_ReturnsFalseWhenClean()
    {
        var dbContext = CreateContext(out _);
        var tracker = new ViewRefreshTracker(dbContext);

        Assert.False(await tracker.IsDirtyAsync());
    }

    [Fact]
    public async Task IsDirtyAsync_ReturnsTrueWhenMarked()
    {
        var dbContext = CreateContext(out _);
        var tracker = new ViewRefreshTracker(dbContext);
        await tracker.MarkDirtyAsync(Guid.NewGuid());

        Assert.True(await tracker.IsDirtyAsync());
    }

    [Fact]
    public async Task ClearDirtyAsync_RemovesAllRows()
    {
        var dbContext = CreateContext(out _);
        var tracker = new ViewRefreshTracker(dbContext);
        await tracker.MarkDirtyAsync(Guid.NewGuid());
        await tracker.MarkDirtyAsync(Guid.NewGuid());

        await tracker.ClearDirtyAsync();

        Assert.Equal(0, await dbContext.MaterializedViewDirty.CountAsync());
        Assert.False(await tracker.IsDirtyAsync());
    }

    [Fact]
    public async Task MarkDirtyAsync_AfterClear_CreatesRowAgain()
    {
        var dbContext = CreateContext(out _);
        var tracker = new ViewRefreshTracker(dbContext);
        var orgId = Guid.NewGuid();
        await tracker.MarkDirtyAsync(orgId);
        await tracker.ClearDirtyAsync();

        await tracker.MarkDirtyAsync(orgId);

        Assert.True(await tracker.IsDirtyAsync());
    }
}