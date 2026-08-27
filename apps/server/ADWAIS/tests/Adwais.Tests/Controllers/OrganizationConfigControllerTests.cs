// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.Controllers.Administration;
using Adwais.Application.Common.Access;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Adwais.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Adwais.Tests.Controllers;

public class OrganizationConfigControllerTests
{
    private readonly Mock<IOrganizationConfigService> _configServiceMock;
    private readonly Mock<ICurrentAccess> _accessMock;
    private readonly OrganizationConfigController _controller;
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly OrganizationConfigDto _configDto;

    public OrganizationConfigControllerTests()
    {
        _configServiceMock = new Mock<IOrganizationConfigService>();
        _accessMock = new Mock<ICurrentAccess>();
        _controller = new OrganizationConfigController(_configServiceMock.Object, _accessMock.Object);
        _configDto = new OrganizationConfigDto(
            WeatherLocation: "Karlstad",
            WeatherFetchIntervalMinutes: 15,
            ReportingTimeZoneId: "Europe/Stockholm",
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
    }

    private void GivenOrgScope(Guid organizationId)
        => _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(organizationId, null, [UserRole.Admin]));

    private void GivenPlatformScope()
        => _accessMock.Setup(access => access.Scope)
            .Returns(new AccessScope(null, null, [UserRole.Admin]));

    private void GivenDeniedScope()
        => _accessMock.Setup(access => access.Scope).Returns((AccessScope?)null);

    [Fact]
    public async Task GetConfig_OrgScope_OwnOrganization_ReturnsConfig()
    {
        GivenOrgScope(_orgId);
        _configServiceMock.Setup(service => service.GetConfigAsync(_orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_configDto);

        var result = await _controller.GetConfig(_orgId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(_configDto, ok.Value);
    }

    [Fact]
    public async Task GetConfig_OrgScope_OtherOrganization_ReturnsNotFound()
    {
        GivenOrgScope(_orgId);

        var result = await _controller.GetConfig(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        _configServiceMock.Verify(
            service => service.GetConfigAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetConfig_PlatformScope_AnyOrganization_ReturnsConfig()
    {
        GivenPlatformScope();
        var targetOrg = Guid.NewGuid();
        _configServiceMock.Setup(service => service.GetConfigAsync(targetOrg, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_configDto);

        var result = await _controller.GetConfig(targetOrg, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetConfig_UnknownOrganization_ReturnsNotFound()
    {
        GivenPlatformScope();
        var missingOrg = Guid.NewGuid();
        _configServiceMock.Setup(service => service.GetConfigAsync(missingOrg, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationConfigDto?)null);

        var result = await _controller.GetConfig(missingOrg, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetMyConfig_OrgScope_ReturnsScopeOrganizationConfig()
    {
        GivenOrgScope(_orgId);
        _configServiceMock.Setup(service => service.GetConfigAsync(_orgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_configDto);

        var result = await _controller.GetMyConfig(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(_configDto, ok.Value);
    }

    [Fact]
    public async Task GetMyConfig_PlatformScope_ReturnsNotFound()
    {
        GivenPlatformScope();

        var result = await _controller.GetMyConfig(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateConfig_OrgAdmin_OwnOrganization_ReturnsUpdatedConfig()
    {
        GivenOrgScope(_orgId);
        var request = new UpdateOrganizationConfigRequestDto(
            WeatherLocation: "Gothenburg",
            WeatherFetchIntervalMinutes: null,
            ReportingTimeZoneId: null,
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: null,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: null);
        _configServiceMock.Setup(service => service.UpdateConfigAsync(_orgId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_configDto);

        var result = await _controller.UpdateConfig(_orgId, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateConfig_OrgAdmin_OtherOrganization_ReturnsNotFound()
    {
        GivenOrgScope(_orgId);
        var request = new UpdateOrganizationConfigRequestDto(
            WeatherLocation: null,
            WeatherFetchIntervalMinutes: null,
            ReportingTimeZoneId: null,
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: null,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: null);

        var result = await _controller.UpdateConfig(Guid.NewGuid(), request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        _configServiceMock.Verify(
            service => service.UpdateConfigAsync(It.IsAny<Guid>(), It.IsAny<UpdateOrganizationConfigRequestDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateConfig_PlatformScope_AnyOrganization_ReturnsUpdatedConfig()
    {
        GivenPlatformScope();
        var targetOrg = Guid.NewGuid();
        var request = new UpdateOrganizationConfigRequestDto(
            WeatherLocation: null,
            WeatherFetchIntervalMinutes: null,
            ReportingTimeZoneId: null,
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: null,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: null);
        _configServiceMock.Setup(service => service.UpdateConfigAsync(targetOrg, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_configDto);

        var result = await _controller.UpdateConfig(targetOrg, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateMyConfig_OrgAdmin_ReturnsUpdatedConfig()
    {
        GivenOrgScope(_orgId);
        var request = new UpdateOrganizationConfigRequestDto(
            WeatherLocation: "Stockholm",
            WeatherFetchIntervalMinutes: null,
            ReportingTimeZoneId: null,
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: null,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: null);
        _configServiceMock.Setup(service => service.UpdateConfigAsync(_orgId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_configDto);

        var result = await _controller.UpdateMyConfig(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateMyConfig_PlatformScope_ReturnsForbid()
    {
        GivenPlatformScope();
        var request = new UpdateOrganizationConfigRequestDto(
            WeatherLocation: null,
            WeatherFetchIntervalMinutes: null,
            ReportingTimeZoneId: null,
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: null,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: null);

        var result = await _controller.UpdateMyConfig(request, CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }
}
