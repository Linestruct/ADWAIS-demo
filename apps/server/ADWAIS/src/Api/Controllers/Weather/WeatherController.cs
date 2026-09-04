// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.DTOs.Weather;
using Adwais.Api.Extensions;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Weather;

[ApiController]
[Route("api/weather")]
public class WeatherController(IWeatherService weatherService) : ControllerBase
{
    private readonly IWeatherService _weatherService = weatherService;

    /// <summary>Returns current weather for the configured location.</summary>
    [HttpGet]
    [Authorize(Policy = "KioskOrStaffAccess")]
    [ProducesResponseType(typeof(WeatherDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<WeatherDto>> GetCurrentWeather(CancellationToken ct)
    {
        var weather = await _weatherService.GetCurrentWeatherAsync(ct);
        return weather.IsFailed ? weather.ToProblem(HttpContext) : Ok(weather.Value);
    }
}
