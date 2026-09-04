// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.Controllers;
using Adwais.Api.Controllers.Administration;
using Adwais.Api.Extensions;
using Adwais.Application.Common.Errors;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using FluentResults;
using Microsoft.AspNetCore.Http;

namespace Adwais.Tests.Controllers;

public class GlobalConfigControllerTests
{
    private readonly Mock<IGlobalConfigService> _configServiceMock;
    private readonly GlobalConfigController _controller;

    public GlobalConfigControllerTests()
    {
        _configServiceMock = new Mock<IGlobalConfigService>();
        _controller = new GlobalConfigController(_configServiceMock.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task GetConfig_ShouldReturnOkWithConfig()
    {
        // Arrange
        var responseDto = new GlobalConfigResponseDto(
            Id: 1,
            LastPolled: null,
            SystemEventRetentionDays: 2,
            MatViewRefreshIntervalMinutes: 60,
            VisibleRecurringJobs: Array.Empty<Adwais.Application.Common.Jobs.RecurringJobKind>()
        );

        _configServiceMock.Setup(s => s.GetConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.GetConfig();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<GlobalConfigResponseDto>(okResult.Value);
        Assert.Equal(2, returned.SystemEventRetentionDays);
        _configServiceMock.Verify(s => s.GetConfigAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConfig_ShouldReturnOkWithUpdatedConfig()
    {
        // Arrange
        var request = new UpdateGlobalConfigRequestDto(SystemEventRetentionDays: 30);
        var responseDto = new GlobalConfigResponseDto(
            Id: 1,
            LastPolled: null,
            SystemEventRetentionDays: 30,
            MatViewRefreshIntervalMinutes: 60,
            VisibleRecurringJobs: Array.Empty<Adwais.Application.Common.Jobs.RecurringJobKind>()
        );

        _configServiceMock.Setup(s => s.UpdateConfigAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(responseDto));

        // Act
        var result = await _controller.UpdateConfig(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<GlobalConfigResponseDto>(okResult.Value);
        Assert.Equal(30, returned.SystemEventRetentionDays);
        _configServiceMock.Verify(s => s.UpdateConfigAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConfig_ShouldReturnBadRequestForValidationFailure()
    {
        var request = new UpdateGlobalConfigRequestDto(MatViewRefreshIntervalMinutes: 4);
        _configServiceMock.Setup(s => s.UpdateConfigAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<GlobalConfigResponseDto>(new ValidationError(new Dictionary<string, string[]>
            {
                [nameof(request.MatViewRefreshIntervalMinutes)] = ["Interval must be at least 5 minutes."]
            })));

        var result = await _controller.UpdateConfig(request);

        var problem = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
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
