// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.Weather;
using FluentResults;

namespace Adwais.Application.Interfaces;

public interface IWeatherService
{
    /// <summary>Fetches current weather for the organization or the platform default location.</summary>
    Task<Result<WeatherDto>> GetCurrentWeatherAsync(CancellationToken ct = default);
}
