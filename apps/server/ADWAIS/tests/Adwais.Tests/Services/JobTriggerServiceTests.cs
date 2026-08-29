// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services.Jobs;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
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
    private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();
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
        _backgroundJobClient.Object);

    private List<Job> CaptureEnqueuedJobs()
    {
        var jobs = new List<Job>();
        _backgroundJobClient
            .Setup(client => client.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, _) => jobs.Add(job))
            .Returns("job-1");
        return jobs;
    }

    private async Task SeedTenantAsync(Guid organizationId, Guid? tenantId = null, bool fetchingEnabled = true, bool currentlyFetching = false)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.Tenants.Add(new Tenant
        {
            Id = tenantId ?? Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "Tenant",
            OrderProvider = "litium",
            OrderProviderSettings = "{}",
            OrderFetchingEnabled = fetchingEnabled,
            CurrentlyFetching = currentlyFetching
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedMonitorAsync(Guid organizationId, int id)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.Monitors.Add(new Adwais.Domain.Entities.Monitoring.UptimeMonitor
        {
            Id = id,
            Provider = "uptimerobot",
            ExternalId = $"ext-{id}",
            Tenant = new Tenant { Id = Guid.NewGuid(), OrganizationId = organizationId, Name = "Tenant" },
            Name = "Monitor",
            Url = "https://example.com",
            UptimeMonitorEnabled = true
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task TriggerOrderSyncAsync_OrgScope_EnqueuesOnlyThatOrgsTenants()
    {
        await SeedTenantAsync(_orgA);
        await SeedTenantAsync(_orgB);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerOrderSyncAsync(_orgA);

        var job = Assert.Single(jobs);
        Assert.Equal(_orgA, Assert.IsType<Guid>(job.Args[0]));
        Assert.Equal(typeof(IOrderIngestionService), job.Type);
    }

    [Fact]
    public async Task TriggerOrderSyncAsync_PlatformScope_EnqueuesAllTenants()
    {
        await SeedTenantAsync(_orgA);
        await SeedTenantAsync(_orgB);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerOrderSyncAsync(null);

        Assert.Equal(2, jobs.Count);
        Assert.Contains(jobs, j => Assert.IsType<Guid>(j.Args[0]) == _orgA);
        Assert.Contains(jobs, j => Assert.IsType<Guid>(j.Args[0]) == _orgB);
    }

    [Fact]
    public async Task TriggerOrderSyncAsync_SkipsCurrentlyFetchingTenants()
    {
        await SeedTenantAsync(_orgA, currentlyFetching: true);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerOrderSyncAsync(_orgA);

        Assert.Empty(jobs);
    }

    [Fact]
    public async Task TriggerUptimeSyncAsync_OrgScope_EnqueuesOrgMonitorsWithOrgFirst()
    {
        await SeedMonitorAsync(_orgA, 1);
        await SeedMonitorAsync(_orgB, 2);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerUptimeSyncAsync(_orgA);

        var job = Assert.Single(jobs);
        Assert.Equal(_orgA, Assert.IsType<Guid>(job.Args[0]));
        Assert.Equal(1, Assert.IsType<int>(job.Args[1]));
    }

    [Fact]
    public async Task TriggerLatencySyncAsync_OrgScope_EnqueuesOrgMonitors()
    {
        await SeedMonitorAsync(_orgA, 1);
        await SeedMonitorAsync(_orgB, 2);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerLatencySyncAsync(_orgA);

        var job = Assert.Single(jobs);
        Assert.Equal(_orgA, Assert.IsType<Guid>(job.Args[0]));
        Assert.Equal(1, Assert.IsType<int>(job.Args[1]));
    }

    [Fact]
    public async Task TriggerFleetSyncAsync_OrgScope_EnqueuesSingleOrgJob()
    {
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerFleetSyncAsync(_orgA);

        var job = Assert.Single(jobs);
        Assert.Equal(typeof(Adwais.Infrastructure.Jobs.Monitor.SyncOrganizationFleetJob), job.Type);
        Assert.Equal(_orgA, Assert.IsType<Guid>(job.Args[0]));
    }

    [Fact]
    public async Task TriggerAccountStatsSyncAsync_PlatformScope_EnqueuesEnabledOrgsOnly()
    {
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.OrganizationConfigs.AddRange(
                new OrganizationConfig { OrganizationId = _orgA, MonitoringProviderSettings = "{}" },
                new OrganizationConfig { OrganizationId = _orgB, MonitoringProviderSettings = "{}", MonitoringFetchEnabled = false });
            await db.SaveChangesAsync();
        }
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerAccountStatsSyncAsync(null);

        var job = Assert.Single(jobs);
        Assert.Equal(typeof(Adwais.Infrastructure.Jobs.Monitor.SyncOrganizationAccountStatsJob), job.Type);
        Assert.Equal(_orgA, Assert.IsType<Guid>(job.Args[0]));
    }
}