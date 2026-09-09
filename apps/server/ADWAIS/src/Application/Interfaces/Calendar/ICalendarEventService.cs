// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.Intranet;
using FluentResults;

namespace Adwais.Application.Interfaces;

public interface ICalendarEventService
{
    Task<CalendarEventDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<CalendarEventDto>> GetEventsAsync(DateTimeOffset? start, DateTimeOffset? end, CancellationToken ct = default);
    Task<Result<CalendarEventDto>> CreateEventAsync(Guid? userId, CreateCalendarEventDto dto, CancellationToken ct = default);
    Task<Result<CalendarEventDto>> UpdateEventAsync(Guid id, UpdateCalendarEventDto dto, CancellationToken ct = default);
    Task<Result> DeleteEventAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<CalendarEventDto>> GetTodaysEventsAsync(CancellationToken ct = default);
}
