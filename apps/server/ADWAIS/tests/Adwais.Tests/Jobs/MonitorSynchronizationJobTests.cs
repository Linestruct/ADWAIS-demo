// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.Monitoring.Upstream;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Jobs.Monitor;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace Adwais.Tests.Jobs;

public class MonitorSynchronizationJobTests
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
    private readonly Mock<IRecurringJobManager> _recurringJobManager = new();
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();

    public MonitorSynchronizationJobTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private async Task SeedOrganizationAsync(Guid organizationId, bool enabled, string? settings)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = organizationId,
            MonitoringProvider = "prov-a",
            MonitoringProviderSettings = settings,
            MonitoringFetchEnabled = enabled
        });
        await db.SaveChangesAsync();
    }

    private MonitorSynchronizationJob CreateJob()
        => new(
            new FakeDbContextFactory(_dbOptions),
            _backgroundJobClient.Object,
            _recurringJobManager.Object);

    private static Guid FirstOrgArg(Job job)
    {
        Assert.Equal(typeof(SyncOrganizationFleetJob), job.Type);
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
    public async Task ExecuteAsync_EnqueuesOneFleetJobPerEnabledOrg()
    {
        await SeedOrganizationAsync(_orgA, enabled: true, settings: "{}");
        await SeedOrganizationAsync(_orgB, enabled: true, settings: "{}");

        var enqueued = CaptureEnqueuedOrgs();
        await CreateJob().ExecuteAsync();

        Assert.Equal(new[] { _orgA, _orgB }, enqueued);
    }

    [Fact]
    public async Task ExecuteAsync_DisabledOrUnconfiguredOrgs_AreSkipped()
    {
        await SeedOrganizationAsync(_orgA, enabled: false, settings: "{}");
        await SeedOrganizationAsync(_orgB, enabled: true, settings: null);

        await CreateJob().ExecuteAsync();

        _backgroundJobClient.Verify(
            client => client.Create(It.IsAny<Job>(), It.IsAny<IState>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ReschedulesFleetCronFromLocalMonitorIntervals()
    {
        await SeedOrganizationAsync(_orgA, enabled: true, settings: "{}");
        var tenantId = Guid.NewGuid();
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Tenants.Add(new Tenant { Id = tenantId, OrganizationId = _orgA, Name = "Tenant" });
            db.Monitors.Add(new Adwais.Domain.Entities.Monitoring.UptimeMonitor
            {
                Id = 1,
                Provider = "prov-a",
                ExternalId = "ext-1",
                TenantId = tenantId,
                Name = "Test Monitor",
                Url = "https://example.com",
                UpdateInterval = 300
            });
            await db.SaveChangesAsync();
        }

        await CreateJob().ExecuteAsync();

        _recurringJobManager.Verify(
            manager => manager.AddOrUpdate(
                "sync-monitoring-fleet",
                It.IsAny<Job>(),
                "*/5 * * * *",
                It.IsAny<RecurringJobOptions>()),
            Times.Once);
    }
}