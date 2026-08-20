// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Adwais.Application.DTOs.Financial.Upstream;
using Adwais.Application.DTOs.Integrations;
using Adwais.Application.Interfaces;
using Adwais.Domain.Enums;

namespace Adwais.Infrastructure.Services;

public sealed class ShopifyOrderSource(HttpClient httpClient) : IOrderSource
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string ApiVersion = "2026-07";

    public string Provider => "shopify";
    public ProviderDescriptor Configuration { get; } = new(
        "shopify",
        "Shopify",
        [
            new("endpointUrl", "Store URL", "url", true, "https://your-store.myshopify.com"),
            new("accessToken", "Admin API token", "password", true, "shpat_…")
        ]);

    public bool IsConfigured(string? settings)
    {
        try
        {
            var parsed = ParseSettings(settings);
            return !string.IsNullOrWhiteSpace(parsed.EndpointUrl) && !string.IsNullOrWhiteSpace(parsed.AccessToken);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public IReadOnlyDictionary<string, string?> GetPublicSettings(string? settings)
    {
        try
        {
            var parsed = ParseSettings(settings);
            return new Dictionary<string, string?>
            {
                ["endpointUrl"] = parsed.EndpointUrl
            };
        }
        catch (JsonException)
        {
            return new Dictionary<string, string?>();
        }
    }

    public IReadOnlyCollection<string> GetConfiguredSecretKeys(string? settings)
    {
        try
        {
            return string.IsNullOrWhiteSpace(ParseSettings(settings).AccessToken) ? [] : ["accessToken"];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public string MergeSettings(string? currentSettings, IReadOnlyDictionary<string, string?> updates)
    {
        foreach (var key in updates.Keys)
        {
            if (key is not ("endpointUrl" or "accessToken"))
                throw new ArgumentException($"'{key}' is not a valid Shopify setting.", nameof(updates));
        }

        if (updates.TryGetValue("accessToken", out var accessToken) && accessToken == "configured")
            throw new ArgumentException("'configured' is a display value, not a valid access token update.", nameof(updates));

        var current = ParseSettings(currentSettings);
        var endpointUrl = updates.TryGetValue("endpointUrl", out var endpoint) ? Normalize(endpoint) : current.EndpointUrl;
        var updatedAccessToken = updates.TryGetValue("accessToken", out var token) ? Normalize(token) : current.AccessToken;
        return JsonSerializer.Serialize(new ShopifySettings(endpointUrl, updatedAccessToken));
    }

    public async Task<IReadOnlyList<OrderSourceOrder>> FetchOrdersAsync(
        string settings,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int take,
        CancellationToken ct = default)
    {
        var sourceSettings = ParseSettings(settings);
        if (string.IsNullOrWhiteSpace(sourceSettings.EndpointUrl) || string.IsNullOrWhiteSpace(sourceSettings.AccessToken))
            throw new InvalidOperationException("Shopify provider settings require endpointUrl and accessToken.");

        var orders = new List<OrderSourceOrder>();
        var nextUrl = BuildOrdersUrl(sourceSettings.EndpointUrl, startDate, endDate, take);

        while (!string.IsNullOrWhiteSpace(nextUrl) && orders.Count < take)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
            request.Headers.Add("X-Shopify-Access-Token", sourceSettings.AccessToken);

            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to fetch chunk. Status: {response.StatusCode}");
            }

            await using var content = await response.Content.ReadAsStreamAsync(ct);
            var payload = await JsonSerializer.DeserializeAsync<ShopifyOrdersResponse>(content, JsonOptions, ct);
            if (payload?.Orders is not null)
            {
                foreach (var order in payload.Orders)
                {
                    if (order is null) continue;
                    orders.Add(Normalize(order));
                    if (orders.Count >= take) break;
                }
            }

            nextUrl = ExtractNextLink(response.Headers) is { } link ? ResolveLink(sourceSettings.EndpointUrl, link) : null;
        }

        return orders;
    }

    public static OrderSourceOrder Normalize(ShopifyOrdersResponse.ShopifyOrderDto order)
    {
        var totalIncVat = TryParseDecimal(order.CurrentTotalPrice);
        var totalTax = TryParseDecimal(order.CurrentTotalTax);

        return new OrderSourceOrder(
            order.Id.ToString(),
            order.Name ?? order.Id.ToString(),
            ParseCreatedAt(order.CreatedAt),
            MapState(order),
            totalIncVat,
            totalIncVat is not null && totalTax is not null ? totalIncVat - totalTax : null,
            order.Currency ?? order.PresentmentCurrency ?? "UNK");
    }

    private static OrderState MapState(ShopifyOrdersResponse.ShopifyOrderDto order)
    {
        if (string.Equals(order.FinancialStatus, "voided", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(order.CancelledAt))
            return OrderState.Cancelled;
        if (string.Equals(order.FinancialStatus, "paid", StringComparison.OrdinalIgnoreCase)
            && string.Equals(order.FulfillmentStatus, "fulfilled", StringComparison.OrdinalIgnoreCase))
            return OrderState.Completed;

        return order.FinancialStatus?.ToLowerInvariant() switch
        {
            "paid" => OrderState.Confirmed,
            "partially_paid" or "partially_refunded" => OrderState.Processing,
            "authorized" or "pending" => OrderState.PendingProcessing,
            "refunded" => OrderState.Cancelled,
            _ => OrderState.Unknown
        };
    }

    private static string BuildOrdersUrl(string endpointUrl, DateTimeOffset startDate, DateTimeOffset endDate, int take)
    {
        var limit = Math.Min(take, 250);
        var query = string.Join("&",
            "status=any",
            $"created_at_min={Uri.EscapeDataString(startDate.UtcDateTime.ToString("O"))}",
            $"created_at_max={Uri.EscapeDataString(endDate.UtcDateTime.ToString("O"))}",
            $"limit={limit}");
        return $"{endpointUrl.TrimEnd('/')}/admin/api/{ApiVersion}/orders.json?{query}";
    }

    private static string? ExtractNextLink(HttpResponseHeaders headers)
    {
        if (!headers.TryGetValues("Link", out var values))
            return null;

        foreach (var value in values)
        {
            foreach (var part in value.Split(','))
            {
                var urlStart = part.IndexOf('<');
                var urlEnd = part.IndexOf('>');
                if (urlStart < 0 || urlEnd <= urlStart)
                    continue;

                var relIndex = part.IndexOf("rel=", StringComparison.OrdinalIgnoreCase);
                if (relIndex < 0)
                    continue;
                if (!part[(relIndex + 4)..].Contains("next", StringComparison.OrdinalIgnoreCase))
                    continue;

                return part[(urlStart + 1)..urlEnd];
            }
        }

        return null;
    }

    private static string? ResolveLink(string endpointUrl, string link)
        => link.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? link
            : $"{endpointUrl.TrimEnd('/')}{link}";

    private static decimal? TryParseDecimal(string? value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static DateTimeOffset ParseCreatedAt(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : default;

    private static ShopifySettings ParseSettings(string? settings)
        => string.IsNullOrWhiteSpace(settings)
            ? new ShopifySettings(null, null)
            : JsonSerializer.Deserialize<ShopifySettings>(settings, JsonOptions) ?? new ShopifySettings(null, null);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record ShopifySettings(string? EndpointUrl, string? AccessToken);

    public class ShopifyOrdersResponse
    {
        public List<ShopifyOrderDto?>? Orders { get; set; } = new();

        public class ShopifyOrderDto
        {
            public long Id { get; set; }
            public string? Name { get; set; }
            [JsonPropertyName("created_at")]
            public string? CreatedAt { get; set; }
            [JsonPropertyName("financial_status")]
            public string? FinancialStatus { get; set; }
            [JsonPropertyName("fulfillment_status")]
            public string? FulfillmentStatus { get; set; }
            [JsonPropertyName("cancelled_at")]
            public string? CancelledAt { get; set; }
            [JsonPropertyName("current_total_price")]
            public string? CurrentTotalPrice { get; set; }
            [JsonPropertyName("current_total_tax")]
            public string? CurrentTotalTax { get; set; }
            public string? Currency { get; set; }
            [JsonPropertyName("presentment_currency")]
            public string? PresentmentCurrency { get; set; }
        }
    }
}
