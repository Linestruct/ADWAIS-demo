// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.DTOs.Intranet;
using Adwais.Api.Extensions;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Calendar;

[ApiController]
[Route("api/intranet/calendar/subscriptions")]
public class CalendarSubscriptionController(ICalendarSubscriptionService subscriptionService) : ControllerBase
{
    private readonly ICalendarSubscriptionService _subscriptionService = subscriptionService;

    [HttpGet]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<CalendarSubscriptionDto>>> GetSubscriptions(CancellationToken ct)
    {
        var subs = await _subscriptionService.GetSubscriptionsAsync(ct);
        return Ok(subs);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<CalendarSubscriptionDto>> GetSubscription(Guid id, CancellationToken ct)
    {
        var sub = await _subscriptionService.GetSubscriptionByIdAsync(id, ct);
        if (sub == null) return NotFound();
        return Ok(sub);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(CalendarSubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CalendarSubscriptionDto>> CreateSubscription([FromBody] CreateCalendarSubscriptionDto dto, CancellationToken ct)
    {
        var result = await _subscriptionService.CreateSubscriptionAsync(dto, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : CreatedAtAction(nameof(GetSubscription), new { id = result.Value.Id }, result.Value);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(CalendarSubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarSubscriptionDto>> UpdateSubscription(Guid id, [FromBody] UpdateCalendarSubscriptionDto dto, CancellationToken ct)
    {
        var result = await _subscriptionService.UpdateSubscriptionAsync(id, dto, ct);
        return result.IsFailed ? result.ToProblem(HttpContext) : Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken ct)
    {
        var result = await _subscriptionService.DeleteSubscriptionAsync(id, ct);
        if (result.IsFailed) return result.ToProblem(HttpContext);
        return NoContent();
    }

    [HttpPost("{id:guid}/sync")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SyncSubscription(Guid id, CancellationToken ct)
    {
        await _subscriptionService.TriggerSyncAsync(id, ct);
        return Ok();
    }
}
