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
using Adwais.Infrastructure.Jobs.Monitor;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly Mock<IMonitoringProvider> _providerMock;
    private readonly Mock<ISystemEventService> _eventServiceMock;
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();

    public UpdateGlobalMonitoringStatsJobTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _providerMock = new Mock<IMonitoringProvider>();
        _providerMock.SetupGet(provider => provider.Provider).Returns("uptimerobot");
        _eventServiceMock = new Mock<ISystemEventService>();
    }

    private async Task SeedAsync(params OrganizationConfig[] configs)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.GlobalConfigs.Add(new GlobalConfig { Id = 1, MonitoringFetchEnabled = true });
        db.OrganizationConfigs.AddRange(configs);
        await db.SaveChangesAsync();
    }

    private async Task<OrganizationConfig> GetConfigAsync(Guid organizationId)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        return await db.OrganizationConfigs.SingleAsync(config => config.OrganizationId == organizationId);
    }

    private UpdateGlobalMonitoringStatsJob CreateJob()
        => new(
            new FakeDbContextFactory(_dbOptions),
            [_providerMock.Object],
            NullLogger<UpdateGlobalMonitoringStatsJob>.Instance,
            _eventServiceMock.Object);

    [Fact]
    public async Task ExecuteAsync_MonitoringDisabled_DoesNotCallProvider()
    {
        // Arrange
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.GlobalConfigs.Add(new GlobalConfig { Id = 1, MonitoringFetchEnabled = false });
            db.OrganizationConfigs.Add(new OrganizationConfig
            {
                OrganizationId = _orgA,
                MonitoringProviderSettings = "{}"
            });
            await db.SaveChangesAsync();
        }

        // Act
        await CreateJob().ExecuteAsync();

        // Assert
        _providerMock.Verify(
            provider => provider.GetAccountDetailsAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesOrgStatsAndClearsSyncError()
    {
        // Arrange
        await SeedAsync(new OrganizationConfig
        {
            OrganizationId = _orgA,
            MonitoringProviderSettings = "{}",
            LastSyncError = "stale error",
            MonitorsCount = 0
        });
        _providerMock.Setup(provider => provider.GetAccountDetailsAsync(_orgA))
            .ReturnsAsync(new MonitoringProviderAccount("ops@example.com", "Ops", 24, 50, "Pro"));

        // Act
        await CreateJob().ExecuteAsync();

        // Assert
        var config = await GetConfigAsync(_orgA);
        Assert.Equal(24, config.MonitorsCount);
        Assert.Equal(50, config.MonitorsLimit);
        Assert.Equal("Pro", config.ActiveSubscription);
        Assert.Null(config.LastSyncError);
    }

    [Fact]
    public async Task ExecuteAsync_OrgWithoutSettings_IsSkipped()
    {
        // Arrange
        await SeedAsync(new OrganizationConfig { OrganizationId = _orgA, MonitoringProviderSettings = null });

        // Act
        await CreateJob().ExecuteAsync();

        // Assert
        _providerMock.Verify(
            provider => provider.GetAccountDetailsAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_OneOrgFails_OthersStillUpdatedAndFailureRecorded()
    {
        // Arrange
        await SeedAsync(
            new OrganizationConfig { OrganizationId = _orgA, MonitoringProviderSettings = "{}" },
            new OrganizationConfig { OrganizationId = _orgB, MonitoringProviderSettings = "{}" });
        _providerMock.Setup(provider => provider.GetAccountDetailsAsync(_orgA))
            .ThrowsAsync(new HttpRequestException("invalid api key"));
        _providerMock.Setup(provider => provider.GetAccountDetailsAsync(_orgB))
            .ReturnsAsync(new MonitoringProviderAccount("b@example.com", "B", 10, 20, "Free"));

        // Act
        await CreateJob().ExecuteAsync();

        // Assert
        var failedConfig = await GetConfigAsync(_orgA);
        Assert.NotNull(failedConfig.LastSyncError);
        Assert.Contains("invalid api key", failedConfig.LastSyncError);

        var updatedConfig = await GetConfigAsync(_orgB);
        Assert.Equal(10, updatedConfig.MonitorsCount);
        Assert.Equal(20, updatedConfig.MonitorsLimit);
        Assert.Equal("Free", updatedConfig.ActiveSubscription);

        _eventServiceMock.Verify(
            service => service.LogErrorAsync(
                nameof(UpdateGlobalMonitoringStatsJob),
                It.Is<string>(message => message.Contains(_orgA.ToString())),
                It.IsAny<Exception>(),
                null),
            Times.Once);
    }
}
