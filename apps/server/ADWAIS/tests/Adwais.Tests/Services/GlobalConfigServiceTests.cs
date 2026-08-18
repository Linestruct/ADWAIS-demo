// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Adwais.Tests.Services;

public class GlobalConfigServiceTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _options;
    private readonly Mock<ISystemEventService> _eventServiceMock;
    private readonly Mock<IReportingRollupRefresher> _reportingRollupRefresherMock;
    private readonly Mock<IMonitoringProvider> _monitoringProviderMock;
    private readonly Guid _orgId;

    public GlobalConfigServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        _options = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(dbName).Options;
        _eventServiceMock = new Mock<ISystemEventService>();
        _reportingRollupRefresherMock = new Mock<IReportingRollupRefresher>();
        _monitoringProviderMock = new Mock<IMonitoringProvider>();
        _monitoringProviderMock.SetupGet(provider => provider.Provider).Returns("uptimerobot");
        _monitoringProviderMock
            .Setup(provider => provider.GetPublicSettings(It.IsAny<string?>()))
            .Returns(new Dictionary<string, string?>());
        _monitoringProviderMock
            .Setup(provider => provider.GetConfiguredSecretKeys(It.IsAny<string?>()))
            .Returns(Array.Empty<string>());
        _orgId = Guid.NewGuid();

        // Setup mock Hangfire JobStorage to avoid "JobStorage.Current has not been initialized" exception
        var jobStorageMock = new Mock<JobStorage>();
        var connectionMock = new Mock<IStorageConnection>();
        var transactionMock = new Mock<IWriteOnlyTransaction>();
        connectionMock.Setup(x => x.CreateWriteTransaction()).Returns(transactionMock.Object);
        jobStorageMock.Setup(x => x.GetConnection()).Returns(connectionMock.Object);
        JobStorage.Current = jobStorageMock.Object;
    }

    private ICurrentAccess OrgAccess()
    {
        var mock = new Mock<ICurrentAccess>();
        mock.Setup(access => access.Scope).Returns(new AccessScope(_orgId, null, [UserRole.Admin]));
        return mock.Object;
    }

    private ICurrentAccess PlatformAccess()
    {
        var mock = new Mock<ICurrentAccess>();
        mock.Setup(access => access.Scope).Returns(new AccessScope(null, null, [UserRole.Admin]));
        return mock.Object;
    }

    private IApplicationDbContext CreateDbContext(out AnalyticsDbContext dbContext)
    {
        dbContext = new AnalyticsDbContext(_options);
        return dbContext;
    }

    private GlobalConfigService CreateService(AnalyticsDbContext dbContext, ICurrentAccess access)
    {
        var orgConfigService = new OrganizationConfigService(dbContext, access, new[] { _monitoringProviderMock.Object });
        return new GlobalConfigService(dbContext, _eventServiceMock.Object, _reportingRollupRefresherMock.Object, orgConfigService, access);
    }

    [Fact]
    public async Task GetConfigAsync_WithOrgScope_ReturnsMergedConfigDto()
    {
        var dbContext = new AnalyticsDbContext(_options);
        dbContext.GlobalConfigs.Add(new GlobalConfig
        {
            Id = 1,
            OrderFetchEnabled = true,
            MonitoringFetchEnabled = true,
            SystemEventRetentionDays = 2
        });
        dbContext.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = _orgId,
            WeatherLocation = "Karlstad",
            FeedFetchIntervalHours = 3
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());

        var result = await service.GetConfigAsync();

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Karlstad", result.WeatherLocation);
        Assert.Equal(3, result.FeedFetchIntervalHours);
        Assert.True(result.OrderFetchEnabled);
    }

    [Fact]
    public async Task GetConfigAsync_WithPlatformScope_ReturnsDefaultsForOrgFields()
    {
        var dbContext = new AnalyticsDbContext(_options);
        dbContext.GlobalConfigs.Add(new GlobalConfig
        {
            Id = 1,
            OrderFetchEnabled = true,
            MonitoringFetchEnabled = true,
            SystemEventRetentionDays = 2
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, PlatformAccess());

        var result = await service.GetConfigAsync();

        Assert.NotNull(result);
        Assert.Null(result.WeatherLocation);
        Assert.Equal(2, result.FeedFetchIntervalHours);
        Assert.Equal(60, result.OrderFetchIntervalMinutes);
    }

    [Fact]
    public async Task UpdateConfigAsync_UpdatesPlatformAndOrgFields()
    {
        var dbContext = new AnalyticsDbContext(_options);
        dbContext.GlobalConfigs.Add(new GlobalConfig
        {
            Id = 1,
            OrderFetchEnabled = true,
            MonitoringFetchEnabled = true,
            SystemEventRetentionDays = 2
        });
        dbContext.OrganizationConfigs.Add(new OrganizationConfig { OrganizationId = _orgId, FeedFetchIntervalHours = 2 });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());
        var request = new UpdateGlobalConfigRequestDto(FeedFetchIntervalHours: 4, OrderFetchEnabled: false);

        var result = await service.UpdateConfigAsync(request);

        Assert.Equal(4, result.FeedFetchIntervalHours);
        Assert.False(result.OrderFetchEnabled);

        var dbCheck = new AnalyticsDbContext(_options);
        var configDb = await dbCheck.GlobalConfigs.FindAsync(1);
        Assert.NotNull(configDb);
        Assert.False(configDb.OrderFetchEnabled);
        var orgConfigDb = await dbCheck.OrganizationConfigs.SingleAsync(c => c.OrganizationId == _orgId);
        Assert.Equal(4, orgConfigDb.FeedFetchIntervalHours);
    }

    [Fact]
    public async Task UpdateConfigAsync_WhenReportingTimeZoneChanges_ShouldRefreshFinancialRollups()
    {
        var dbContext = new AnalyticsDbContext(_options);
        dbContext.GlobalConfigs.Add(new GlobalConfig
        {
            Id = 1,
            OrderFetchEnabled = true,
            MonitoringFetchEnabled = true,
            SystemEventRetentionDays = 2
        });
        dbContext.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = _orgId,
            ReportingTimeZoneId = "Europe/Stockholm"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());

        var result = await service.UpdateConfigAsync(
            new UpdateGlobalConfigRequestDto(ReportingTimeZoneId: "UTC"));

        Assert.Equal("UTC", result.ReportingTimeZoneId);
        _reportingRollupRefresherMock.Verify(
            refresher => refresher.RefreshAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateConfigAsync_WithPerOrgFieldsAndPlatformScope_Throws()
    {
        var dbContext = new AnalyticsDbContext(_options);
        dbContext.GlobalConfigs.Add(new GlobalConfig
        {
            Id = 1,
            OrderFetchEnabled = true,
            MonitoringFetchEnabled = true,
            SystemEventRetentionDays = 2
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, PlatformAccess());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateConfigAsync(
            new UpdateGlobalConfigRequestDto(WeatherLocation: "Karlstad")));
    }

    [Fact]
    public async Task UpdateFeedIntervalAsync_ShouldPersistInterval()
    {
        var dbContext = new AnalyticsDbContext(_options);
        dbContext.GlobalConfigs.Add(new GlobalConfig
        {
            Id = 1,
            OrderFetchEnabled = true,
            MonitoringFetchEnabled = true,
            SystemEventRetentionDays = 2
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());

        await service.UpdateFeedIntervalAsync(12);

        var dbCheck = new AnalyticsDbContext(_options);
        var orgConfigDb = await dbCheck.OrganizationConfigs.SingleAsync(c => c.OrganizationId == _orgId);
        Assert.Equal(12, orgConfigDb.FeedFetchIntervalHours);
    }

    [Fact]
    public async Task GetFetchIntervalsAsync_WithOrgScope_ReturnsOrgIntervals()
    {
        var dbContext = new AnalyticsDbContext(_options);
        dbContext.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = _orgId,
            OrderFetchIntervalMinutes = 60,
            UptimeFetchIntervalMinutes = 50,
            LatencyFetchIntervalMinutes = 10,
            UserStatsFetchIntervalMinutes = 40,
            FeedFetchIntervalHours = 3
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());

        var result = await service.GetFetchIntervalsAsync();

        Assert.NotNull(result);
        Assert.Equal(60, result.OrderFetchIntervalMinutes);
        Assert.Equal(50, result.UptimeFetchIntervalMinutes);
        Assert.Equal(10, result.LatencyFetchIntervalMinutes);
        Assert.Equal(40, result.UserStatsFetchIntervalMinutes);
        Assert.Equal(3, result.FeedFetchIntervalHours);
    }

    [Fact]
    public async Task UpdateFetchIntervalsAsync_ShouldUpdateIntervalsAndPersist()
    {
        var dbContext = new AnalyticsDbContext(_options);
        dbContext.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = _orgId,
            OrderFetchIntervalMinutes = 60,
            UptimeFetchIntervalMinutes = 60,
            LatencyFetchIntervalMinutes = 10,
            UserStatsFetchIntervalMinutes = 60,
            FeedFetchIntervalHours = 2
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());

        var request = new UpdateFetchIntervalsRequestDto(
            OrderFetchIntervalMinutes: 120,
            UptimeFetchIntervalMinutes: 30,
            FeedFetchIntervalHours: 5
        );

        var result = await service.UpdateFetchIntervalsAsync(request);

        Assert.Equal(120, result.OrderFetchIntervalMinutes);
        Assert.Equal(30, result.UptimeFetchIntervalMinutes);
        Assert.Equal(5, result.FeedFetchIntervalHours);

        var dbCheck = new AnalyticsDbContext(_options);
        var configDb = await dbCheck.OrganizationConfigs.SingleAsync(c => c.OrganizationId == _orgId);
        Assert.Equal(120, configDb.OrderFetchIntervalMinutes);
        Assert.Equal(30, configDb.UptimeFetchIntervalMinutes);
        Assert.Equal(5, configDb.FeedFetchIntervalHours);
    }
}