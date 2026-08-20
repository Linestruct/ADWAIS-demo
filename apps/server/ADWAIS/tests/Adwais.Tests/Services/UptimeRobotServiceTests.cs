// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using Xunit;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;

namespace Adwais.Tests.Services;

public class UptimeRobotServiceTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly Mock<IDbContextFactory<AnalyticsDbContext>> _dbContextFactoryMock;
    private readonly Mock<ISystemEventService> _eventServiceMock;
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _client;
    private readonly UptimeRobotService _service;

    public UptimeRobotServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContextFactoryMock = new Mock<IDbContextFactory<AnalyticsDbContext>>();
        _dbContextFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AnalyticsDbContext(_dbOptions));

        _eventServiceMock = new Mock<ISystemEventService>();
        _httpHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _client = new HttpClient(_httpHandlerMock.Object);

        _service = new UptimeRobotService(_client, _dbContextFactoryMock.Object, _eventServiceMock.Object);

        // Seed API key config
        using var db = new AnalyticsDbContext(_dbOptions);
        db.GlobalConfigs.Add(new GlobalConfig
        {
            Id = 1,
            MonitoringProviderSettings = "{\"apiKey\":\"test-api-key\"}",
            OrderFetchIntervalMinutes = 30
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task CreateMonitorAsync_ShouldSendPostRequest_AndReturnParsedMonitor()
    {
        // Arrange
        var monitorJson = @"
        {
            ""id"": 12345,
            ""type"": ""PING"",
            ""friendlyName"": ""Test Monitor"",
            ""url"": ""https://test.com"",
            ""status"": ""paused"",
            ""createDateTime"": ""2026-07-06T12:00:00Z"",
            ""interval"": 300
        }";

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(monitorJson)
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString() == "https://api.uptimerobot.com/v3/monitors"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.CreateMonitorAsync("Test Monitor", "https://test.com", "ping");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("12345", result.ExternalId);
        Assert.Equal("PING", result.Type);
        Assert.Equal("Test Monitor", result.Name);
        Assert.Equal("https://test.com", result.Url);
        Assert.Equal("paused", result.Status);
    }

    [Fact]
    public async Task GetResponseTimeAsync_ShouldQueryEndpoint_AndReturnStats()
    {
        // Arrange
        var responseTimeJson = @"
        {
            ""summary"": {
                ""avg"": 150,
                ""min"": 110,
                ""max"": 320
            }
        }";

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseTimeJson)
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("/stats/response-time")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var (avg, lowest, highest) = await _service.GetResponseTimeAsync(
            "12345",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            "Test Monitor"
        );

        // Assert
        Assert.Equal(150, avg);
        Assert.Equal(110, lowest);
        Assert.Equal(320, highest);
    }

    [Fact]
    public async Task UpdateMonitorAsync_ShouldSendNormalizedType()
    {
        string? requestBody = null;
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(request =>
                    request.Method == HttpMethod.Patch &&
                    request.RequestUri != null &&
                    request.RequestUri.ToString() == "https://api.uptimerobot.com/v3/monitors/12345"),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())
            .ReturnsAsync(mockResponse);

        await _service.UpdateMonitorAsync("12345", null, null, "ping", null);

        Assert.NotNull(requestBody);
        using var payload = JsonDocument.Parse(requestBody);
        Assert.Equal("PING", payload.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task GetUptimeAsync_ShouldQueryEndpoint_AndReturnUptimeValue()
    {
        // Arrange
        var uptimeJson = @"
        {
            ""uptime"": 99.95
        }";

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(uptimeJson)
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains("/stats/uptime")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _service.GetUptimeAsync("12345");

        // Assert
        Assert.Equal(99.95, result);
    }

    [Fact]
    public async Task GetResponseAsync_ShouldLogErrorAndThrow_WhenRequestFails()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Invalid parameters")
        };

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.CreateMonitorAsync("Fail Monitor", "https://fail.com", null));

        _eventServiceMock.Verify(s => s.LogErrorAsync(
            nameof(UptimeRobotService),
            It.Is<string>(m => m.Contains("UptimeRobot request failed")),
            It.IsAny<Exception>()
        ), Times.Once);
    }

    [Fact]
    public async Task GetMonitorsAsync_ShouldQueryNextPages_WhenNextLinkPresent()
    {
        // Arrange
        var page1Json = @"
        {
            ""nextLink"": ""/v3/monitors?cursor=page2"",
            ""data"": [
                {
                    ""id"": 1,
                    ""type"": ""HTTP"",
                    ""friendlyName"": ""Monitor 1"",
                    ""url"": ""https://test1.com"",
                    ""status"": ""up"",
                    ""createDateTime"": ""2026-07-06T12:00:00Z"",
                    ""interval"": 300,
                    ""httpMethodType"": ""HEAD"",
                    ""timeout"": 30,
                    ""sslExpiryDateTime"": ""2027-01-10T10:00:00Z"",
                    ""domainExpireDate"": ""2027-02-11T00:00:00Z"",
                    ""currentStateDuration"": 810490,
                    ""regionalData"": { ""REGION"": [""eu"", ""na""] },
                    ""lastIncident"": {
                        ""id"": ""incident-1"",
                        ""status"": ""Resolved"",
                        ""cause"": 500,
                        ""reason"": ""500 Internal Server Error"",
                        ""startedAt"": ""2026-07-13T06:24:56.690Z"",
                        ""duration"": 6105
                    }
                }
            ]
        }";

        var page2Json = @"
        {
            ""nextLink"": null,
            ""data"": [
                {
                    ""id"": 2,
                    ""type"": ""PING"",
                    ""friendlyName"": ""Monitor 2"",
                    ""url"": ""https://test2.com"",
                    ""status"": ""paused"",
                    ""createDateTime"": ""2026-07-06T12:00:00Z"",
                    ""interval"": 300
                }
            ]
        }";

        var responsePage1 = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(page1Json) };
        var responsePage2 = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(page2Json) };

        _httpHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responsePage1)
            .ReturnsAsync(responsePage2);

        // Act
        var result = await _service.GetMonitorsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("1", result[0].ExternalId);
        Assert.Equal("HTTP", result[0].Type);
        Assert.Equal("Monitor 1", result[0].Name);
        Assert.Equal("HEAD", result[0].HttpMethod);
        Assert.Equal(30, result[0].TimeoutSeconds);
        Assert.Equal(new[] { "eu", "na" }, result[0].MonitoredRegions);
        Assert.Equal(810490, result[0].CurrentStateDurationSeconds);
        Assert.Equal("500", result[0].LastIncident?.Cause);
        Assert.Equal("500 Internal Server Error", result[0].LastIncident?.Reason);
        Assert.Equal(6105, result[0].LastIncident?.DurationSeconds);
        Assert.Equal("2", result[1].ExternalId);
        Assert.Equal("PING", result[1].Type);
        Assert.Equal("Monitor 2", result[1].Name);

        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetResponseAsync_ShouldIncludeAuthorizationBearerToken()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockResponse);

        // Act
        await _service.DeleteMonitorAsync("123");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.Headers.Authorization);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization.Scheme);
        Assert.Equal("test-api-key", capturedRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetMonitorsAsync_WithMonitorIds_ShouldRequestExactQueryParameters()
    {
        // Arrange
        var responseJson = @"{ ""data"": [] }";
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson)
        };

        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockResponse);

        // Act
        await _service.GetMonitorsAsync(new[] { "7", "11" });

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal("https://api.uptimerobot.com/v3/monitors?limit=200&monitors=7-11", capturedRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task DeleteMonitorAsync_ShouldUseCorrectHttpMethodAndUrl()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockResponse);

        // Act
        await _service.DeleteMonitorAsync("999");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Delete, capturedRequest.Method);
        Assert.Equal("https://api.uptimerobot.com/v3/monitors/999", capturedRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task PauseMonitorAsync_ShouldUseCorrectHttpMethodAndUrl()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockResponse);

        // Act
        await _service.PauseMonitorAsync("999");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("https://api.uptimerobot.com/v3/monitors/999/pause", capturedRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task StartMonitorAsync_ShouldUseCorrectHttpMethodAndUrl()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        };

        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(mockResponse);

        // Act
        await _service.StartMonitorAsync("999");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("https://api.uptimerobot.com/v3/monitors/999/start", capturedRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task UpdateMonitorAsync_WhenAllFieldsNull_ShouldNotSendRequest()
    {
        // Act
        await _service.UpdateMonitorAsync("12345", null, null, null, null);

        // Assert
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DeleteMonitorAsync_WithInvalidExternalId_ShouldRejectBeforeSending()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteMonitorAsync("not-an-integer"));

        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public void MergeSettings_WithRedactedApiKey_RejectsDisplayValue()
    {
        const string settings = "{\"apiKey\":\"actual-key\"}";

        Assert.Empty(_service.GetPublicSettings(settings));
        Assert.Contains("apiKey", _service.GetConfiguredSecretKeys(settings));
        Assert.Throws<ArgumentException>(() => _service.MergeSettings(
            settings,
            new Dictionary<string, string?> { ["apiKey"] = "configured" }));
    }

    [Fact]
    public void MergeSettings_NullApiKeyExplicitlyClearsIt()
    {
        const string settings = "{\"apiKey\":\"actual-key\"}";

        var cleared = _service.MergeSettings(settings, new Dictionary<string, string?> { ["apiKey"] = null });

        Assert.Empty(_service.GetConfiguredSecretKeys(cleared));
        Assert.False(_service.IsConfigured(cleared));
    }
}
