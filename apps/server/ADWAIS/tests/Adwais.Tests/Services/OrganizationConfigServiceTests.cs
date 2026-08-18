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

        var service = new OrganizationConfigService(context, OrgAccess(orgId), [CreateProvider()]);

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

        var service = new OrganizationConfigService(context, OrgAccess(orgId), [CreateProvider()]);

        Assert.Null(await service.GetConfigAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetConfigAsync_WithPlatformScope_ReturnsNull()
    {
        var context = CreateDbContext(out var dbContext);

        var service = new OrganizationConfigService(context, PlatformAccess(), [CreateProvider()]);

        Assert.Null(await service.GetConfigAsync(CancellationToken.None));
    }

    [Fact]
    public async Task UpdateConfigAsync_CreatesRowWithDefaults_WhenMissing()
    {
        var context = CreateDbContext(out var dbContext);
        var orgId = Guid.NewGuid();

        var service = new OrganizationConfigService(context, OrgAccess(orgId), [CreateProvider()]);

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

        Assert.Equal("Stockholm", result.WeatherLocation);
        Assert.Equal("Europe/Stockholm", result.ReportingTimeZoneId);
        Assert.Equal(60, result.OrderFetchIntervalMinutes);
        Assert.Equal(15, result.WeatherFetchIntervalMinutes);

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

        var service = new OrganizationConfigService(context, OrgAccess(orgId), [CreateProvider()]);

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

        Assert.Equal("Goteborg", result.WeatherLocation);
        Assert.Equal("America/New_York", result.ReportingTimeZoneId);
        Assert.Equal(30, result.WeatherFetchIntervalMinutes);
        Assert.Equal(120, result.OrderFetchIntervalMinutes);
    }

    [Fact]
    public async Task UpdateConfigAsync_WithUnknownMonitoringProvider_DoesNotPersistIt()
    {
        var context = CreateDbContext(out var dbContext);
        var orgId = Guid.NewGuid();
        dbContext.OrganizationConfigs.Add(new OrganizationConfig { OrganizationId = orgId });
        await dbContext.SaveChangesAsync();

        var service = new OrganizationConfigService(context, OrgAccess(orgId), [CreateProvider()]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateConfigAsync(orgId, new UpdateOrganizationConfigRequestDto(
                WeatherLocation: null,
                WeatherFetchIntervalMinutes: null,
                ReportingTimeZoneId: null,
                MonitoringProvider: "no-such-provider",
                MonitoringProviderSettings: null,
                OrderFetchIntervalMinutes: null,
                UptimeFetchIntervalMinutes: null,
                LatencyFetchIntervalMinutes: null,
                UserStatsFetchIntervalMinutes: null,
                FeedFetchIntervalHours: null), CancellationToken.None));

        var persisted = await dbContext.OrganizationConfigs.SingleAsync(c => c.OrganizationId == orgId);
        Assert.Equal(IntegrationProviders.UptimeRobot, persisted.MonitoringProvider);
    }
}
