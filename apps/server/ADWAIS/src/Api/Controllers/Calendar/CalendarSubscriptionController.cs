// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Calendar;

/// <summary>
/// Manages external calendar subscriptions used to import calendar events.
/// </summary>
[ApiController]
[Route("api/intranet/calendar/subscriptions")]
public class CalendarSubscriptionController(ICalendarSubscriptionService subscriptionService) : ControllerBase
{
    private readonly ICalendarSubscriptionService _subscriptionService = subscriptionService;

    /// <summary>
    /// Lists all configured calendar subscriptions.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The configured calendar subscriptions and their synchronization state.</returns>
    [HttpGet]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<IEnumerable<CalendarSubscriptionDto>>> GetSubscriptions(CancellationToken ct)
    {
        var subs = await _subscriptionService.GetSubscriptionsAsync(ct);
        return Ok(subs);
    }

    /// <summary>
    /// Retrieves a calendar subscription by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the subscription.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The requested subscription, or <see cref="NotFoundResult"/> when no subscription matches the ID.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "KioskOrStaffAccess")]
    public async Task<ActionResult<CalendarSubscriptionDto>> GetSubscription(Guid id, CancellationToken ct)
    {
        var sub = await _subscriptionService.GetSubscriptionByIdAsync(id, ct);
        if (sub == null) return NotFound();
        return Ok(sub);
    }

    /// <summary>
    /// Creates a calendar subscription.
    /// </summary>
    /// <param name="dto">The display name, iCalendar URL, and active state for the subscription.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created subscription.</returns>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CalendarSubscriptionDto>> CreateSubscription([FromBody] CreateCalendarSubscriptionDto dto, CancellationToken ct)
    {
        var sub = await _subscriptionService.CreateSubscriptionAsync(dto, ct);
        return CreatedAtAction(nameof(GetSubscription), new { id = sub.Id }, sub);
    }

    /// <summary>
    /// Updates a calendar subscription.
    /// </summary>
    /// <param name="id">The unique identifier of the subscription.</param>
    /// <param name="dto">The subscription fields to update. Omitted fields keep their current values.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated subscription, or <see cref="NotFoundResult"/> when no subscription matches the ID.</returns>
    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<CalendarSubscriptionDto>> UpdateSubscription(Guid id, [FromBody] UpdateCalendarSubscriptionDto dto, CancellationToken ct)
    {
        var sub = await _subscriptionService.UpdateSubscriptionAsync(id, dto, ct);
        if (sub == null) return NotFound();
        return Ok(sub);
    }

    /// <summary>
    /// Deletes a calendar subscription.
    /// </summary>
    /// <param name="id">The unique identifier of the subscription.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>No content when the subscription is deleted, or <see cref="NotFoundResult"/> when no subscription matches the ID.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken ct)
    {
        var success = await _subscriptionService.DeleteSubscriptionAsync(id, ct);
        if (!success) return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Starts an immediate synchronization for a calendar subscription.
    /// </summary>
    /// <param name="id">The unique identifier of the subscription.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An empty successful response after the synchronization has been queued.</returns>
    [HttpPost("{id:guid}/sync")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> SyncSubscription(Guid id, CancellationToken ct)
    {
        await _subscriptionService.TriggerSyncAsync(id, ct);
        return Ok();
    }
}
