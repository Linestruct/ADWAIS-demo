// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.Monitoring.Upstream;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Infrastructure.Jobs.Monitor;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace Adwais.Tests.Jobs;

public class SyncOrganizationFleetJobTests
{
    private sealed class FakeDbContextFactory(DbContextOptions<AnalyticsDbContext> options)
        : IDbContextFactory<AnalyticsDbContext>
    {
        public AnalyticsDbContext CreateDbContext() => new(options);

        public Task<AnalyticsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AnalyticsDbContext(options));
    }

    private sealed class TestableSyncOrganizationFleetJob(
        IDbContextFactory<AnalyticsDbContext> dbContextFactory,
        IEnumerable<IMonitoringProvider> monitoringProviders,
        IMemoryCache cache)
        : SyncOrganizationFleetJob(dbContextFactory, monitoringProviders, cache)
    {
        protected override string? CurrentSyncCron => null;
    }

    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();
    private readonly Mock<IMonitoringProvider> _providerA = new();
    private readonly Mock<IMonitoringProvider> _providerB = new();

    public SyncOrganizationFleetJobTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _providerA.SetupGet(provider => provider.Provider).Returns("prov-a");
        _providerB.SetupGet(provider => provider.Provider).Returns("prov-b");
    }

    private static MonitoringProviderMonitor RemoteMonitor(string externalId, string status = "UP")
        => new(externalId, "HTTP(s)", "Remote", "https://example.com", status,
            DateTimeOffset.UtcNow, 300, []);

    private async Task SeedOrganizationAsync(Guid organizationId, string providerName, Guid? bucketId)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = organizationId,
            MonitoringProvider = providerName,
            MonitoringProviderSettings = "{}"
        });
        if (bucketId is { } bucket)
        {
            db.Tenants.Add(new Tenant
            {
                Id = bucket,
                OrganizationId = organizationId,
                Name = "System (unassigned monitors)",
                IsSystem = true
            });
        }
        await db.SaveChangesAsync();
    }

    private SyncOrganizationFleetJob CreateJob()
    {
        var providers = new List<IMonitoringProvider> { _providerA.Object, _providerB.Object };
        return new TestableSyncOrganizationFleetJob(
            new FakeDbContextFactory(_dbOptions),
            providers,
            new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public async Task ExecuteAsync_IdenticalExternalIdsAcrossOrganizations_LandInRespectiveBuckets()
    {
        var bucketA = Guid.NewGuid();
        var bucketB = Guid.NewGuid();
        await SeedOrganizationAsync(_orgA, "prov-a", bucketA);
        await SeedOrganizationAsync(_orgB, "prov-b", bucketB);

        _providerA.Setup(p => p.GetMonitorsAsync(_orgA, null))
            .ReturnsAsync(new List<MonitoringProviderMonitor> { RemoteMonitor("dup-1") });
        _providerB.Setup(p => p.GetMonitorsAsync(_orgB, null))
            .ReturnsAsync(new List<MonitoringProviderMonitor> { RemoteMonitor("dup-1") });

        var job = CreateJob();
        await job.ExecuteAsync(_orgA);
        await job.ExecuteAsync(_orgB);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        var monitors = await verifyDb.Monitors.ToListAsync();
        Assert.Equal(2, monitors.Count);
        Assert.Contains(monitors, m => m.Provider == "prov-a" && m.ExternalId == "dup-1" && m.TenantId == bucketA);
        Assert.Contains(monitors, m => m.Provider == "prov-b" && m.ExternalId == "dup-1" && m.TenantId == bucketB);
    }

    [Fact]
    public async Task ExecuteAsync_NewUpstreamMonitor_LandsInOrganizationBucket()
    {
        var bucketA = Guid.NewGuid();
        var bucketB = Guid.NewGuid();
        await SeedOrganizationAsync(_orgA, "prov-a", bucketA);
        await SeedOrganizationAsync(_orgB, "prov-b", bucketB);

        _providerA.Setup(p => p.GetMonitorsAsync(_orgA, null))
            .ReturnsAsync(new List<MonitoringProviderMonitor> { RemoteMonitor("new-a") });
        _providerB.Setup(p => p.GetMonitorsAsync(_orgB, null))
            .ReturnsAsync(new List<MonitoringProviderMonitor>());

        await CreateJob().ExecuteAsync(_orgA);

        await using var verifyDb = new AnalyticsDbContext(_dbOptions);
        var monitor = Assert.Single(await verifyDb.Monitors.Where(m => m.ExternalId == "new-a").ToListAsync());
        Assert.Equal(bucketA, monitor.TenantId);
    }

    [Fact]
    public async Task ExecuteAsync_OrganizationWithoutBucket_ThrowsLoudly()
    {
        await SeedOrganizationAsync(_orgA, "prov-a", bucketId: null);

        _providerA.Setup(p => p.GetMonitorsAsync(_orgA, null))
            .ReturnsAsync(new List<MonitoringProviderMonitor> { RemoteMonitor("orphan-1") });

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateJob().ExecuteAsync(_orgA));
    }

    [Fact]
    public async Task ExecuteAsync_DisabledFetch_DoesNotCallProvider()
    {
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.OrganizationConfigs.Add(new OrganizationConfig
            {
                OrganizationId = _orgA,
                MonitoringProvider = "prov-a",
                MonitoringProviderSettings = "{}",
                MonitoringFetchEnabled = false
            });
            await db.SaveChangesAsync();
        }

        await CreateJob().ExecuteAsync(_orgA);

        _providerA.Verify(p => p.GetMonitorsAsync(_orgA, null), Times.Never);
    }
}