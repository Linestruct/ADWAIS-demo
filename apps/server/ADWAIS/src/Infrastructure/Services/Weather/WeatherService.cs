// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Exceptions;
using Adwais.Application.Common.Errors;
using Adwais.Application.DTOs.Weather;
using Adwais.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using FluentResults;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Resolves the configured weather location through Open-Meteo geocoding, then fetches
/// current conditions from the Open-Meteo Forecast API.
/// No API key required. Responses use the configured cache interval.
/// Organization requests use their configured location and cache entry. The
/// unscoped platform-admin view uses Stockholm as its default location.
/// </summary>
public class WeatherService(
    HttpClient httpClient,
    IOrganizationConfigService configService,
    IMemoryCache cache,
    ICurrentAccess currentAccess) : IWeatherService
{
    private const string CacheKeyPrefix = "weather:current:";
    private const string PlatformDefaultLocation = "Stockholm";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Result<WeatherDto>> GetCurrentWeatherAsync(CancellationToken ct = default)
    {
        var scope = currentAccess.Scope;
        if (scope is null)
            return Result.Fail<WeatherDto>(new ScopeDeniedError("an organization scope", "none"));

        var orgId = scope.OrganizationId;
        var config = orgId is { } organizationId
            ? await configService.GetConfigAsync(organizationId, ct)
            : null;
        var location = scope.IsPlatformAdmin
            ? PlatformDefaultLocation
            : config?.WeatherLocation;
        if (string.IsNullOrWhiteSpace(location))
            return Result.Fail<WeatherDto>(new ConfigurationError("Weather location is not configured."));

        var cacheKey = $"{CacheKeyPrefix}{orgId?.ToString() ?? "platform"}";
        if (cache.TryGetValue(cacheKey, out WeatherDto? cached) && cached is not null)
            return Result.Ok(cached);

        WeatherDto dto;
        try
        {
            var (latitude, longitude, resolvedLocation) = await GeocodeAsync(location, ct);
            dto = await FetchForecastAsync(latitude, longitude, resolvedLocation, ct);
        }
        catch (ConfigurationException exception)
        {
            return Result.Fail<WeatherDto>(new ConfigurationError(exception.Message));
        }
        catch (TaskCanceledException exception) when (!ct.IsCancellationRequested)
        {
            return Result.Fail<WeatherDto>(new ProviderTimeoutError(exception.Message));
        }
        catch (HttpRequestException exception)
        {
            return Result.Fail<WeatherDto>(new ProviderError(exception.Message));
        }

        var duration = TimeSpan.FromMinutes(
            config is not null && config.WeatherFetchIntervalMinutes > 0
                ? config.WeatherFetchIntervalMinutes
                : 15);
        cache.Set(cacheKey, dto, duration);
        return Result.Ok(dto);
    }

    private async Task<(double latitude, double longitude, string location)> GeocodeAsync(
        string location,
        CancellationToken ct)
    {
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}&count=1&language=en&format=json";
        var response = await httpClient.GetFromJsonAsync<GeocodingResponse>(url, JsonOptions, ct)
            ?? throw new HttpRequestException($"Geocoding API returned no response for '{location}'.");
        var result = response.Results?.Length > 0
            ? response.Results[0]
            : throw new ConfigurationException($"No coordinates found for weather location '{location}'.");
        return (result.Latitude, result.Longitude, result.Name);
    }

    private async Task<WeatherDto> FetchForecastAsync(
        double latitude,
        double longitude,
        string locationName,
        CancellationToken ct)
    {
        var latitudeValue = latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var longitudeValue = longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var url = $"https://api.open-meteo.com/v1/forecast" +
                  $"?latitude={latitudeValue}&longitude={longitudeValue}" +
                  $"&current=temperature_2m,apparent_temperature,precipitation_probability,precipitation,weather_code" +
                  $"&timezone=UTC";

        var response = await httpClient.GetFromJsonAsync<ForecastResponse>(url, JsonOptions, ct)
                       ?? throw new HttpRequestException("Open-Meteo forecast API returned null.");

        var current = response.Current;
        return new WeatherDto(
            locationName,
            current.Temperature2m,
            current.ApparentTemperature,
            current.PrecipitationProbability,
            current.Precipitation,
            current.WeatherCode,
            DateTimeOffset.UtcNow
        );
    }

    // ── Internal deserialization models ───────────────────────────────────────

    private sealed record GeocodingResponse(
        [property: JsonPropertyName("results")] GeocodingResult[]? Results
    );

    private sealed record GeocodingResult(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("latitude")] double Latitude,
        [property: JsonPropertyName("longitude")] double Longitude
    );

    private sealed record ForecastResponse(
        [property: JsonPropertyName("current")] CurrentWeather Current
    );

    private sealed record CurrentWeather(
        [property: JsonPropertyName("temperature_2m")] double Temperature2m,
        [property: JsonPropertyName("apparent_temperature")] double ApparentTemperature,
        [property: JsonPropertyName("precipitation_probability")] int PrecipitationProbability,
        [property: JsonPropertyName("precipitation")] double Precipitation,
        [property: JsonPropertyName("weather_code")] int WeatherCode
    );
}
