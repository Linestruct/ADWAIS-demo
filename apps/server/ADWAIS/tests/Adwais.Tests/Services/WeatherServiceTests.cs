// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using Xunit;

namespace Adwais.Tests.Services;

public class WeatherServiceTests
{
    private readonly Mock<IGlobalConfigService> _configServiceMock;
    private readonly Mock<IMemoryCache> _cacheMock;

    public WeatherServiceTests()
    {
        _configServiceMock = new Mock<IGlobalConfigService>();
        _cacheMock = new Mock<IMemoryCache>();
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_ShouldRejectMissingConfiguredLocation()
    {
        _configServiceMock.Setup(c => c.GetConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateConfig(null));
        var service = new WeatherService(new HttpClient(), _configServiceMock.Object, _cacheMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetCurrentWeatherAsync());
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_ShouldFetchForecast_WhenLocationIsConfigured()
    {
        // Arrange
        var configDto = CreateConfig("Karlstad");

        _configServiceMock.Setup(c => c.GetConfigAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configDto);

        // Mock caching
        object? cacheEntry = null;
        _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheEntry)).Returns(false);
        _cacheMock.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        // Mock HttpClient calls
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        
        var geocodingResponse = new
        {
            results = new[] { new { name = "Karlstad", latitude = 59.4, longitude = 13.5 } }
        };
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("geocoding-api")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(geocodingResponse))
            });

        // Mock forecast response
        var forecastResponse = new
        {
            current = new
            {
                temperature_2m = 18.5,
                apparent_temperature = 17.2,
                precipitation_probability = 65,
                precipitation = 0.4,
                weather_code = 1,
            }
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("latitude=59.4&longitude=13.5") &&
                    req.RequestUri.ToString().Contains("temperature_2m,apparent_temperature,precipitation_probability,precipitation,weather_code") &&
                    req.RequestUri.ToString().Contains("timezone=UTC")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(forecastResponse))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var service = new WeatherService(httpClient, _configServiceMock.Object, _cacheMock.Object);

        // Act
        var result = await service.GetCurrentWeatherAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Karlstad", result.Location);
        Assert.Equal(18.5, result.Temperature);
        Assert.Equal(17.2, result.ApparentTemperature);
        Assert.Equal(65, result.PrecipitationProbability);
        Assert.Equal(0.4, result.Precipitation);
        Assert.Equal(1, result.WeatherCode);
    }

    private static GlobalConfigResponseDto CreateConfig(string? weatherLocation) => new(
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
        MonitorsCount: 0,
        MonitorsLimit: 100,
        ActiveSubscription: null,
        FeedFetchIntervalHours: 2,
        WeatherLocation: weatherLocation,
        WeatherFetchIntervalMinutes: 15,
        ReportingTimeZoneId: "Europe/Stockholm"
    );
}
