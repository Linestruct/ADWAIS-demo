// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Adwais.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services;

public class CalendarEventService(IApplicationDbContext dbContext) : ICalendarEventService
{
    private readonly IApplicationDbContext _dbContext = dbContext;

    public async Task<CalendarEventDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default)
    {
        var calendarEvent = await _dbContext.CalendarEvents
            .Include(oe => oe.User)
            .SingleOrDefaultAsync(oe => oe.Id == id, ct);

        if (calendarEvent == null) return null;
        return MapToDto(calendarEvent);
    }

    public async Task<IEnumerable<CalendarEventDto>> GetEventsAsync(DateTimeOffset? start, DateTimeOffset? end, CancellationToken ct = default)
    {
        var startUtc = start?.ToUniversalTime() ?? DateTimeOffset.MinValue;
        var endUtc = end?.ToUniversalTime() ?? DateTimeOffset.MaxValue;
        var expansionCap = DateTimeOffset.UtcNow.AddYears(1);
        var effectiveEnd = endUtc < expansionCap ? endUtc : expansionCap;

        // Fetch non-recurring events that overlap the window, plus all recurring events
        // whose base start time is before the window ends (they may have occurrences inside).
        var dbEvents = await _dbContext.CalendarEvents
            .Include(oe => oe.User)
            .Where(oe =>
                (!oe.IsRecurring && oe.EndTime >= startUtc && oe.StartTime <= endUtc) ||
                (oe.IsRecurring && oe.StartTime <= endUtc))
            .ToListAsync(ct);

        var results = new List<CalendarEventDto>();

        foreach (var oe in dbEvents)
        {
            if (!oe.IsRecurring || oe.Recurrence == RecurrenceType.None)
            {
                results.Add(MapToDto(oe));
                continue;
            }

            var duration = oe.EndTime - oe.StartTime;

            for (var n = 0; ; n++)
            {
                // Calculate Nth occurrence from base start — no drift, handles month/year edge cases natively.
                var occurrenceStart = oe.Recurrence switch
                {
                    RecurrenceType.Daily   => oe.StartTime.AddDays(n),
                    RecurrenceType.Weekly  => oe.StartTime.AddDays(n * 7),
                    RecurrenceType.Monthly => oe.StartTime.AddMonths(n),
                    RecurrenceType.Yearly  => oe.StartTime.AddYears(n),
                    _                      => effectiveEnd.AddTicks(1) // exit condition
                };

                if (occurrenceStart > effectiveEnd) break;

                var occurrenceEnd = occurrenceStart + duration;

                // Only include occurrences that overlap the requested window.
                if (occurrenceEnd >= startUtc && occurrenceStart <= endUtc)
                {
                    results.Add(new CalendarEventDto(
                        oe.Id,
                        oe.Title,
                        oe.Description,
                        oe.Location,
                        occurrenceStart,
                        occurrenceEnd,
                        oe.EventType,
                        oe.IsRecurring,
                        oe.Recurrence,
                        oe.UserId,
                        oe.User?.Name,
                        oe.ExternalUid,
                        oe.CalendarSubscriptionId
                    ));
                }
            }
        }

        return results.OrderBy(o => o.StartTime);
    }

    public async Task<CalendarEventDto> CreateEventAsync(Guid? userId, CreateCalendarEventDto dto, CancellationToken ct = default)
    {
        var calendarEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Location = dto.Location,
            StartTime = dto.StartTime.ToUniversalTime(),
            EndTime = dto.EndTime.ToUniversalTime(),
            EventType = dto.EventType,
            IsRecurring = dto.IsRecurring,
            Recurrence = dto.Recurrence,
            UserId = userId
        };

        _dbContext.CalendarEvents.Add(calendarEvent);
        await _dbContext.SaveChangesAsync(ct);

        // Fetch again to include User details if userId was provided
        if (userId.HasValue)
        {
            calendarEvent.User = await _dbContext.Users.FindAsync(new object[] { userId.Value }, ct);
        }

        return MapToDto(calendarEvent);
    }

    public async Task<CalendarEventDto?> UpdateEventAsync(Guid id, UpdateCalendarEventDto dto, CancellationToken ct = default)
    {
        var calendarEvent = await _dbContext.CalendarEvents
            .Include(oe => oe.User)
            .SingleOrDefaultAsync(oe => oe.Id == id, ct);

        if (calendarEvent == null) return null;

        if (dto.Title != null) calendarEvent.Title = dto.Title;
        if (dto.Description != null) calendarEvent.Description = dto.Description;
        if (dto.Location != null) calendarEvent.Location = dto.Location;
        if (dto.StartTime.HasValue) calendarEvent.StartTime = dto.StartTime.Value.ToUniversalTime();
        if (dto.EndTime.HasValue) calendarEvent.EndTime = dto.EndTime.Value.ToUniversalTime();
        if (dto.EventType.HasValue) calendarEvent.EventType = dto.EventType.Value;
        if (dto.IsRecurring.HasValue) calendarEvent.IsRecurring = dto.IsRecurring.Value;
        if (dto.Recurrence.HasValue) calendarEvent.Recurrence = dto.Recurrence.Value;

        if (calendarEvent.EndTime < calendarEvent.StartTime)
        {
            throw new ArgumentException("End time must be greater than or equal to start time.");
        }

        await _dbContext.SaveChangesAsync(ct);
        return MapToDto(calendarEvent);
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken ct = default)
    {
        var calendarEvent = await _dbContext.CalendarEvents.FindAsync(new object[] { id }, ct);
        if (calendarEvent == null) return false;

        _dbContext.CalendarEvents.Remove(calendarEvent);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public Task<IEnumerable<CalendarEventDto>> GetTodaysEventsAsync(CancellationToken ct = default)
    {
        var todayStart = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1).AddTicks(-1);
        return GetEventsAsync(todayStart, todayEnd, ct);
    }

    private static CalendarEventDto MapToDto(CalendarEvent oe)
    {
        return new CalendarEventDto(
            oe.Id,
            oe.Title,
            oe.Description,
            oe.Location,
            oe.StartTime,
            oe.EndTime,
            oe.EventType,
            oe.IsRecurring,
            oe.Recurrence,
            oe.UserId,
            oe.User?.Name,
            oe.ExternalUid,
            oe.CalendarSubscriptionId
        );
    }
}
