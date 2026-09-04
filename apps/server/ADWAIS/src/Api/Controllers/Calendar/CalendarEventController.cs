// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Security.Claims;
using Adwais.Api.Extensions;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Calendar;

[ApiController]
[Route("api/intranet/events")]
public class CalendarEventController(ICalendarEventService eventService) : ControllerBase
{
    private readonly ICalendarEventService _eventService = eventService;

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

    [HttpGet("today")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<CalendarEventDto>>> GetTodaysEvents(CancellationToken ct)
    {
        var events = await _eventService.GetTodaysEventsAsync(ct);
        return Ok(events);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<CalendarEventDto>> GetEvent(Guid id, CancellationToken ct)
    {
        var calendarEvent = await _eventService.GetEventByIdAsync(id, ct);
        if (calendarEvent == null) return NotFound();
        return Ok(calendarEvent);
    }

    [HttpPost]
    [Authorize(Policy = "StaffAccess")]
    [ProducesResponseType(typeof(CalendarEventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CalendarEventDto>> CreateEvent([FromBody] CreateCalendarEventDto dto, CancellationToken ct)
    {
        var nameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = null;
        if (!string.IsNullOrEmpty(nameIdentifier) && Guid.TryParse(nameIdentifier, out var parsedId))
        {
            userId = parsedId;
        }

        var result = await _eventService.CreateEventAsync(userId, dto, ct);
        return result.IsFailed
            ? result.ToProblem(HttpContext)
            : CreatedAtAction(nameof(GetEvent), new { id = result.Value.Id }, result.Value);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "StaffAccess")]
    [ProducesResponseType(typeof(CalendarEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarEventDto>> UpdateEvent(Guid id, [FromBody] UpdateCalendarEventDto dto, CancellationToken ct)
    {
        var result = await _eventService.UpdateEventAsync(id, dto, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "StaffAccess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken ct)
    {
        var result = await _eventService.DeleteEventAsync(id, ct);
        if (result.IsFailed) return result.ToProblem(HttpContext);
        return NoContent();
    }
}
