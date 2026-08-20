// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.Controllers;
using Adwais.Api.Controllers.Administration;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Adwais.Tests.Controllers;

public class GlobalConfigControllerTests
{
    private readonly Mock<IGlobalConfigService> _configServiceMock;
    private readonly GlobalConfigController _controller;

    public GlobalConfigControllerTests()
    {
        _configServiceMock = new Mock<IGlobalConfigService>();
        _controller = new GlobalConfigController(_configServiceMock.Object);
    }

    [Fact]
    public async Task GetConfig_ShouldReturnOkWithConfig()
    {
        // Arrange
        var responseDto = new GlobalConfigResponseDto(
            Id: 1,
            LastPolled: null,
            OrderFetchEnabled: true,
            MonitoringFetchEnabled: true,
            OrderFetchIntervalMinutes: 60,
            MonitoringProviderSettings: new Dictionary<string, string?>(),
            MonitoringProviderConfiguredSecretKeys: [],
            UptimeFetchIntervalMinutes: 60,
            LatencyFetchIntervalMinutes: 10,
            UserStatsFetchIntervalMinutes: 60,
            SystemEventRetentionDays: 2,
            MonitorsCount: null,
            MonitorsLimit: null,
            ActiveSubscription: null,
            FeedFetchIntervalHours: 2,
            WeatherLocation: "Karlstad",
            WeatherFetchIntervalMinutes: 15,
            ReportingTimeZoneId: "Europe/Stockholm"
        );

        _configServiceMock.Setup(s => s.GetConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.GetConfig();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<GlobalConfigResponseDto>(okResult.Value);
        Assert.Equal(2, returned.FeedFetchIntervalHours);
        _configServiceMock.Verify(s => s.GetConfigAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConfig_ShouldReturnOkWithUpdatedConfig()
    {
        // Arrange
        var request = new UpdateGlobalConfigRequestDto(FeedFetchIntervalHours: 6);
        var responseDto = new GlobalConfigResponseDto(
            Id: 1,
            LastPolled: null,
            OrderFetchEnabled: true,
            MonitoringFetchEnabled: true,
            OrderFetchIntervalMinutes: 60,
            MonitoringProviderSettings: new Dictionary<string, string?>(),
            MonitoringProviderConfiguredSecretKeys: [],
            UptimeFetchIntervalMinutes: 60,
            LatencyFetchIntervalMinutes: 10,
            UserStatsFetchIntervalMinutes: 60,
            SystemEventRetentionDays: 2,
            MonitorsCount: null,
            MonitorsLimit: null,
            ActiveSubscription: null,
            FeedFetchIntervalHours: 6,
            WeatherLocation: "Karlstad",
            WeatherFetchIntervalMinutes: 15,
            ReportingTimeZoneId: "Europe/Stockholm"
        );

        _configServiceMock.Setup(s => s.UpdateConfigAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.UpdateConfig(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<GlobalConfigResponseDto>(okResult.Value);
        Assert.Equal(6, returned.FeedFetchIntervalHours);
        _configServiceMock.Verify(s => s.UpdateConfigAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TriggerFeedFetch_ShouldTriggerHangfireJobAndReturnOk()
    {
        // Act
        var result = await _controller.TriggerFeedFetch();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _configServiceMock.Verify(s => s.TriggerFeedFetchAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFetchIntervals_ShouldReturnOkWithIntervals()
    {
        // Arrange
        var responseDto = new FetchIntervalsDto
        {
            LatencyFetchIntervalMinutes = 10,
            UptimeFetchIntervalMinutes = 60,
            StatusFetchIntervalMinutes = 5,
            OrderFetchIntervalMinutes = 60,
            UserStatsFetchIntervalMinutes = 60,
            FeedFetchIntervalHours = 2
        };

        _configServiceMock.Setup(s => s.GetFetchIntervalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.GetFetchIntervals();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<FetchIntervalsDto>(okResult.Value);
        Assert.Equal(2, returned.FeedFetchIntervalHours);
        _configServiceMock.Verify(s => s.GetFetchIntervalsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFetchIntervals_ShouldReturnOkWithUpdatedIntervals()
    {
        // Arrange
        var request = new UpdateFetchIntervalsRequestDto(FeedFetchIntervalHours: 4);
        var responseDto = new FetchIntervalsDto
        {
            LatencyFetchIntervalMinutes = 10,
            UptimeFetchIntervalMinutes = 60,
            StatusFetchIntervalMinutes = 5,
            OrderFetchIntervalMinutes = 60,
            UserStatsFetchIntervalMinutes = 60,
            FeedFetchIntervalHours = 4
        };

        _configServiceMock.Setup(s => s.UpdateFetchIntervalsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.UpdateFetchIntervals(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<FetchIntervalsDto>(okResult.Value);
        Assert.Equal(4, returned.FeedFetchIntervalHours);
        _configServiceMock.Verify(s => s.UpdateFetchIntervalsAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
