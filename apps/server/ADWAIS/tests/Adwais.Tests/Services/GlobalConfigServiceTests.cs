// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
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
    private readonly Mock<IViewRefreshTracker> _viewRefreshTrackerMock;
    private readonly Mock<IMonitoringProvider> _monitoringProviderMock;
    private readonly Guid _orgId;

    public GlobalConfigServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        _options = new DbContextOptionsBuilder<AnalyticsDbContext>().UseInMemoryDatabase(dbName).Options;
        _eventServiceMock = new Mock<ISystemEventService>();
        _viewRefreshTrackerMock = new Mock<IViewRefreshTracker>();
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
        var orgConfigService = new OrganizationConfigService(
            dbContext, access, new[] { _monitoringProviderMock.Object }, _viewRefreshTrackerMock.Object);
        return new GlobalConfigService(dbContext, _eventServiceMock.Object, orgConfigService, access,
            new Mock<IJobTriggerService>().Object);
    }

    private static GlobalConfig SeedGlobalConfig(AnalyticsDbContext dbContext, int retentionDays = 2)
    {
        var config = new GlobalConfig
        {
            Id = 1,
            SystemEventRetentionDays = retentionDays,
            MatViewRefreshIntervalMinutes = 60,
            VisibleRecurringJobsCsv = "FinancialViewRefresh,SystemEventCleanup"
        };
        dbContext.GlobalConfigs.Add(config);
        return config;
    }

    [Fact]
    public async Task GetConfigAsync_ReturnsRetentionAndMeta()
    {
        var dbContext = new AnalyticsDbContext(_options);
        SeedGlobalConfig(dbContext, retentionDays: 7);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());

        var result = await service.GetConfigAsync();

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(7, result.SystemEventRetentionDays);
        Assert.Equal(60, result.MatViewRefreshIntervalMinutes);
    }

    [Fact]
    public async Task UpdateConfigAsync_UpdatesRetentionOnly()
    {
        var dbContext = new AnalyticsDbContext(_options);
        SeedGlobalConfig(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());
        var request = new UpdateGlobalConfigRequestDto(SystemEventRetentionDays: 30);

        var result = await service.UpdateConfigAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value.SystemEventRetentionDays);

        var dbCheck = new AnalyticsDbContext(_options);
        var configDb = await dbCheck.GlobalConfigs.FindAsync(1);
        Assert.NotNull(configDb);
        Assert.Equal(30, configDb.SystemEventRetentionDays);
    }

    [Fact]
    public async Task UpdateConfigAsync_UpdatesMatViewRefreshIntervalAndPersists()
    {
        var dbContext = new AnalyticsDbContext(_options);
        SeedGlobalConfig(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());
        var request = new UpdateGlobalConfigRequestDto(
            SystemEventRetentionDays: 30,
            MatViewRefreshIntervalMinutes: 30);

        var result = await service.UpdateConfigAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value.MatViewRefreshIntervalMinutes);

        var dbCheck = new AnalyticsDbContext(_options);
        var configDb = await dbCheck.GlobalConfigs.FindAsync(1);
        Assert.NotNull(configDb);
        Assert.Equal(30, configDb.MatViewRefreshIntervalMinutes);
    }

    [Fact]
    public async Task UpdateConfigAsync_RejectsIntervalBelowFiveMinutes()
    {
        var dbContext = new AnalyticsDbContext(_options);
        SeedGlobalConfig(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());
        var request = new UpdateGlobalConfigRequestDto(MatViewRefreshIntervalMinutes: 4);

        var result = await service.UpdateConfigAsync(request);
        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(Assert.Single(result.Errors));

        var dbCheck = new AnalyticsDbContext(_options);
        var configDb = await dbCheck.GlobalConfigs.FindAsync(1);
        Assert.Equal(60, configDb!.MatViewRefreshIntervalMinutes);
    }

    [Fact]
    public async Task UpdateConfigAsync_UpdatesVisibleRecurringJobsAndPersists()
    {
        var dbContext = new AnalyticsDbContext(_options);
        SeedGlobalConfig(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());
        var request = new UpdateGlobalConfigRequestDto(
            VisibleRecurringJobs: new[] { Adwais.Application.Common.Jobs.RecurringJobKind.CalendarSync, Adwais.Application.Common.Jobs.RecurringJobKind.RuntimeDataSeeder });

        var result = await service.UpdateConfigAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { Adwais.Application.Common.Jobs.RecurringJobKind.CalendarSync, Adwais.Application.Common.Jobs.RecurringJobKind.RuntimeDataSeeder }, result.Value.VisibleRecurringJobs);

        var dbCheck = new AnalyticsDbContext(_options);
        var configDb = await dbCheck.GlobalConfigs.FindAsync(1);
        Assert.Equal("CalendarSync,RuntimeDataSeeder", configDb!.VisibleRecurringJobsCsv);
    }

    [Fact]
    public async Task GetConfigAsync_ReturnsVisibleRecurringJobs()
    {
        var dbContext = new AnalyticsDbContext(_options);
        SeedGlobalConfig(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());

        var result = await service.GetConfigAsync();

        Assert.Equal(new[] { Adwais.Application.Common.Jobs.RecurringJobKind.FinancialViewRefresh, Adwais.Application.Common.Jobs.RecurringJobKind.SystemEventCleanup }, result!.VisibleRecurringJobs);
    }

    [Fact]
    public async Task UpdateFeedIntervalAsync_ShouldPersistInterval()
    {
        var dbContext = new AnalyticsDbContext(_options);
        SeedGlobalConfig(dbContext);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, OrgAccess());

        var result = await service.UpdateFeedIntervalAsync(12);

        Assert.True(result.IsSuccess);

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

        Assert.True(result.IsSuccess);
        Assert.Equal(120, result.Value.OrderFetchIntervalMinutes);
        Assert.Equal(30, result.Value.UptimeFetchIntervalMinutes);
        Assert.Equal(5, result.Value.FeedFetchIntervalHours);

        var dbCheck = new AnalyticsDbContext(_options);
        var configDb = await dbCheck.OrganizationConfigs.SingleAsync(c => c.OrganizationId == _orgId);
        Assert.Equal(120, configDb.OrderFetchIntervalMinutes);
        Assert.Equal(30, configDb.UptimeFetchIntervalMinutes);
        Assert.Equal(5, configDb.FeedFetchIntervalHours);
    }
}
