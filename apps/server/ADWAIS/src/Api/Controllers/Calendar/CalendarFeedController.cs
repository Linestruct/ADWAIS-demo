// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Security.Claims;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Calendar;

/// <summary>
/// Provides personal calendar feed tokens and token-protected iCalendar feeds.
/// </summary>
[ApiController]
[Route("api/intranet/calendar")]
public class CalendarFeedController(ICalendarFeedService feedService) : ControllerBase
{
    private readonly ICalendarFeedService _feedService = feedService;

    /// <summary>
    /// Generates an iCalendar feed for a valid feed token.
    /// </summary>
    /// <remarks>
    /// The token grants access to the calendar feed, so clients must keep the feed URL private.
    /// </remarks>
    /// <param name="token">The calendar feed token.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An iCalendar file containing the events available to the token.</returns>
    [HttpGet("feed.ics")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeed([FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("A calendar feed token is required.");
        }

        var fileBytes = await _feedService.GenerateIcsFeedAsync(token, ct);
        return File(fileBytes, "text/calendar", "feed.ics");
    }

    /// <summary>
    /// Retrieves the current calendar feed token for the authenticated user.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The authenticated user's calendar feed token.</returns>
    [HttpGet("token")]
    [Authorize(Policy = "StaffAccess")]
    public async Task<ActionResult<CalendarTokenDto>> GetToken(CancellationToken ct)
    {
        var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
        {
            return Unauthorized("User context is invalid.");
        }

        var token = await _feedService.GetUserCalendarFeedTokenAsync(userId, ct);
        return Ok(new CalendarTokenDto { Token = token });
    }

    /// <summary>
    /// Replaces the authenticated user's calendar feed token.
    /// </summary>
    /// <remarks>
    /// Existing calendar feed URLs stop working after the token is regenerated.
    /// </remarks>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The new calendar feed token.</returns>
    [HttpPost("token/regenerate")]
    [Authorize(Policy = "StaffAccess")]
    public async Task<ActionResult<CalendarTokenDto>> RegenerateToken(CancellationToken ct)
    {
        var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(nameIdentifier) || !Guid.TryParse(nameIdentifier, out var userId))
        {
            return Unauthorized("User context is invalid.");
        }

        var token = await _feedService.RegenerateUserCalendarFeedTokenAsync(userId, ct);
        return Ok(new CalendarTokenDto { Token = token });
    }
}
