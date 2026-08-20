// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using Adwais.Domain.Enums;

namespace Adwais.Application.DTOs.Intranet;

/// <summary>
/// Request data for updating an organization calendar event.
/// </summary>
/// <param name="Title">The replacement event title, or <see langword="null"/> to keep the current title.</param>
/// <param name="Description">The replacement description, or <see langword="null"/> to keep the current description.</param>
/// <param name="Location">The replacement location, or <see langword="null"/> to keep the current location.</param>
/// <param name="StartTime">The replacement start time, or <see langword="null"/> to keep the current value.</param>
/// <param name="EndTime">The replacement end time, or <see langword="null"/> to keep the current value.</param>
/// <param name="EventType">The replacement event category, or <see langword="null"/> to keep the current value.</param>
/// <param name="IsRecurring">The replacement recurrence flag, or <see langword="null"/> to keep the current value.</param>
/// <param name="Recurrence">The replacement recurrence pattern, or <see langword="null"/> to keep the current value.</param>
public record UpdateCalendarEventDto(
    string? Title,
    string? Description,
    string? Location,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime,
    EventType? EventType,
    bool? IsRecurring,
    RecurrenceType? Recurrence
);
