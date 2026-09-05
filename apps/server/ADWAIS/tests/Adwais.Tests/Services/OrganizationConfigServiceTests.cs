// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Adwais.Domain;
using Adwais.Domain.Entities;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Adwais.Tests.Services;

public class OrganizationConfigServiceTests
{
    private static IMonitoringProvider CreateProvider()
    {
        var providerMock = new Mock<IMonitoringProvider>();
        providerMock.SetupGet(provider => provider.Provider).Returns("uptimerobot");
        providerMock.Setup(provider => provider.GetPublicSettings(It.IsAny<string?>()))
            .Returns(new Dictionary<string, string?>());
        providerMock.Setup(provider => provider.GetConfiguredSecretKeys(It.IsAny<string?>()))
            .Returns(Array.Empty<string>());
        providerMock.Setup(provider => provider.MergeSettings(It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, string?>>()))
            .Returns((string? current, IReadOnlyDictionary<string, string?> updates) => updates.Count == 0 ? current : string.Join(",", updates.Keys));
        return providerMock.Object;
    }

    private static IApplicationDbContext CreateDbContext(out AnalyticsDbContext dbContext)
    {
        var options = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        dbContext = new AnalyticsDbContext(options);
        return dbContext;
    }

    private static ICurrentAccess OrgAccess(Guid orgId)
    {
        var mock = new Mock<ICurrentAccess>();
        mock.Setup(access => access.Scope).Returns(new AccessScope(orgId, null, [UserRole.Admin]));
        return mock.Object;
    }

    private static ICurrentAccess PlatformAccess()
    {
        var mock = new Mock<ICurrentAccess>();
        mock.Setup(access => access.Scope).Returns(new AccessScope(null, null, [UserRole.Admin]));
        return mock.Object;
    }

    private static (OrganizationConfigService Service, Mock<IViewRefreshTracker> Tracker) CreateService(
        IApplicationDbContext context, ICurrentAccess access)
    {
        var tracker = new Mock<IViewRefreshTracker>();
        var service = new OrganizationConfigService(context, access, [CreateProvider()], tracker.Object);
        return (service, tracker);
    }

