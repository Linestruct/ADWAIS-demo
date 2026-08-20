// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Adwais.Domain.Enums;
using Ical.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Adwais.Infrastructure.Services;

public class CalendarSubscriptionService(
    IApplicationDbContext dbContext,
    HttpClient httpClient,
    ILogger<CalendarSubscriptionService> logger,
    ISystemEventService eventService)
    : ICalendarSubscriptionService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<CalendarSubscriptionService> _logger = logger;
    private readonly ISystemEventService _eventService = eventService;

    public async Task<CalendarSubscriptionDto?> GetSubscriptionByIdAsync(Guid id, CancellationToken ct = default)
    {
        var sub = await _dbContext.CalendarSubscriptions.FindAsync(new object[] { id }, ct);
        if (sub == null) return null;
        return MapToDto(sub);
    }

    public async Task<IEnumerable<CalendarSubscriptionDto>> GetSubscriptionsAsync(CancellationToken ct = default)
    {
        var subs = await _dbContext.CalendarSubscriptions.OrderBy(s => s.Name).ToListAsync(ct);
        return subs.Select(MapToDto);
    }

    public async Task<CalendarSubscriptionDto> CreateSubscriptionAsync(CreateCalendarSubscriptionDto dto, CancellationToken ct = default)
    {
        var sub = new CalendarSubscription
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Url = dto.Url,
            IsActive = dto.IsActive
        };

        _dbContext.CalendarSubscriptions.Add(sub);
        await _dbContext.SaveChangesAsync(ct);
        return MapToDto(sub);
    }

    public async Task<CalendarSubscriptionDto?> UpdateSubscriptionAsync(Guid id, UpdateCalendarSubscriptionDto dto, CancellationToken ct = default)
    {
        var sub = await _dbContext.CalendarSubscriptions.FindAsync(new object[] { id }, ct);
        if (sub == null) return null;

        if (dto.Name != null) sub.Name = dto.Name;
        if (dto.Url != null) sub.Url = dto.Url;
        if (dto.IsActive.HasValue) sub.IsActive = dto.IsActive.Value;

        await _dbContext.SaveChangesAsync(ct);
        return MapToDto(sub);
    }

    public async Task<bool> DeleteSubscriptionAsync(Guid id, CancellationToken ct = default)
    {
        var sub = await _dbContext.CalendarSubscriptions.FindAsync(new object[] { id }, ct);
        if (sub == null) return false;

        _dbContext.CalendarSubscriptions.Remove(sub);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task TriggerSyncAsync(Guid id, CancellationToken ct = default)
    {
        var sub = await _dbContext.CalendarSubscriptions.FindAsync(new object[] { id }, ct);
        if (sub == null || !sub.IsActive) return;

        try
        {
            _logger.LogInformation("Starting calendar subscription sync for {Name} ({Url})", sub.Name, sub.Url);
            sub.LastPolledAt = DateTime.UtcNow;

            var icsContent = await _httpClient.GetStringAsync(sub.Url, ct);
            var calendar = Calendar.Load(icsContent)
                ?? throw new InvalidDataException("The calendar subscription returned invalid iCalendar data.");

            var incomingUids = new HashSet<string>();

            foreach (var calendarEvent in calendar.Events)
            {
                var externalUid = calendarEvent.Uid;
                if (string.IsNullOrEmpty(externalUid)) continue;

                if (calendarEvent.Start is null)
                {
                    _logger.LogWarning(
                        "Skipping calendar event {Uid} from subscription {Name} because it has no start time",
                        externalUid,
                        sub.Name);
                    continue;
                }

                incomingUids.Add(externalUid);

                var existingEvent = await _dbContext.CalendarEvents
                    .FirstOrDefaultAsync(oe => oe.CalendarSubscriptionId == sub.Id && oe.ExternalUid == externalUid, ct);

                var startTimeVal = calendarEvent.Start.Value;
                var startTime = startTimeVal.Kind == DateTimeKind.Utc
                    ? new DateTimeOffset(startTimeVal)
                    : new DateTimeOffset(startTimeVal.ToUniversalTime());

                var endTimeVal = calendarEvent.End?.Value ?? startTimeVal;
                var endTime = endTimeVal.Kind == DateTimeKind.Utc
                    ? new DateTimeOffset(endTimeVal)
                    : new DateTimeOffset(endTimeVal.ToUniversalTime());

                if (existingEvent != null)
                {
                    existingEvent.Title = calendarEvent.Summary ?? "Untitled Event";
                    existingEvent.Description = calendarEvent.Description;
                    existingEvent.Location = calendarEvent.Location;
                    existingEvent.StartTime = startTime;
                    existingEvent.EndTime = endTime;
                    existingEvent.IsRecurring = calendarEvent.RecurrenceRule is not null;
                }
                else
                {
                    var newEvent = new CalendarEvent
                    {
                        Id = Guid.NewGuid(),
                        Title = calendarEvent.Summary ?? "Untitled Event",
                        Description = calendarEvent.Description,
                        Location = calendarEvent.Location,
                        StartTime = startTime,
                        EndTime = endTime,
                        EventType = EventType.ExternalSync,
                        IsRecurring = calendarEvent.RecurrenceRule is not null,
                        Recurrence = RecurrenceType.None,
                        ExternalUid = externalUid,
                        CalendarSubscriptionId = sub.Id
                    };
                    _dbContext.CalendarEvents.Add(newEvent);
                }
            }

            // Remove events that are no longer in the external feed
            var eventsToRemove = await _dbContext.CalendarEvents
                .Where(oe => oe.CalendarSubscriptionId == sub.Id && oe.ExternalUid != null && !incomingUids.Contains(oe.ExternalUid))
                .ToListAsync(ct);

            _dbContext.CalendarEvents.RemoveRange(eventsToRemove);

            sub.LastSuccessAt = DateTime.UtcNow;
            sub.LastSyncError = null;

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Successfully completed calendar sync for {Name}", sub.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed sync for calendar subscription {Name}", sub.Name);
            sub.LastSyncError = ex.Message;
            
            // Scoped with CancellationToken.None to survive cancel timeouts
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            await _eventService.LogErrorAsync(
                nameof(CalendarSubscriptionService),
                $"Calendar sync failed for {sub.Name}: {ex.Message}",
                ex);
        }
    }

    public async Task TriggerAllSyncsAsync(CancellationToken ct = default)
    {
        List<Guid> activeSubIds;
        activeSubIds = await _dbContext.CalendarSubscriptions
            .Where(s => s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(ct);

        foreach (var subId in activeSubIds)
        {
            await TriggerSyncAsync(subId, ct);
        }
    }

    private static CalendarSubscriptionDto MapToDto(CalendarSubscription cs)
    {
        return new CalendarSubscriptionDto(
            cs.Id,
            cs.Name,
            cs.Url,
            cs.IsActive,
            cs.LastPolledAt,
            cs.LastSuccessAt,
            cs.LastSyncError
        );
    }
}
