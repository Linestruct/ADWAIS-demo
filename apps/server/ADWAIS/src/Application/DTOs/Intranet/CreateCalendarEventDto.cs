// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using Adwais.Domain.Enums;

namespace Adwais.Application.DTOs.Intranet;

/// <summary>
/// Request data for creating an organization calendar event.
/// </summary>
/// <param name="Title">The event title.</param>
/// <param name="Description">Optional details shown with the event.</param>
/// <param name="Location">Optional location for the event.</param>
/// <param name="StartTime">The event start time.</param>
/// <param name="EndTime">The event end time.</param>
/// <param name="EventType">The category of the event.</param>
/// <param name="IsRecurring">A value indicating whether the event repeats.</param>
/// <param name="Recurrence">The recurrence pattern. Use <see cref="Adwais.Domain.Enums.RecurrenceType.None"/> for a non-recurring event.</param>
public record CreateCalendarEventDto(
    string Title,
    string? Description,
    string? Location,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    EventType EventType,
    bool IsRecurring,
    RecurrenceType Recurrence
);
