// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Adwais.Application.DTOs.Monitoring.Upstream;
using Adwais.Application.DTOs.Integrations;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services;

public class UptimeRobotService(
    HttpClient httpClient, 
    IDbContextFactory<AnalyticsDbContext> contextFactory,
    ISystemEventService eventService) : IMonitoringProvider
{
    public string Provider => "uptimerobot";
    public ProviderDescriptor Configuration { get; } = new(
        "uptimerobot",
        "UptimeRobot",
        [new("apiKey", "API Key", "password", true)]);

    public bool IsConfigured(string? settings)
    {
        try { return !string.IsNullOrWhiteSpace(ParseSettings(settings).ApiKey); }
        catch (JsonException) { return false; }
    }

    public IReadOnlyDictionary<string, string?> GetPublicSettings(string? settings)
    {
        try
        {
            return new Dictionary<string, string?>
            {};
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
            return string.IsNullOrWhiteSpace(ParseSettings(settings).ApiKey) ? [] : ["apiKey"];
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
            if (key != "apiKey") throw new ArgumentException($"'{key}' is not a valid UptimeRobot setting.", nameof(updates));
        }

        if (updates.TryGetValue("apiKey", out var apiKey) && apiKey == "configured")
            throw new ArgumentException("'configured' is a display value, not a valid API key update.", nameof(updates));

        var current = ParseSettings(currentSettings);
        var updatedApiKey = updates.TryGetValue("apiKey", out var updated) ? Normalize(updated) : current.ApiKey;
        return JsonSerializer.Serialize(new UptimeRobotSettings(updatedApiKey));
    }

    private async Task<string> GetApiKeyAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();
        var config = await context.GlobalConfigs.SingleOrDefaultAsync();
        if (config == null || !IsConfigured(config.MonitoringProviderSettings))
        {
            throw new InvalidOperationException("UptimeRobot provider settings require apiKey.");
        }
        return ParseSettings(config.MonitoringProviderSettings).ApiKey!;
    }

    private async Task<JsonDocument> GetResponseAsync(HttpRequestMessage request, string? context = null)
    {
        var apiKey = await GetApiKeyAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        
        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var msg = $"UptimeRobot request failed with HTTP {(int)response.StatusCode}";
            if (!string.IsNullOrEmpty(context)) msg += $" ({context})";
            
            await eventService.LogErrorAsync(nameof(UptimeRobotService), msg, new Exception(responseContent));
            throw new HttpRequestException($"{msg}: {responseContent}");
        }
        return JsonDocument.Parse(responseContent);
    }

    public async Task<MonitoringProviderMonitor> CreateMonitorAsync(string name, string url, string? type)
    {
        var normalizedType = UptimeMonitorTypes.Normalize(type);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.uptimerobot.com/v3/monitors");
        request.Content = JsonContent.Create(new { friendlyName = name, url, type = normalizedType, interval = 300, timeout = 60 });
        using var response = await GetResponseAsync(request, $"Create: {name}");
        
        var monitor = new MonitoringProviderMonitor(
            ExternalId: response.RootElement.GetProperty("id").GetInt32().ToString(CultureInfo.InvariantCulture),
            Type: ParseType(response.RootElement, normalizedType),
            Name: response.RootElement.GetProperty("friendlyName").GetString()!,
            Url: response.RootElement.GetProperty("url").GetString()!,
            Status: response.RootElement.GetProperty("status").GetString()!,
            CreatedDate: response.RootElement.GetProperty("createDateTime").GetDateTimeOffset(),
            UpdateInterval: response.RootElement.GetProperty("interval").GetInt32(),
            Tags: ParseTags(response.RootElement),
            HttpMethod: ParseScalarString(response.RootElement, "httpMethodType"),
            TimeoutSeconds: ParseNullableInt(response.RootElement, "timeout"),
            SslExpiresAt: ParseNullableDateTimeOffset(response.RootElement, "sslExpiryDateTime"),
            DomainExpiresAt: ParseNullableDateTimeOffset(response.RootElement, "domainExpireDate"),
            MonitoredRegions: ParseRegions(response.RootElement),
            CurrentStateDurationSeconds: ParseNullableLong(response.RootElement, "currentStateDuration"),
            LastIncident: ParseIncident(response.RootElement)
        );
        return monitor;
    }

    public async Task UpdateMonitorAsync(string externalId, string? name, string? url, string? type, List<string>? tags)
    {
        var payload = new Dictionary<string, object>();
        if (name != null)
        {
            payload.Add("friendlyName", name);
        }
        if (url != null)
        {
            payload.Add("url", url);
        }
        if (type != null)
        {
            payload.Add("type", UptimeMonitorTypes.Normalize(type));
        }
        if (tags != null)
        {
            payload.Add("tagNames", tags);
        }

        if (payload.Count == 0)
        {
            return;
        }

        var monitorId = ParseExternalId(externalId);
        var request = new HttpRequestMessage(HttpMethod.Patch, $"https://api.uptimerobot.com/v3/monitors/{monitorId}")
        {
            Content = JsonContent.Create(payload)
        };
        using var response = await GetResponseAsync(request, $"Update: {name ?? "Unspecified"} ({monitorId})");
    }

    public async Task<List<MonitoringProviderMonitor>> GetMonitorsAsync(IReadOnlyCollection<string>? externalIds = null)
    {
        var monitors = new List<MonitoringProviderMonitor>();
        var baseUrl = "https://api.uptimerobot.com/v3/monitors";
        var queryParams = new List<string> { "limit=200" };

        if (externalIds is { Count: > 0 })
        {
            queryParams.Add($"monitors={string.Join("-", externalIds.Select(ParseExternalId))}");
        }

        var nextUrl = $"{baseUrl}?{string.Join("&", queryParams)}";
        var context = "GetMonitors";

        while (!string.IsNullOrEmpty(nextUrl))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
            using var response = await GetResponseAsync(request, context);

            if (response.RootElement.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var monitor in dataProp.EnumerateArray())
                {
                    monitors.Add(new MonitoringProviderMonitor(
                        ExternalId: monitor.GetProperty("id").GetInt32().ToString(CultureInfo.InvariantCulture),
                        Type: ParseType(monitor),
                        Name: monitor.GetProperty("friendlyName").GetString()!,
                        Url: monitor.GetProperty("url").GetString()!,
                        Status: monitor.GetProperty("status").GetString()!,
                        CreatedDate: monitor.GetProperty("createDateTime").GetDateTimeOffset(),
                        UpdateInterval: monitor.GetProperty("interval").GetInt32(),
                        Tags: ParseTags(monitor),
                        HttpMethod: ParseScalarString(monitor, "httpMethodType"),
                        TimeoutSeconds: ParseNullableInt(monitor, "timeout"),
                        SslExpiresAt: ParseNullableDateTimeOffset(monitor, "sslExpiryDateTime"),
                        DomainExpiresAt: ParseNullableDateTimeOffset(monitor, "domainExpireDate"),
                        MonitoredRegions: ParseRegions(monitor),
                        CurrentStateDurationSeconds: ParseNullableLong(monitor, "currentStateDuration"),
                        LastIncident: ParseIncident(monitor)
                    ));
                }
            }

            nextUrl = null;
            if (response.RootElement.TryGetProperty("nextLink", out var nextLinkProp) && nextLinkProp.ValueKind == JsonValueKind.String)
            {
                var link = nextLinkProp.GetString();
                if (!string.IsNullOrWhiteSpace(link))
                {
                    nextUrl = link.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? link
                        : "https://api.uptimerobot.com" + (link.StartsWith("/") ? link : "/" + link);
                }
            }
        }

        return monitors;
    }

    private static List<string> ParseTags(JsonElement monitorElement)
    {
        var tagsList = new List<string>();
        if (monitorElement.TryGetProperty("tags", out var tagsElement) && tagsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var tag in tagsElement.EnumerateArray())
            {
                if (tag.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
                {
                    var tagName = nameElement.GetString();
                    if (!string.IsNullOrEmpty(tagName))
                    {
                        tagsList.Add(tagName);
                    }
                }
            }
        }
        return tagsList;
    }

    private static string ParseType(JsonElement monitorElement, string fallback = UptimeMonitorTypes.Http)
    {
        if (!monitorElement.TryGetProperty("type", out var typeElement))
        {
            return fallback;
        }

        return typeElement.ValueKind switch
        {
            JsonValueKind.String => UptimeMonitorTypes.Normalize(typeElement.GetString()),
            JsonValueKind.Number => typeElement.GetRawText(),
            _ => fallback
        };
    }

    private static string? ParseScalarString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.GetRawText();
    }

    private static int? ParseNullableInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number)) return number;
        return property.ValueKind == JsonValueKind.String
            && int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
                ? number
                : null;
    }

    private static long? ParseNullableLong(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var number)) return number;
        return property.ValueKind == JsonValueKind.String
            && long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
                ? number
                : null;
    }

    private static DateTimeOffset? ParseNullableDateTimeOffset(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property)
            || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(
                property.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var unixValue))
        {
            return unixValue > 10_000_000_000
                ? DateTimeOffset.FromUnixTimeMilliseconds(unixValue)
                : DateTimeOffset.FromUnixTimeSeconds(unixValue);
        }

        return null;
    }

    private static List<string> ParseRegions(JsonElement monitorElement)
    {
        if (!monitorElement.TryGetProperty("regionalData", out var regionalData)
            || regionalData.ValueKind != JsonValueKind.Object
            || !regionalData.TryGetProperty("REGION", out var regions)
            || regions.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return regions.EnumerateArray()
            .Where(region => region.ValueKind == JsonValueKind.String)
            .Select(region => region.GetString())
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region!)
            .ToList();
    }

    private static MonitoringProviderIncident? ParseIncident(JsonElement monitorElement)
    {
        if (!monitorElement.TryGetProperty("lastIncident", out var incident)
            || incident.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return new MonitoringProviderIncident(
            ParseScalarString(incident, "id"),
            ParseScalarString(incident, "status"),
            ParseScalarString(incident, "cause"),
            ParseScalarString(incident, "reason"),
            ParseNullableDateTimeOffset(incident, "startedAt"),
            ParseNullableLong(incident, "duration"));
    }
    
    public async Task<double> GetUptimeAsync(string externalId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? monitorName = null)
    {
        var monitorId = ParseExternalId(externalId);
        var url = $"https://api.uptimerobot.com/v3/monitors/{monitorId}/stats/uptime";
        if (startDate.HasValue && endDate.HasValue)
        {
            var fromStr = startDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toStr = endDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            url += $"?from={fromStr}&to={toStr}";
        }
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await GetResponseAsync(request, monitorName != null ? $"Monitor: {monitorName}" : $"MonitorId: {monitorId}");
        return response.RootElement.GetProperty("uptime").GetDouble();
    }
        
    public async Task<(int? Average, int? Lowest, int? Highest)> GetResponseTimeAsync(string externalId, DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, string? monitorName = null)
    {
        var monitorId = ParseExternalId(externalId);
        var url = $"https://api.uptimerobot.com/v3/monitors/{monitorId}/stats/response-time";
        if (startDate.HasValue && endDate.HasValue)
        {
            var fromStr = startDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toStr = endDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            url += $"?from={fromStr}&to={toStr}";
        }
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await GetResponseAsync(request, monitorName != null ? $"Monitor: {monitorName}" : $"MonitorId: {monitorId}");
        var summary = response.RootElement.GetProperty("summary");
        
        int? avg = summary.TryGetProperty("avg", out var avgProp) && avgProp.ValueKind != JsonValueKind.Null ? avgProp.GetInt32() : null;
        int? lowest = summary.TryGetProperty("min", out var minProp) && minProp.ValueKind != JsonValueKind.Null ? minProp.GetInt32() : null;
        int? highest = summary.TryGetProperty("max", out var maxProp) && maxProp.ValueKind != JsonValueKind.Null ? maxProp.GetInt32() : null;

        return (avg, lowest, highest);
    }

    public async Task DeleteMonitorAsync(string externalId)
    {
        var monitorId = ParseExternalId(externalId);
        var request = new HttpRequestMessage(HttpMethod.Delete, $"https://api.uptimerobot.com/v3/monitors/{monitorId}");
        await GetResponseAsync(request, $"Delete MonitorId: {monitorId}");
    }

    public async Task PauseMonitorAsync(string externalId)
    {
        var monitorId = ParseExternalId(externalId);
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.uptimerobot.com/v3/monitors/{monitorId}/pause");
        request.Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");
        await GetResponseAsync(request, $"Pause MonitorId: {monitorId}");
    }

    public async Task StartMonitorAsync(string externalId)
    {
        var monitorId = ParseExternalId(externalId);
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.uptimerobot.com/v3/monitors/{monitorId}/start");
        request.Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");
        await GetResponseAsync(request, $"Start MonitorId: {monitorId}");
    }

    public async Task<MonitoringProviderAccount> GetAccountDetailsAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.uptimerobot.com/v3/user/me");
        using var response = await GetResponseAsync(request, "GetAccountDetails");
        
        var root = response.RootElement;
        var sub = root.GetProperty("activeSubscription");

        return new MonitoringProviderAccount(
            Email: root.GetProperty("email").GetString()!,
            FullName: root.GetProperty("fullName").GetString()!,
            MonitorsCount: root.GetProperty("monitorsCount").GetInt32(),
            MonitorLimit: root.GetProperty("monitorLimit").GetInt32(),
            ActiveSubscriptionPlan: sub.GetProperty("plan").GetString()!
        );
    }

    private static UptimeRobotSettings ParseSettings(string? settings)
        => string.IsNullOrWhiteSpace(settings)
            ? new UptimeRobotSettings(null)
            : JsonSerializer.Deserialize<UptimeRobotSettings>(settings, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
              ?? new UptimeRobotSettings(null);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record UptimeRobotSettings(string? ApiKey);

    private static int ParseExternalId(string externalId)
        => int.TryParse(externalId, NumberStyles.None, CultureInfo.InvariantCulture, out var monitorId) && monitorId > 0
            ? monitorId
            : throw new ArgumentException("UptimeRobot monitor IDs must be positive integers.", nameof(externalId));
}


