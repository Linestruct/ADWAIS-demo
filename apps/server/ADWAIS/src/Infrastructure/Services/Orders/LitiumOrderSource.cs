// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Text.Json;
using Adwais.Application.DTOs.Financial.Upstream;
using Adwais.Application.DTOs.Integrations;
using Adwais.Application.Interfaces;
using Adwais.Domain.Enums;

namespace Adwais.Infrastructure.Services;

public sealed class LitiumOrderSource(HttpClient httpClient) : IOrderSource
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string Provider => "litium";
    public ProviderDescriptor Configuration { get; } = new(
        "litium",
        "Litium",
        [
            new("endpointUrl", "Endpoint URL", "url", true, "https://example.com"),
            new("authorization", "Authorization", "password", true, "ServiceAccount …")
        ]);

    public bool IsConfigured(string? settings)
    {
        try
        {
            var parsed = ParseSettings(settings);
            return !string.IsNullOrWhiteSpace(parsed.EndpointUrl) && !string.IsNullOrWhiteSpace(parsed.Authorization);
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
            return string.IsNullOrWhiteSpace(ParseSettings(settings).Authorization) ? [] : ["authorization"];
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
            if (key is not ("endpointUrl" or "authorization"))
                throw new ArgumentException($"'{key}' is not a valid Litium setting.", nameof(updates));
        }

        if (updates.TryGetValue("authorization", out var authorization) && authorization == "configured")
            throw new ArgumentException("'configured' is a display value, not a valid authorization update.", nameof(updates));

        var current = ParseSettings(currentSettings);
        var endpointUrl = updates.TryGetValue("endpointUrl", out var endpoint) ? Normalize(endpoint) : current.EndpointUrl;
        var updatedAuthorization = updates.TryGetValue("authorization", out var auth) ? Normalize(auth) : current.Authorization;
        return JsonSerializer.Serialize(new LitiumSettings(endpointUrl, updatedAuthorization));
    }

    public async Task<IReadOnlyList<OrderSourceOrder>> FetchOrdersAsync(
        string settings,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int take,
        CancellationToken ct = default)
    {
        var sourceSettings = ParseSettings(settings);
        if (string.IsNullOrWhiteSpace(sourceSettings.EndpointUrl) || string.IsNullOrWhiteSpace(sourceSettings.Authorization))
            throw new InvalidOperationException("Litium provider settings require endpointUrl and authorization.");

        var since = Uri.EscapeDataString(startDate.ToString("O"));
        var until = Uri.EscapeDataString(endDate.ToString("O"));
        var url = $"{sourceSettings.EndpointUrl.TrimEnd('/')}/api/motasticadapter/sync?since={since}&until={until}&skip=0&take={take}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", sourceSettings.Authorization);

        using var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to fetch chunk. Status: {response.StatusCode}");
        }

        await using var content = await response.Content.ReadAsStreamAsync(ct);
        var payload = await JsonSerializer.DeserializeAsync<LitiumSyncResponse>(content, JsonOptions, ct);
        return payload?.Orders?
            .Where(order => order is not null)
            .Select(order => Normalize(order!))
            .ToList() ?? [];
    }

    public static OrderSourceOrder Normalize(LitiumSyncResponse.LitiumOrderDto order)
    {
        if (!Enum.TryParse<OrderState>(order.OrderStatus, true, out var state))
        {
            state = OrderState.Unknown;
        }

        return new OrderSourceOrder(
            order.Id.ToString("D"),
            order.OrderNumber,
            order.CreatedDate.ToUniversalTime(),
            state,
            order.TotalValueIncludingVat,
            order.TotalValueExcludingVat,
            order.Currency ?? "UNK");
    }

    private static LitiumSettings ParseSettings(string? settings)
        => string.IsNullOrWhiteSpace(settings)
            ? new LitiumSettings(null, null)
            : JsonSerializer.Deserialize<LitiumSettings>(settings, JsonOptions) ?? new LitiumSettings(null, null);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record LitiumSettings(string? EndpointUrl, string? Authorization);
}
