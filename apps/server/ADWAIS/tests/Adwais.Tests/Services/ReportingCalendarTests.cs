// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Adwais.Application.Services;
using Adwais.Domain.Enums;
using Moq;
using Xunit;

namespace Adwais.Tests.Services;

public class ReportingCalendarTests
{
    private readonly Mock<IOrganizationConfigService> _configServiceMock;

    public ReportingCalendarTests()
    {
        _configServiceMock = new Mock<IOrganizationConfigService>();
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

    private static OrganizationConfigDto CreateConfig(string? timeZoneId) => new(
        WeatherLocation: null,
        WeatherFetchIntervalMinutes: 15,
        ReportingTimeZoneId: timeZoneId ?? "Europe/Stockholm",
        MonitoringProvider: "uptimerobot",
        MonitoringProviderSettings: new Dictionary<string, string?>(),
        MonitoringProviderConfiguredSecretKeys: [],
        OrderFetchEnabled: true,
        MonitoringFetchEnabled: true,
        OrderFetchIntervalMinutes: 60,
        UptimeFetchIntervalMinutes: 60,
        LatencyFetchIntervalMinutes: 10,
        UserStatsFetchIntervalMinutes: 60,
        FeedFetchIntervalHours: 2,
        MonitorsCount: null,
        MonitorsLimit: null,
        ActiveSubscription: null,
        LastSyncError: null);

    [Fact]
    public async Task GetTimeZoneAsync_WithOrgConfig_ReturnsConfiguredZone()
    {
        var orgId = Guid.NewGuid();
        _configServiceMock.Setup(c => c.GetConfigAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateConfig("America/New_York"));
        var calendar = new ReportingCalendar(_configServiceMock.Object, OrgAccess(orgId));

        var timeZone = await calendar.GetTimeZoneAsync(CancellationToken.None);

        Assert.Equal("America/New_York", timeZone.Id);
    }

    [Fact]
    public async Task GetTimeZoneAsync_WithoutOrgConfig_FallsBackToDefault()
    {
        var orgId = Guid.NewGuid();
        _configServiceMock.Setup(c => c.GetConfigAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationConfigDto?)null);
        var calendar = new ReportingCalendar(_configServiceMock.Object, OrgAccess(orgId));

        var timeZone = await calendar.GetTimeZoneAsync(CancellationToken.None);

        Assert.Equal("Europe/Stockholm", timeZone.Id);
    }

    [Fact]
    public async Task GetTimeZoneAsync_WithPlatformScope_ReturnsDefault()
    {
        var calendar = new ReportingCalendar(_configServiceMock.Object, PlatformAccess());

        var timeZone = await calendar.GetTimeZoneAsync(CancellationToken.None);

        Assert.Equal("Europe/Stockholm", timeZone.Id);
    }

    [Fact]
    public async Task GetTimeZoneAsync_WithInvalidZoneId_FallsBackToDefault()
    {
        var orgId = Guid.NewGuid();
        _configServiceMock.Setup(c => c.GetConfigAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateConfig("Not/AZone"));
        var calendar = new ReportingCalendar(_configServiceMock.Object, OrgAccess(orgId));

        var timeZone = await calendar.GetTimeZoneAsync(CancellationToken.None);

        Assert.Equal("Europe/Stockholm", timeZone.Id);
    }
}
