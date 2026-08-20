// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Services;
using Moq;
using Moq.Protected;
using Xunit;

namespace Adwais.Tests.Services;

public class ShopifyOrderSourceTests
{
    private const string SettingsJson =
        "{\"endpointUrl\":\"https://shop.example.com\",\"accessToken\":\"shpat_test123\"}";

    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly ShopifyOrderSource _service;

    public ShopifyOrderSourceTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _service = new ShopifyOrderSource(new HttpClient(_httpHandlerMock.Object));
    }

    [Fact]
    public void Provider_IsShopify()
    {
        Assert.Equal("shopify", _service.Provider);
        Assert.Equal("Shopify", _service.Configuration.DisplayName);
        Assert.Collection(_service.Configuration.Settings,
            setting => Assert.Equal("endpointUrl", setting.Key),
            setting => Assert.Equal("accessToken", setting.Key));
    }

    [Fact]
    public void IsConfigured_RequiresEndpointUrlAndAccessToken()
    {
        Assert.True(_service.IsConfigured(SettingsJson));
        Assert.False(_service.IsConfigured("{\"endpointUrl\":\"https://shop.example.com\"}"));
        Assert.False(_service.IsConfigured("{\"accessToken\":\"shpat_test123\"}"));
        Assert.False(_service.IsConfigured("not-json"));
        Assert.False(_service.IsConfigured(null));
    }

    [Fact]
    public void GetPublicSettings_OnlyExposesEndpointUrl()
    {
        var publicSettings = _service.GetPublicSettings(SettingsJson);

        Assert.Equal("https://shop.example.com", publicSettings["endpointUrl"]);
        Assert.False(publicSettings.ContainsKey("accessToken"));
    }

    [Fact]
    public void GetConfiguredSecretKeys_ReportsAccessTokenWhenSet()
    {
        Assert.Contains("accessToken", _service.GetConfiguredSecretKeys(SettingsJson));
        Assert.Empty(_service.GetConfiguredSecretKeys("{\"endpointUrl\":\"https://shop.example.com\"}"));
    }

    [Fact]
    public void MergeSettings_WithConfiguredToken_RejectsDisplayValue()
    {
        Assert.Throws<ArgumentException>(() => _service.MergeSettings(
            SettingsJson,
            new Dictionary<string, string?> { ["accessToken"] = "configured" }));
    }

    [Fact]
    public void MergeSettings_NullAccessTokenExplicitlyClearsIt()
    {
        var cleared = _service.MergeSettings(SettingsJson, new Dictionary<string, string?> { ["accessToken"] = null });

        Assert.Empty(_service.GetConfiguredSecretKeys(cleared));
        Assert.False(_service.IsConfigured(cleared));
        Assert.Equal("https://shop.example.com", _service.GetPublicSettings(cleared)["endpointUrl"]);
    }

    [Fact]
    public void MergeSettings_RejectsUnknownKey()
    {
        Assert.Throws<ArgumentException>(() => _service.MergeSettings(
            SettingsJson,
            new Dictionary<string, string?> { ["apiKey"] = "nope" }));
    }

    [Fact]
    public async Task FetchOrdersAsync_SendsExpectedUrlAndAccessTokenHeader()
    {
        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"orders\":[]}")
            });

        var startDate = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2026, 8, 31, 23, 59, 59, TimeSpan.Zero);
        await _service.FetchOrdersAsync(SettingsJson, startDate, endDate, 500);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal("https://shop.example.com/admin/api/2026-07/orders.json", capturedRequest.RequestUri!.GetLeftPart(UriPartial.Path));
        Assert.Contains("status=any", capturedRequest.RequestUri.Query);
        Assert.Contains($"created_at_min={Uri.EscapeDataString(startDate.UtcDateTime.ToString("O"))}", capturedRequest.RequestUri.Query);
        Assert.Contains($"created_at_max={Uri.EscapeDataString(endDate.UtcDateTime.ToString("O"))}", capturedRequest.RequestUri.Query);
        Assert.Contains("limit=250", capturedRequest.RequestUri.Query);
        Assert.True(capturedRequest.Headers.TryGetValues("X-Shopify-Access-Token", out var values));
        Assert.Equal("shpat_test123", Assert.Single(values!));
    }

    [Fact]
    public void Normalize_PartiallyRefundedOrder_StaysCountedWithUpdatedValue()
    {
        var order = new ShopifyOrderSource.ShopifyOrdersResponse.ShopifyOrderDto
        {
            Id = 1004,
            Name = "#1004",
            CreatedAt = "2026-08-10T08:00:00Z",
            FinancialStatus = "partially_refunded",
            FulfillmentStatus = "fulfilled",
            CurrentTotalPrice = "150.00",
            CurrentTotalTax = "30.00",
            Currency = "SEK"
        };

        var normalized = ShopifyOrderSource.Normalize(order);

        Assert.Equal(OrderState.Processing, normalized.State);
        Assert.Equal(150.00m, normalized.TotalValueIncludingVat);
        Assert.Equal(120.00m, normalized.TotalValueExcludingVat);
    }

    [Fact]
    public async Task FetchOrdersAsync_ParsesOrdersAndMapsState()
    {
        var ordersJson = """
        {
            "orders": [
                {
                    "id": 1001,
                    "name": "#1001",
                    "created_at": "2026-08-13T10:00:00-04:00",
                    "financial_status": "paid",
                    "fulfillment_status": "fulfilled",
                    "current_total_price": "199.00",
                    "current_total_tax": "39.80",
                    "currency": "SEK",
                    "presentment_currency": "USD"
                },
                {
                    "id": 1002,
                    "name": "#1002",
                    "created_at": "2026-08-12T09:30:00Z",
                    "financial_status": "pending",
                    "fulfillment_status": null,
                    "current_total_price": "49.50",
                    "current_total_tax": "0.00",
                    "currency": "SEK"
                },
                {
                    "id": 1003,
                    "name": "#1003",
                    "created_at": "2026-08-11T08:00:00Z",
                    "financial_status": "voided",
                    "fulfillment_status": null,
                    "current_total_price": "10.00",
                    "current_total_tax": "2.00",
                    "currency": "SEK"
                }
            ]
        }
        """;

        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ordersJson) });

        var result = await _service.FetchOrdersAsync(
            SettingsJson,
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero),
            500);

        Assert.Equal(3, result.Count);

        Assert.Equal("1001", result[0].ExternalId);
        Assert.Equal("#1001", result[0].OrderNumber);
        Assert.Equal(new DateTimeOffset(2026, 8, 13, 14, 0, 0, TimeSpan.Zero), result[0].CreatedDate);
        Assert.Equal(OrderState.Completed, result[0].State);
        Assert.Equal(199.00m, result[0].TotalValueIncludingVat);
        Assert.Equal(159.20m, result[0].TotalValueExcludingVat);
        Assert.Equal("SEK", result[0].Currency);

        Assert.Equal(OrderState.PendingProcessing, result[1].State);
        Assert.Equal("SEK", result[1].Currency);

        Assert.Equal(OrderState.Cancelled, result[2].State);
        Assert.Equal(10.00m, result[2].TotalValueIncludingVat);
        Assert.Equal(8.00m, result[2].TotalValueExcludingVat);
    }

    [Fact]
    public async Task FetchOrdersAsync_FollowsNextLinkUntilTakeIsReached()
    {
        var page1Json = """
        { "orders": [ { "id": 1, "created_at": "2026-08-01T00:00:00Z", "financial_status": "paid" } ] }
        """;
        var page2Json = """
        { "orders": [ { "id": 2, "created_at": "2026-08-02T00:00:00Z", "financial_status": "pending" } ] }
        """;

        var page1 = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(page1Json) };
        page1.Headers.Add("Link", "<https://shop.example.com/admin/api/2026-07/orders.json?page_info=abc123>; rel=next");
        var page2 = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(page2Json) };

        var responses = new Queue<HttpResponseMessage>(new[] { page1, page2 });
        var requestedUris = new List<string>();
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => requestedUris.Add(req.RequestUri!.ToString()))
            .Returns(() => Task.FromResult(responses.Dequeue()));

        var result = await _service.FetchOrdersAsync(
            SettingsJson,
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero),
            500);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, requestedUris.Count);
        Assert.Equal("https://shop.example.com/admin/api/2026-07/orders.json?page_info=abc123", requestedUris[1]);
    }

    [Fact]
    public async Task FetchOrdersAsync_StopsRequestingOnceTakeIsReached()
    {
        var page1Json = """
        { "orders": [ { "id": 1, "created_at": "2026-08-01T00:00:00Z", "financial_status": "paid" }, { "id": 2, "created_at": "2026-08-02T00:00:00Z", "financial_status": "paid" } ] }
        """;
        var page2Json = """
        { "orders": [ { "id": 3, "created_at": "2026-08-03T00:00:00Z", "financial_status": "paid" } ] }
        """;

        var page1 = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(page1Json) };
        page1.Headers.Add("Link", "<https://shop.example.com/admin/api/2026-07/orders.json?page_info=abc123>; rel=next");
        var page2 = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(page2Json) };

        var responses = new Queue<HttpResponseMessage>(new[] { page1, page2 });
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(() => Task.FromResult(responses.Dequeue()));

        var result = await _service.FetchOrdersAsync(
            SettingsJson,
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero),
            3);

        Assert.Equal(3, result.Count);
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task FetchOrdersAsync_ThrowsOnNonSuccessResponse()
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("Invalid token") });

        await Assert.ThrowsAsync<HttpRequestException>(() => _service.FetchOrdersAsync(
            SettingsJson,
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero),
            500));
    }

    [Fact]
    public async Task FetchOrdersAsync_ThrowsWhenSettingsIncomplete()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.FetchOrdersAsync(
            "{\"endpointUrl\":\"https://shop.example.com\"}",
            new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero),
            500));
    }
}
