// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Weather;

/// <param name="Location">The resolved location name from config.</param>
/// <param name="Temperature">Current temperature in degrees Celsius.</param>
/// <param name="ApparentTemperature">Current apparent temperature in degrees Celsius.</param>
/// <param name="PrecipitationProbability">Current precipitation probability as a percentage.</param>
/// <param name="Precipitation">Current precipitation in millimetres.</param>
/// <param name="WeatherCode">WMO weather interpretation code.</param>
/// <param name="FetchedAt">UTC timestamp of when this reading was fetched.</param>
public record WeatherDto(
    string Location,
    double Temperature,
    double ApparentTemperature,
    int PrecipitationProbability,
    double Precipitation,
    int WeatherCode,
    DateTimeOffset FetchedAt
);
