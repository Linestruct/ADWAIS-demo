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
using Adwais.Application.DTOs.Weather;
using Adwais.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Resolves the configured weather location through Open-Meteo geocoding, then fetches
/// current conditions from the Open-Meteo Forecast API.
/// No API key required. Responses use the configured cache interval.
/// </summary>
public class WeatherService(
    HttpClient httpClient,
    IGlobalConfigService configService,
    IMemoryCache cache) : IWeatherService
{
    private const string CacheKey = "weather:current";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<WeatherDto> GetCurrentWeatherAsync(CancellationToken ct = default)
    {
        var config = await configService.GetConfigAsync(ct);
        var location = config.WeatherLocation;
        if (string.IsNullOrWhiteSpace(location))
            throw new InvalidOperationException("Weather location is not configured.");

        if (cache.TryGetValue(CacheKey, out WeatherDto? cached) && cached is not null)
            return cached;

        var (latitude, longitude, resolvedLocation) = await GeocodeAsync(location, ct);
        var dto = await FetchForecastAsync(latitude, longitude, resolvedLocation, ct);

        var duration = TimeSpan.FromMinutes(config.WeatherFetchIntervalMinutes > 0 ? config.WeatherFetchIntervalMinutes : 15);
        cache.Set(CacheKey, dto, duration);
        return dto;
    }

    private async Task<(double latitude, double longitude, string location)> GeocodeAsync(
        string location,
        CancellationToken ct)
    {
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(location)}&count=1&language=en&format=json";
        var response = await httpClient.GetFromJsonAsync<GeocodingResponse>(url, JsonOptions, ct)
            ?? throw new InvalidOperationException($"Geocoding API returned no response for '{location}'.");
        var result = response.Results?.Length > 0
            ? response.Results[0]
            : throw new InvalidOperationException($"No coordinates found for weather location '{location}'.");
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
                       ?? throw new InvalidOperationException("Open-Meteo forecast API returned null.");

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
