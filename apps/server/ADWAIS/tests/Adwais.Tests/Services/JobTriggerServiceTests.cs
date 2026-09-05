// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services.Jobs;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Adwais.Tests.Services;

public class JobTriggerServiceTests
{
    private sealed class FakeDbContextFactory(DbContextOptions<AnalyticsDbContext> options)
        : IDbContextFactory<AnalyticsDbContext>
    {
        public AnalyticsDbContext CreateDbContext() => new(options);

        public Task<AnalyticsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AnalyticsDbContext(options));
    }

    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly Mock<IRecurringJobManager> _recurringJobManager = new();
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();

    public JobTriggerServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private JobTriggerService CreateService() => new(
        new FakeDbContextFactory(_dbOptions),
        _recurringJobManager.Object);

    private async Task SeedOrgAsync(Guid organizationId)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = organizationId,
            MonitoringProviderSettings = "{}"
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task TriggerOrderSyncAsync_OrgScope_TriggersThatOrgsRecurringJob()
    {
        await SeedOrgAsync(_orgA);
        await SeedOrgAsync(_orgB);

        await CreateService().TriggerOrderSyncAsync(_orgA);

        _recurringJobManager.Verify(
            manager => manager.Trigger($"dispatch-order-fetch-{_orgA}"),
            Times.Once);
        _recurringJobManager.Verify(
            manager => manager.Trigger(It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task TriggerOrderSyncAsync_PlatformScope_TriggersEveryOrg()
    {
        await SeedOrgAsync(_orgA);
        await SeedOrgAsync(_orgB);

        await CreateService().TriggerOrderSyncAsync(null);

        _recurringJobManager.Verify(manager => manager.Trigger($"dispatch-order-fetch-{_orgA}"), Times.Once);
        _recurringJobManager.Verify(manager => manager.Trigger($"dispatch-order-fetch-{_orgB}"), Times.Once);
    }

    [Fact]
    public async Task TriggerUptimeSyncAsync_OrgScope_TriggersThatOrgsRecurringJob()
    {
        await SeedOrgAsync(_orgA);

        await CreateService().TriggerUptimeSyncAsync(_orgA);

        _recurringJobManager.Verify(
            manager => manager.Trigger($"dispatch-monitoring-uptime-{_orgA}"),
            Times.Once);
    }

    [Fact]
    public async Task TriggerLatencySyncAsync_OrgScope_TriggersThatOrgsRecurringJob()
    {
        await SeedOrgAsync(_orgA);

        await CreateService().TriggerLatencySyncAsync(_orgA);

        _recurringJobManager.Verify(
            manager => manager.Trigger($"dispatch-monitoring-latency-{_orgA}"),
            Times.Once);
    }

    [Fact]
    public async Task TriggerFleetSyncAsync_OrgScope_TriggersThatOrgsRecurringJob()
    {
        await SeedOrgAsync(_orgA);

        await CreateService().TriggerFleetSyncAsync(_orgA);

        _recurringJobManager.Verify(
            manager => manager.Trigger($"sync-monitoring-fleet-{_orgA}"),
            Times.Once);
    }

    [Fact]
    public async Task TriggerAccountStatsSyncAsync_PlatformScope_TriggersEveryOrg()
    {
        await SeedOrgAsync(_orgA);
        await SeedOrgAsync(_orgB);

        await CreateService().TriggerAccountStatsSyncAsync(null);

        _recurringJobManager.Verify(
            manager => manager.Trigger($"sync-monitoring-account-stats-{_orgA}"),
            Times.Once);
        _recurringJobManager.Verify(
            manager => manager.Trigger($"sync-monitoring-account-stats-{_orgB}"),
            Times.Once);
    }

    [Fact]
    public async Task TriggerFeedSyncAsync_OrgScope_TriggersThatOrgsRecurringJob()
    {
        await SeedOrgAsync(_orgA);

        await CreateService().TriggerFeedSyncAsync(_orgA);

        _recurringJobManager.Verify(
            manager => manager.Trigger($"aggregate-intranet-feeds-{_orgA}"),
            Times.Once);
    }
}