    [Fact]
    public async Task GetConfigAsync_WithOrgScope_ReturnsOrgConfig()
    {
        var context = CreateDbContext(out var dbContext);
        var orgId = Guid.NewGuid();
        dbContext.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = orgId,
            WeatherLocation = "Karlstad",
            ReportingTimeZoneId = "Europe/Stockholm"
        });
        await dbContext.SaveChangesAsync();

        var (service, _) = CreateService(context, OrgAccess(orgId));

        var result = await service.GetConfigAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Karlstad", result!.WeatherLocation);
        Assert.Equal("Europe/Stockholm", result.ReportingTimeZoneId);
    }

    [Fact]
    public async Task GetConfigAsync_WithMissingRow_ReturnsNull()
    {
        var context = CreateDbContext(out var dbContext);
        var orgId = Guid.NewGuid();

        var (service, _) = CreateService(context, OrgAccess(orgId));

        Assert.Null(await service.GetConfigAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetConfigAsync_WithPlatformScope_ReturnsNull()
    {
        var context = CreateDbContext(out var dbContext);

        var (service, _) = CreateService(context, PlatformAccess());

        Assert.Null(await service.GetConfigAsync(CancellationToken.None));
    }

    [Fact]
    public async Task UpdateConfigAsync_CreatesRowWithDefaults_WhenMissing()
    {
        var context = CreateDbContext(out var dbContext);
        var orgId = Guid.NewGuid();

        var (service, _) = CreateService(context, OrgAccess(orgId));

        var result = await service.UpdateConfigAsync(orgId, new UpdateOrganizationConfigRequestDto(
            WeatherLocation: "Stockholm",
            WeatherFetchIntervalMinutes: null,
            ReportingTimeZoneId: null,
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: null,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Stockholm", result.Value.WeatherLocation);
        Assert.Equal("Europe/Stockholm", result.Value.ReportingTimeZoneId);
        Assert.Equal(60, result.Value.OrderFetchIntervalMinutes);
        Assert.Equal(15, result.Value.WeatherFetchIntervalMinutes);

        var persisted = await dbContext.OrganizationConfigs.SingleAsync(c => c.OrganizationId == orgId);
        Assert.Equal("Stockholm", persisted.WeatherLocation);
    }

    [Fact]
    public async Task UpdateConfigAsync_UpdatesExistingRow()
    {
        var context = CreateDbContext(out var dbContext);
        var orgId = Guid.NewGuid();
        dbContext.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = orgId,
            WeatherLocation = "Karlstad",
            ReportingTimeZoneId = "Europe/Stockholm"
        });
        await dbContext.SaveChangesAsync();

        var (service, _) = CreateService(context, OrgAccess(orgId));

        var result = await service.UpdateConfigAsync(orgId, new UpdateOrganizationConfigRequestDto(
            WeatherLocation: "Goteborg",
            WeatherFetchIntervalMinutes: 30,
            ReportingTimeZoneId: "America/New_York",
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: 120,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Goteborg", result.Value.WeatherLocation);
        Assert.Equal("America/New_York", result.Value.ReportingTimeZoneId);
        Assert.Equal(30, result.Value.WeatherFetchIntervalMinutes);
        Assert.Equal(120, result.Value.OrderFetchIntervalMinutes);
    }

    [Fact]
    public async Task UpdateConfigAsync_WithUnknownMonitoringProvider_DoesNotPersistIt()
    {
        var context = CreateDbContext(out var dbContext);
        var orgId = Guid.NewGuid();
        dbContext.OrganizationConfigs.Add(new OrganizationConfig { OrganizationId = orgId });
        await dbContext.SaveChangesAsync();

        var (service, _) = CreateService(context, OrgAccess(orgId));

        var result = await service.UpdateConfigAsync(orgId, new UpdateOrganizationConfigRequestDto(
                WeatherLocation: null,
                WeatherFetchIntervalMinutes: null,
                ReportingTimeZoneId: null,
                MonitoringProvider: "no-such-provider",
                MonitoringProviderSettings: null,
                OrderFetchIntervalMinutes: null,
                UptimeFetchIntervalMinutes: null,
                LatencyFetchIntervalMinutes: null,
                UserStatsFetchIntervalMinutes: null,
                FeedFetchIntervalHours: null), CancellationToken.None);

        Assert.True(result.IsFailed);
        Assert.IsType<ValidationError>(Assert.Single(result.Errors));

        var persisted = await dbContext.OrganizationConfigs.SingleAsync(c => c.OrganizationId == orgId);
        Assert.Equal(IntegrationProviders.UptimeRobot, persisted.MonitoringProvider);
    }

    [Fact]
    public async Task UpdateConfigAsync_UpdatesFetchToggles()
    {
        var context = CreateDbContext(out var dbContext);
        var orgId = Guid.NewGuid();
        dbContext.OrganizationConfigs.Add(new OrganizationConfig { OrganizationId = orgId });
        await dbContext.SaveChangesAsync();

        var (service, _) = CreateService(context, OrgAccess(orgId));

        var result = await service.UpdateConfigAsync(orgId, new UpdateOrganizationConfigRequestDto(
            WeatherLocation: null,
            WeatherFetchIntervalMinutes: null,
            ReportingTimeZoneId: null,
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: null,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: null,
            OrderFetchEnabled: false,
            MonitoringFetchEnabled: false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.OrderFetchEnabled);
        Assert.False(result.Value.MonitoringFetchEnabled);

        var persisted = await dbContext.OrganizationConfigs.SingleAsync(c => c.OrganizationId == orgId);
        Assert.False(persisted.OrderFetchEnabled);
        Assert.False(persisted.MonitoringFetchEnabled);
    }

    [Fact]
    public async Task UpdateConfigAsync_WhenReportingTimeZoneChanges_MarksViewsDirty()
    {
        var context = CreateDbContext(out var dbContext);
        var orgId = Guid.NewGuid();
        dbContext.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = orgId,
            ReportingTimeZoneId = "Europe/Stockholm"
        });
        await dbContext.SaveChangesAsync();

        var (service, tracker) = CreateService(context, OrgAccess(orgId));

        await service.UpdateConfigAsync(orgId, new UpdateOrganizationConfigRequestDto(
            WeatherLocation: null,
            WeatherFetchIntervalMinutes: null,
            ReportingTimeZoneId: "UTC",
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: null,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: null), CancellationToken.None);

        tracker.Verify(
            trackerMock => trackerMock.MarkDirtyAsync(orgId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateConfigAsync_WhenReportingTimeZoneIsUnchanged_DoesNotMarkViewsDirty()
    {
        var context = CreateDbContext(out var dbContext);
        var orgId = Guid.NewGuid();
        dbContext.OrganizationConfigs.Add(new OrganizationConfig
        {
            OrganizationId = orgId,
            ReportingTimeZoneId = "UTC"
        });
        await dbContext.SaveChangesAsync();

        var (service, tracker) = CreateService(context, OrgAccess(orgId));

        await service.UpdateConfigAsync(orgId, new UpdateOrganizationConfigRequestDto(
            WeatherLocation: null,
            WeatherFetchIntervalMinutes: null,
            ReportingTimeZoneId: "UTC",
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: null,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: null), CancellationToken.None);

        tracker.Verify(
            trackerMock => trackerMock.MarkDirtyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
