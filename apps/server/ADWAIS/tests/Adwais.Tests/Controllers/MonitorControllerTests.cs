// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Adwais.Api.Controllers;
using Adwais.Api.Controllers.Analytics;
using Adwais.Api.DTOs.Monitoring;
using Adwais.Application.DTOs.Monitoring;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Application.Common.Models;
using Adwais.Application.Services;

namespace Adwais.Tests.Controllers;

public class MonitorControllerTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly AnalyticsDbContext _dbContext;
    private readonly Mock<IMonitorOrchestrationService> _monitorServiceMock;
    private readonly Mock<IReportingCalendar> _reportingCalendarMock;
    private readonly Mock<IMonitoringProvider> _monitoringProviderMock;
    private readonly Mock<IOrderSource> _orderSourceMock;
    private readonly MonitorController _controller;

    public MonitorControllerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AnalyticsDbContext(_dbOptions);
        _monitorServiceMock = new Mock<IMonitorOrchestrationService>();
        _reportingCalendarMock = new Mock<IReportingCalendar>();
        _monitoringProviderMock = new Mock<IMonitoringProvider>();
        _monitoringProviderMock.SetupGet(provider => provider.Provider).Returns("uptimerobot");
        _monitoringProviderMock.Setup(provider => provider.IsConfigured(It.IsAny<string?>())).Returns(true);
        _orderSourceMock = new Mock<IOrderSource>();
        _orderSourceMock.SetupGet(source => source.Provider).Returns("litium");
        _orderSourceMock.Setup(source => source.GetPublicSettings(It.IsAny<string?>())).Returns(new Dictionary<string, string?>());
        _reportingCalendarMock
            .Setup(calendar => calendar.ResolvePeriodAsync(
                It.IsAny<Timeframe>(),
                It.IsAny<ComparisonType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Timeframe timeframe, ComparisonType comparison, CancellationToken _) =>
                TimeframeResolver.Resolve(timeframe, comparison));

        _controller = new MonitorController(
            _dbContext,
            _monitorServiceMock.Object,
            _reportingCalendarMock.Object,
            new[] { _monitoringProviderMock.Object },
            new[] { _orderSourceMock.Object });

        // Seed global config for IsUptimeRobotConfiguredAsync
        using var db = new AnalyticsDbContext(_dbOptions);
        db.GlobalConfigs.Add(new GlobalConfig
        {
            Id = 1,
            MonitoringProviderSettings = "{\"apiKey\":\"valid-api-key\"}",
            OrderFetchIntervalMinutes = 30
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GetAnalytics_ShouldReturnOkWithData()
    {
        // Arrange
        var request = new MonitorRequestDto
        {
            TenantId = Guid.NewGuid(),
            Timeframe = Timeframe.T30,
            Comparison = ComparisonType.Preceding,
            Tags = ["PROD"],
            ExcludedTags = ["DEV"],
            ExcludedStatuses = ["PAUSED"]
        };
        var mockResult = new MonitorAnalyticsDto(
            GlobalAverageLatency: 120.5,
            LatencyPoints: new List<LatencyPointDto>
            {
                new(
                    DateTimeOffset.UtcNow,
                    120.5,
                    115.0,
                    100.0,
                    150.0,
                    LatencySampleState.Observed,
                    LatencySampleState.Observed)
            },
            Kpis: new MonitorKpiDto(99.9, 99.8, 0.1, 120.5, 115.0, 4.7, 150.0, 145.0, 3.4, 100.0, 95.0, 5.2)
        );

        _monitorServiceMock.Setup(s => s.GetAnalyticsAsync(
                It.IsAny<ResolvedPeriod>(),
                request.TenantId,
                request.MonitorId,
                request.Tags,
                request.Statuses,
                It.IsAny<CancellationToken>(),
                request.ExcludedTags,
                request.ExcludedStatuses))
            .ReturnsAsync(mockResult);

        // Act
        var result = await _controller.GetAnalytics(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MonitorAnalyticsResponseDto>(okResult.Value);
        Assert.Equal(120.5, response.GlobalAverageLatency);
        Assert.Single(response.LatencyPoints);
    }

    [Fact]
    public async Task GetAvailability_ShouldReturnDailySeries()
    {
        var request = new MonitorRequestDto
        {
            TenantId = Guid.NewGuid(),
            Timeframe = Timeframe.T30,
            ExcludedTags = ["TEST"],
            ExcludedStatuses = ["PAUSED"]
        };
        var pointDate = new DateOnly(2026, 7, 22);
        var series = new MonitorAvailabilitySeriesDto(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            99.9,
            99.5,
            new List<MonitorAvailabilityPointDto>
            {
                new(pointDate, pointDate, 99.9, 99.5, 2, true)
            });

        _monitorServiceMock
            .Setup(service => service.GetAvailabilitySeriesAsync(
                It.IsAny<ResolvedPeriod>(),
                It.IsAny<TimeZoneInfo>(),
                request.TenantId,
                request.MonitorId,
                request.Tags,
                request.Statuses,
                It.IsAny<CancellationToken>(),
                request.ExcludedTags,
                request.ExcludedStatuses))
            .ReturnsAsync(series);

        var result = await _controller.GetAvailability(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<MonitorAvailabilitySeriesResponseDto>(okResult.Value);
        var point = Assert.Single(response.Points);
        Assert.Equal(pointDate, point.Date);
        Assert.Equal(pointDate, point.EndDate);
        Assert.Equal(99.9, point.UptimePercentage);
        Assert.True(point.IsPartial);
    }

    [Fact]
    public async Task GetMonitors_WithoutScope_ShouldUseBatchedServiceRead()
    {
        var request = new MonitorRequestDto { Timeframe = Timeframe.T30 };
        var monitors = new List<UptimeMonitor>
        {
            new() { Id = 41, TenantId = Guid.NewGuid(), Name = "One", Url = "https://one.test" },
            new() { Id = 42, TenantId = Guid.NewGuid(), Name = "Two", Url = "https://two.test" }
        };
        _monitorServiceMock
            .Setup(service => service.GetMonitorsAsync(
                It.IsAny<ResolvedPeriod>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(monitors);

        var result = await _controller.GetMonitors(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IEnumerable<UptimeMonitorDto>>(okResult.Value);
        Assert.Equal(2, response.Count());
        _monitorServiceMock.Verify(service => service.GetMonitorsAsync(
            It.IsAny<ResolvedPeriod>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        _monitorServiceMock.Verify(service => service.GetMonitorAsync(
            It.IsAny<Guid>(),
            It.IsAny<int>(),
            It.IsAny<ResolvedPeriod>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateMonitor_ShouldReturnCreated()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var request = new CreateMonitorRequestDto
        {
            Name = "New Monitor",
            Url = "https://test.com",
            Type = "PING",
            UptimeSla = 99.5
        };
        var createdMonitor = new UptimeMonitor
        {
            Id = 123,
            TenantId = tenantId,
            Type = "PING",
            Name = "New Monitor",
            Url = "https://test.com",
            UptimeSla = 99.5
        };

        _monitorServiceMock.Setup(s => s.CreateMonitorAsync(tenantId, request.Name, request.Url, request.Type, request.UptimeSla, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdMonitor);

        // Act
        var result = await _controller.CreateMonitor(tenantId, request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<UptimeMonitorDto>(createdResult.Value);
        Assert.Equal(123, response.Id);
        Assert.Equal("PING", response.Type);
        Assert.Equal("New Monitor", response.Name);
    }

    [Fact]
    public async Task AssignMonitor_ShouldReturnOk()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = await _controller.AssignMonitor(123, tenantId, CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(result);
        _monitorServiceMock.Verify(s => s.AssignMonitorAsync(123, tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PauseMonitor_ShouldReturnOk()
    {
        // Act
        var result = await _controller.PauseMonitor(123, CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(result);
        _monitorServiceMock.Verify(s => s.PauseMonitorAsync(123, It.IsAny<CancellationToken>()), Times.Once);
    }
}
