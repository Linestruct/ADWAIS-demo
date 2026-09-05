// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using Adwais.Domain.Enums;

namespace Adwais.Application.DTOs.Intranet;

public record CalendarEventDto(
    Guid Id,
    string Title,
    string? Description,
    string? Location,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    EventType EventType,
    bool IsRecurring,
    RecurrenceType Recurrence,
    Guid? UserId,
    string? UserName,
    string? ExternalUid,
    Guid? CalendarSubscriptionId
);
