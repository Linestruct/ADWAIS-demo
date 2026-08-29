// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Jobs;
using Adwais.Infrastructure.Jobs.Monitor;
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

    private async Task SeedOrgAsync(Guid organizationId, bool monitoringEnabled = true)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = organizationId,
            MonitoringProviderSettings = monitoringEnabled ? "{}" : null,
            MonitoringFetchEnabled = monitoringEnabled
        });
        await db.SaveChangesAsync();
    }

    private static Guid FirstOrgArg(Job job) => Assert.IsType<Guid>(job.Args[0]);

    [Fact]
    public async Task TriggerOrderSyncAsync_OrgScope_EnqueuesDispatchJobForThatOrg()
    {
        await SeedOrgAsync(_orgA);
        await SeedOrgAsync(_orgB);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerOrderSyncAsync(_orgA);

        var job = Assert.Single(jobs);
        Assert.Equal(typeof(OrderFetchDispatchJob), job.Type);
        Assert.Equal(_orgA, FirstOrgArg(job));
    }

    [Fact]
    public async Task TriggerOrderSyncAsync_PlatformScope_EnqueuesAllConfiguredOrgs()
    {
        await SeedOrgAsync(_orgA);
        await SeedOrgAsync(_orgB);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerOrderSyncAsync(null);

        Assert.Equal(2, jobs.Count);
        Assert.All(jobs, job => Assert.Equal(typeof(OrderFetchDispatchJob), job.Type));
        Assert.Contains(jobs, j => FirstOrgArg(j) == _orgA);
        Assert.Contains(jobs, j => FirstOrgArg(j) == _orgB);
    }

    [Fact]
    public async Task TriggerUptimeSyncAsync_OrgScope_EnqueuesDispatchJobForThatOrg()
    {
        await SeedOrgAsync(_orgA);
        await SeedOrgAsync(_orgB);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerUptimeSyncAsync(_orgA);

        var job = Assert.Single(jobs);
        Assert.Equal(typeof(MonitorUptimeDispatchJob), job.Type);
        Assert.Equal(_orgA, FirstOrgArg(job));
    }

    [Fact]
    public async Task TriggerLatencySyncAsync_OrgScope_EnqueuesDispatchJobForThatOrg()
    {
        await SeedOrgAsync(_orgA);
        await SeedOrgAsync(_orgB);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerLatencySyncAsync(_orgA);

        var job = Assert.Single(jobs);
        Assert.Equal(typeof(MonitorLatencyDispatchJob), job.Type);
        Assert.Equal(_orgA, FirstOrgArg(job));
    }

    [Fact]
    public async Task TriggerFleetSyncAsync_OrgScope_EnqueuesSingleOrgJob()
    {
        await SeedOrgAsync(_orgA);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerFleetSyncAsync(_orgA);

        var job = Assert.Single(jobs);
        Assert.Equal(typeof(SyncOrganizationFleetJob), job.Type);
        Assert.Equal(_orgA, FirstOrgArg(job));
    }

    [Fact]
    public async Task TriggerAccountStatsSyncAsync_PlatformScope_EnqueuesAllConfiguredOrgs()
    {
        await SeedOrgAsync(_orgA);
        await SeedOrgAsync(_orgB, monitoringEnabled: false);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerAccountStatsSyncAsync(null);

        Assert.Equal(2, jobs.Count);
        Assert.All(jobs, job => Assert.Equal(typeof(SyncOrganizationAccountStatsJob), job.Type));
    }

    [Fact]
    public async Task TriggerFeedSyncAsync_OrgScope_EnqueuesFeedJobForThatOrg()
    {
        await SeedOrgAsync(_orgA);
        var jobs = CaptureEnqueuedJobs();

        await CreateService().TriggerFeedSyncAsync(_orgA);

        var job = Assert.Single(jobs);
        Assert.Equal(typeof(AggregateOrganizationFeedsJob), job.Type);
        Assert.Equal(_orgA, FirstOrgArg(job));
    }
}