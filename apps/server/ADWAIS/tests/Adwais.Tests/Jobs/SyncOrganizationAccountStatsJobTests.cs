// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.Monitoring.Upstream;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Jobs.Monitor;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Adwais.Tests.Jobs;

public class SyncOrganizationAccountStatsJobTests
{
    private sealed class FakeDbContextFactory(DbContextOptions<AnalyticsDbContext> options)
        : IDbContextFactory<AnalyticsDbContext>
    {
        public AnalyticsDbContext CreateDbContext() => new(options);

        public Task<AnalyticsDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AnalyticsDbContext(options));
    }

    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly Mock<IMonitoringProvider> _providerMock;
    private readonly Mock<ISystemEventService> _eventServiceMock;
    private readonly Guid _orgA = Guid.NewGuid();

    public SyncOrganizationAccountStatsJobTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _providerMock = new Mock<IMonitoringProvider>();
        _providerMock.SetupGet(provider => provider.Provider).Returns("uptimerobot");
        _eventServiceMock = new Mock<ISystemEventService>();
    }

    private async Task SeedAsync(OrganizationConfig config)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.OrganizationConfigs.Add(config);
        await db.SaveChangesAsync();
    }

    private async Task<OrganizationConfig> GetConfigAsync(Guid organizationId)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        return await db.OrganizationConfigs.SingleAsync(config => config.OrganizationId == organizationId);
    }

    private SyncOrganizationAccountStatsJob CreateJob()
        => new(
            new FakeDbContextFactory(_dbOptions),
            [_providerMock.Object],
            NullLogger<SyncOrganizationAccountStatsJob>.Instance,
            _eventServiceMock.Object);

    [Fact]
    public async Task ExecuteAsync_MonitoringDisabled_DoesNotCallProvider()
    {
        await SeedAsync(new OrganizationConfig
        {
            OrganizationId = _orgA,
            MonitoringProviderSettings = "{}",
            MonitoringFetchEnabled = false
        });

        await CreateJob().ExecuteAsync(_orgA);

        _providerMock.Verify(
            provider => provider.GetAccountDetailsAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesOrgStatsAndClearsSyncError()
    {
        await SeedAsync(new OrganizationConfig
        {
            OrganizationId = _orgA,
            MonitoringProviderSettings = "{}",
            LastSyncError = "stale error",
            MonitorsCount = 0
        });
        _providerMock.Setup(provider => provider.GetAccountDetailsAsync(_orgA))
            .ReturnsAsync(new MonitoringProviderAccount("ops@example.com", "Ops", 24, 50, "Pro"));

        await CreateJob().ExecuteAsync(_orgA);

        var config = await GetConfigAsync(_orgA);
        Assert.Equal(24, config.MonitorsCount);
        Assert.Equal(50, config.MonitorsLimit);
        Assert.Equal("Pro", config.ActiveSubscription);
        Assert.Null(config.LastSyncError);
    }

    [Fact]
    public async Task ExecuteAsync_OrgWithoutSettings_IsSkipped()
    {
        await SeedAsync(new OrganizationConfig { OrganizationId = _orgA, MonitoringProviderSettings = null });

        await CreateJob().ExecuteAsync(_orgA);

        _providerMock.Verify(
            provider => provider.GetAccountDetailsAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ProviderFailure_RecordsErrorWithoutThrowing()
    {
        await SeedAsync(new OrganizationConfig { OrganizationId = _orgA, MonitoringProviderSettings = "{}" });
        _providerMock.Setup(provider => provider.GetAccountDetailsAsync(_orgA))
            .ThrowsAsync(new HttpRequestException("invalid api key"));

        var exception = await Record.ExceptionAsync(() => CreateJob().ExecuteAsync(_orgA));

        Assert.Null(exception);
        var config = await GetConfigAsync(_orgA);
        Assert.NotNull(config.LastSyncError);
        Assert.Contains("invalid api key", config.LastSyncError);
    }
}