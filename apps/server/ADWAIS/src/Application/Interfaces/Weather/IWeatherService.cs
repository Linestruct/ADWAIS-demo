// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.Weather;
using FluentResults;

namespace Adwais.Application.Interfaces;

public interface IWeatherService
{
    /// <summary>Fetches current weather for the configured WeatherLocation.</summary>
    Task<Result<WeatherDto>> GetCurrentWeatherAsync(CancellationToken ct = default);
}
