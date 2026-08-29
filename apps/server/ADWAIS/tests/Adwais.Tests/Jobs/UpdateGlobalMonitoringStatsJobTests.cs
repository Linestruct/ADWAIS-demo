// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Jobs.Monitor;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Adwais.Tests.Jobs;

public class UpdateGlobalMonitoringStatsJobTests
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

    public UpdateGlobalMonitoringStatsJobTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private async Task SeedAsync(params OrganizationConfig[] configs)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.OrganizationConfigs.AddRange(configs);
        await db.SaveChangesAsync();
    }

    private UpdateGlobalMonitoringStatsJob CreateJob()
        => new(
            new FakeDbContextFactory(_dbOptions),
            _backgroundJobClient.Object);

    private static Guid FirstOrgArg(Job job)
    {
        Assert.Equal(typeof(SyncOrganizationAccountStatsJob), job.Type);
        return Assert.IsType<Guid>(job.Args[0]);
    }

    private List<Guid> CaptureEnqueuedOrgs()
    {
        var enqueued = new List<Guid>();
        _backgroundJobClient
            .Setup(client => client.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Callback<Job, IState>((job, _) => enqueued.Add(FirstOrgArg(job)))
            .Returns("job-1");
        return enqueued;
    }

    [Fact]
    public async Task ExecuteAsync_EnqueuesOneStatsJobPerEnabledOrg()
    {
        await SeedAsync(
            new OrganizationConfig { OrganizationId = _orgA, MonitoringProviderSettings = "{}" },
            new OrganizationConfig { OrganizationId = _orgB, MonitoringProviderSettings = "{}" });

        var enqueued = CaptureEnqueuedOrgs();
        await CreateJob().ExecuteAsync();

        Assert.Equal(new[] { _orgA, _orgB }, enqueued);
    }

    [Fact]
    public async Task ExecuteAsync_MonitoringDisabled_DoesNotEnqueue()
    {
        await SeedAsync(new OrganizationConfig
        {
            OrganizationId = _orgA,
            MonitoringProviderSettings = "{}",
            MonitoringFetchEnabled = false
        });

        await CreateJob().ExecuteAsync();

        _backgroundJobClient.Verify(
            client => client.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OrgWithoutSettings_DoesNotEnqueue()
    {
        await SeedAsync(new OrganizationConfig { OrganizationId = _orgA, MonitoringProviderSettings = null });

        await CreateJob().ExecuteAsync();

        _backgroundJobClient.Verify(
            client => client.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Never);
    }
}