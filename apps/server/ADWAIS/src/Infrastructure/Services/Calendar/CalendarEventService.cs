// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Adwais.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using FluentResults;

namespace Adwais.Infrastructure.Services;

public class CalendarEventService(IApplicationDbContext dbContext, ICurrentAccess currentAccess) : ICalendarEventService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ICurrentAccess _currentAccess = currentAccess;

    private OrganizationFilter OrganizationFilter => OrganizationFilter.From(_currentAccess.Scope);

    private ScopeDeniedError ScopeDenied()
        => new("an organization scope", _currentAccess.Scope?.OrganizationId?.ToString() ?? "none");

    public async Task<CalendarEventDto?> GetEventByIdAsync(Guid id, CancellationToken ct = default)
    {
        var filter = OrganizationFilter;
        var query = _dbContext.CalendarEvents
            .Include(oe => oe.User)
            .Where(oe => oe.Id == id);
        if (filter.Denied) return null;
        if (filter.OrganizationId is { } orgId) query = query.Where(oe => oe.OrganizationId == orgId);

        var calendarEvent = await query.SingleOrDefaultAsync(ct);

        if (calendarEvent == null) return null;
        return MapToDto(calendarEvent);
    }

    public async Task<IEnumerable<CalendarEventDto>> GetEventsAsync(DateTimeOffset? start, DateTimeOffset? end, CancellationToken ct = default)
    {
        var filter = OrganizationFilter;
        if (filter.Denied) return [];

        var startUtc = start?.ToUniversalTime() ?? DateTimeOffset.MinValue;
        var endUtc = end?.ToUniversalTime() ?? DateTimeOffset.MaxValue;
        var expansionCap = DateTimeOffset.UtcNow.AddYears(1);
        var effectiveEnd = endUtc < expansionCap ? endUtc : expansionCap;

        // Fetch non-recurring events that overlap the window, plus all recurring events
        // whose base start time is before the window ends (they may have occurrences inside).
        var query = _dbContext.CalendarEvents
            .Include(oe => oe.User)
            .Where(oe =>
                (!oe.IsRecurring && oe.EndTime >= startUtc && oe.StartTime <= endUtc) ||
                (oe.IsRecurring && oe.StartTime <= endUtc));
        if (filter.OrganizationId is { } orgId) query = query.Where(oe => oe.OrganizationId == orgId);

        var dbEvents = await query.ToListAsync(ct);

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

    public async Task<Result<CalendarEventDto>> CreateEventAsync(Guid? userId, CreateCalendarEventDto dto, CancellationToken ct = default)
    {
        var filter = OrganizationFilter;
        if (filter.Denied || filter.OrganizationId is null)
            return Result.Fail<CalendarEventDto>(ScopeDenied());

        var calendarEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = filter.OrganizationId.Value,
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

        return Result.Ok(MapToDto(calendarEvent));
    }

    public async Task<Result<CalendarEventDto>> UpdateEventAsync(Guid id, UpdateCalendarEventDto dto, CancellationToken ct = default)
    {
        var filter = OrganizationFilter;
        var query = _dbContext.CalendarEvents
            .Include(oe => oe.User)
            .Where(oe => oe.Id == id);

        var calendarEvent = await query.SingleOrDefaultAsync(ct);
        if (calendarEvent is null) return Result.Fail<CalendarEventDto>(new NotFoundError("calendar event", id));
        if (filter.Denied || (filter.OrganizationId is { } orgId && calendarEvent.OrganizationId != orgId))
            return Result.Fail<CalendarEventDto>(ScopeDenied());

        var startTime = dto.StartTime?.ToUniversalTime() ?? calendarEvent.StartTime;
        var endTime = dto.EndTime?.ToUniversalTime() ?? calendarEvent.EndTime;
        if (endTime < startTime)
            return Result.Fail<CalendarEventDto>(new ValidationError(new Dictionary<string, string[]>
            {
                ["endTime"] = ["End time must be greater than or equal to start time."]
            }));

        if (dto.Title != null) calendarEvent.Title = dto.Title;
        if (dto.Description != null) calendarEvent.Description = dto.Description;
        if (dto.Location != null) calendarEvent.Location = dto.Location;
        calendarEvent.StartTime = startTime;
        calendarEvent.EndTime = endTime;
        if (dto.EventType.HasValue) calendarEvent.EventType = dto.EventType.Value;
        if (dto.IsRecurring.HasValue) calendarEvent.IsRecurring = dto.IsRecurring.Value;
        if (dto.Recurrence.HasValue) calendarEvent.Recurrence = dto.Recurrence.Value;

        await _dbContext.SaveChangesAsync(ct);
        return Result.Ok(MapToDto(calendarEvent));
    }

    public async Task<Result> DeleteEventAsync(Guid id, CancellationToken ct = default)
    {
        var filter = OrganizationFilter;
        var query = _dbContext.CalendarEvents.Where(oe => oe.Id == id);

        var calendarEvent = await query.SingleOrDefaultAsync(ct);
        if (calendarEvent is null) return Result.Fail(new NotFoundError("calendar event", id));
        if (filter.Denied || (filter.OrganizationId is { } orgId && calendarEvent.OrganizationId != orgId))
            return Result.Fail(ScopeDenied());

        _dbContext.CalendarEvents.Remove(calendarEvent);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Ok();
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
