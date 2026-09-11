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
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Exceptions;
using Adwais.Application.Common.Errors;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using Xunit;

namespace Adwais.Tests.Services;

public class WeatherServiceTests
{
    private readonly Mock<IOrganizationConfigService> _configServiceMock;
    private readonly Mock<IMemoryCache> _cacheMock;

    public WeatherServiceTests()
    {
        _configServiceMock = new Mock<IOrganizationConfigService>();
        _cacheMock = new Mock<IMemoryCache>();
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
        mock.Setup(access => access.Scope).Returns(new AccessScope(null, null, [UserRole.PlatformAdmin]));
        return mock.Object;
    }

    private static ICurrentAccess SelectedPlatformAccess(Guid organizationId)
    {
        var mock = new Mock<ICurrentAccess>();
        mock.Setup(access => access.Scope).Returns(new AccessScope(organizationId, null, [UserRole.Admin])
        {
            IsPlatformAdmin = true
        });
        return mock.Object;
    }

    private static OrganizationConfigDto CreateConfig(string? weatherLocation) => new(
        WeatherLocation: weatherLocation,
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

    [Fact]
    public async Task GetCurrentWeatherAsync_ShouldRejectMissingConfiguredLocation()
    {
        var orgId = Guid.NewGuid();
        _configServiceMock.Setup(c => c.GetConfigAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateConfig(null));
        var service = new WeatherService(new HttpClient(), _configServiceMock.Object, _cacheMock.Object, OrgAccess(orgId));

        var result = await service.GetCurrentWeatherAsync();
        Assert.True(result.IsFailed);
        Assert.IsType<ConfigurationError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_WithPlatformScope_UsesStockholmFallback()
    {
        object? cacheEntry = null;
        _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheEntry)).Returns(false);
        _cacheMock.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(Mock.Of<ICacheEntry>());

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("geocoding-api")
                    && req.RequestUri.ToString().Contains("name=Stockholm")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    results = new[] { new { name = "Stockholm", latitude = 59.33, longitude = 18.07 } }
                }))
            });
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api.open-meteo.com/v1/forecast")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    current = new
                    {
                        temperature_2m = 18.5,
                        apparent_temperature = 17.2,
                        precipitation_probability = 10,
                        precipitation = 0.0,
                        weather_code = 0
                    }
                }))
            });

        var service = new WeatherService(new HttpClient(handlerMock.Object), _configServiceMock.Object, _cacheMock.Object, PlatformAccess());

        var result = await service.GetCurrentWeatherAsync();
        Assert.True(result.IsSuccess);
        Assert.Equal("Stockholm", result.Value.Location);
        _configServiceMock.Verify(c => c.GetConfigAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_UsesDistinctCacheKeysPerOrganization()
    {
        // Arrange
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        var configDto = CreateConfig("Karlstad");
        _configServiceMock.Setup(c => c.GetConfigAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(configDto);

        object? cacheEntry = null;
        var cacheKeys = new List<string>();
        _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheEntry)).Returns(false);
        _cacheMock.Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Callback((object key) => cacheKeys.Add(key.ToString()!))
            .Returns(Mock.Of<ICacheEntry>());

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
            .Returns((HttpRequestMessage _, CancellationToken _) => Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(geocodingResponse))
            }));
        var forecastResponse = new
        {
            current = new
            {
                temperature_2m = 18.5,
                apparent_temperature = 17.2,
                precipitation_probability = 65,
                precipitation = 0.4,
                weather_code = 1
            }
        };
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api.open-meteo.com/v1/forecast")),
                ItExpr.IsAny<CancellationToken>())
            .Returns((HttpRequestMessage _, CancellationToken _) => Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(forecastResponse))
            }));

        var httpClient = new HttpClient(handlerMock.Object);

        // Act
        var serviceA = new WeatherService(httpClient, _configServiceMock.Object, _cacheMock.Object, OrgAccess(orgA));
        var serviceB = new WeatherService(httpClient, _configServiceMock.Object, _cacheMock.Object, OrgAccess(orgB));
        await serviceA.GetCurrentWeatherAsync();
        await serviceB.GetCurrentWeatherAsync();

        // Assert
        Assert.Equal(2, cacheKeys.Count);
        Assert.Equal($"weather:current:{orgA}", cacheKeys[0]);
        Assert.Equal($"weather:current:{orgB}", cacheKeys[1]);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_ShouldFetchForecast_WhenLocationIsConfigured()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var configDto = CreateConfig("Karlstad");

        _configServiceMock.Setup(c => c.GetConfigAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(configDto);

        // Mock caching with org-keyed cache key
        object? cacheEntry = null;
        _cacheMock.Setup(c => c.TryGetValue(It.Is<object>(key => key.ToString()!.Contains(orgId.ToString())), out cacheEntry)).Returns(false);
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
        var service = new WeatherService(httpClient, _configServiceMock.Object, _cacheMock.Object, SelectedPlatformAccess(orgId));

        // Act
        var result = await service.GetCurrentWeatherAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Karlstad", result.Value.Location);
        Assert.Equal(18.5, result.Value.Temperature);
        Assert.Equal(17.2, result.Value.ApparentTemperature);
        Assert.Equal(65, result.Value.PrecipitationProbability);
        Assert.Equal(0.4, result.Value.Precipitation);
        Assert.Equal(1, result.Value.WeatherCode);
    }
}
