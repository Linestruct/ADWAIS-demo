// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using Xunit;

namespace Adwais.Tests.Services;

public class UptimeRobotRateLimitHandlerTests
{
    private readonly Mock<HttpMessageHandler> _innerHandlerMock;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _client;

    public UptimeRobotRateLimitHandlerTests()
    {
        _innerHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new UptimeRobotRateLimitHandler(_cache)
        {
            InnerHandler = _innerHandlerMock.Object
        };
        _client = new HttpClient(handler);
    }

    [Fact]
    public async Task SendAsync_ShouldPassThrough_WhenNoRateLimitCached()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.uptimerobot.com/v3/monitors");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("success")
        };

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        _innerHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_ShouldReturn429_WhenBudgetPreservedAndGetRequest()
    {
        // Arrange
        var resetTime = DateTimeOffset.UtcNow.AddMinutes(5);
        _cache.Set("UptimeRobotRemainingLimit", 8); // <= 10 remaining limit
        _cache.Set("UptimeRobotRateLimitReset", resetTime);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.uptimerobot.com/v3/monitors");

        // Act
        var result = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, result.StatusCode);
        Assert.Equal("Rate Limit Budget Preserved", result.ReasonPhrase);
        Assert.NotNull(result.Headers.RetryAfter);
    }

    [Fact]
    public async Task SendAsync_ShouldUpdateCacheKeys_WhenResponseHasRateLimitHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.uptimerobot.com/v3/monitors");
        var resetEpoch = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("success")
        };
        response.Headers.Add("X-RateLimit-Remaining", "2");
        response.Headers.Add("X-RateLimit-Reset", resetEpoch.ToString());

        _innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        await _client.SendAsync(request);

        // Assert
        Assert.True(_cache.TryGetValue("UptimeRobotRemainingLimit", out int remaining));
        Assert.Equal(2, remaining);
        Assert.True(_cache.TryGetValue("UptimeRobotRateLimitReset", out DateTimeOffset resetDate));
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(resetEpoch), resetDate);
    }

    [Fact]
    public async Task SendAsync_ShouldRetryRequest_WhenInitialReturns429()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.uptimerobot.com/v3/monitors");
        
        var response429 = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("limit reached")
        };
        response429.Headers.Add("Retry-After", "1");

        var responseSuccess = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("success after delay")
        };

        var sequence = _innerHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response429)
            .ReturnsAsync(responseSuccess);

        // Act
        var result = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        _innerHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}
