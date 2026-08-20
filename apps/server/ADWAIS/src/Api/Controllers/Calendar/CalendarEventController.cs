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
/// Creates, reads, updates, and deletes organization calendar events.
/// </summary>
[ApiController]
[Route("api/intranet/events")]
public class CalendarEventController(ICalendarEventService eventService) : ControllerBase
{
    private readonly ICalendarEventService _eventService = eventService;

    /// <summary>
    /// Lists calendar events within an optional time range.
    /// </summary>
    /// <param name="start">The inclusive start of the requested range.</param>
    /// <param name="end">The exclusive end of the requested range.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The calendar events that overlap the requested range.</returns>
    [HttpGet]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<CalendarEventDto>>> GetEvents(
        [FromQuery] DateTimeOffset? start,
        [FromQuery] DateTimeOffset? end,
        CancellationToken ct)
    {
        var events = await _eventService.GetEventsAsync(start, end, ct);
        return Ok(events);
    }

    /// <summary>
    /// Lists the calendar events scheduled for today.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Today's calendar events.</returns>
    [HttpGet("today")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<CalendarEventDto>>> GetTodaysEvents(CancellationToken ct)
    {
        var events = await _eventService.GetTodaysEventsAsync(ct);
        return Ok(events);
    }

    /// <summary>
    /// Retrieves a calendar event by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The requested event, or <see cref="NotFoundResult"/> when no event matches the ID.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<CalendarEventDto>> GetEvent(Guid id, CancellationToken ct)
    {
        var calendarEvent = await _eventService.GetEventByIdAsync(id, ct);
        if (calendarEvent == null) return NotFound();
        return Ok(calendarEvent);
    }

    /// <summary>
    /// Creates a calendar event for the authenticated user.
    /// </summary>
    /// <param name="dto">The event title, time range, type, and recurrence settings.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created calendar event.</returns>
    [HttpPost]
    [Authorize(Policy = "StaffAccess")]
    public async Task<ActionResult<CalendarEventDto>> CreateEvent([FromBody] CreateCalendarEventDto dto, CancellationToken ct)
    {
        var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(nameIdentifier) && Guid.TryParse(nameIdentifier, out var parsedId))
        {
            userId = parsedId;
        }

        var calendarEvent = await _eventService.CreateEventAsync(userId, dto, ct);
        return CreatedAtAction(nameof(GetEvent), new { id = calendarEvent.Id }, calendarEvent);
    }

    /// <summary>
    /// Updates a calendar event.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="dto">The event fields to update. Omitted fields keep their current values.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated event, or <see cref="NotFoundResult"/> when no event matches the ID.</returns>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "StaffAccess")]
    public async Task<ActionResult<CalendarEventDto>> UpdateEvent(Guid id, [FromBody] UpdateCalendarEventDto dto, CancellationToken ct)
    {
        var calendarEvent = await _eventService.UpdateEventAsync(id, dto, ct);
        if (calendarEvent == null) return NotFound();
        return Ok(calendarEvent);
    }

    /// <summary>
    /// Deletes a calendar event.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>No content when the event is deleted, or <see cref="NotFoundResult"/> when no event matches the ID.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "StaffAccess")]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken ct)
    {
        var success = await _eventService.DeleteEventAsync(id, ct);
        if (!success) return NotFound();
        return NoContent();
    }
}
