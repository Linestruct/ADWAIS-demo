// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.Weather;

namespace Adwais.Application.Interfaces;

public interface IWeatherService
{
    /// <summary>Fetches current weather for the configured WeatherLocation.</summary>
    Task<WeatherDto> GetCurrentWeatherAsync(CancellationToken ct = default);
}
