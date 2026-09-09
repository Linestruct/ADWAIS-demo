// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Exceptions;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services;

public class CalendarFeedService(IApplicationDbContext dbContext) : ICalendarFeedService
{
    private readonly IApplicationDbContext _dbContext = dbContext;

    public async Task<string> GetUserCalendarFeedTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) throw new HttpContractException(404, "Not Found", "User not found.");

        if (string.IsNullOrEmpty(user.CalendarFeedToken))
        {
            user.CalendarFeedToken = GenerateSecureToken();
            await _dbContext.SaveChangesAsync(ct);
        }

        return user.CalendarFeedToken;
    }

    public async Task<string> RegenerateUserCalendarFeedTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users.FindAsync(new object[] { userId }, ct);
        if (user == null) throw new HttpContractException(404, "Not Found", "User not found.");

        user.CalendarFeedToken = GenerateSecureToken();
        await _dbContext.SaveChangesAsync(ct);

        return user.CalendarFeedToken;
    }

    public async Task<byte[]> GenerateIcsFeedAsync(string feedToken, CancellationToken ct = default)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.CalendarFeedToken == feedToken, ct);
        if (user == null)
        {
            throw new HttpContractException(403, "Forbidden", "Invalid calendar feed token.");
        }

        var memberships = await _dbContext.UserAccesses
            .AsNoTracking()
            .Where(access => access.UserId == user.Id)
            .ToListAsync(ct);
        var scope = AccessScopeResolver.SelectEffective(
            AccessScopeResolver.ResolveAllowed(memberships),
            null,
            null);
        var filter = OrganizationFilter.From(scope);
        if (filter.Denied || filter.OrganizationId is null)
        {
            throw new HttpContractException(403, "Forbidden", "The calendar feed user has no organization scope.");
        }

        // Fetch the organization's events for the feed
        var events = await _dbContext.CalendarEvents
            .Where(oe => oe.OrganizationId == filter.OrganizationId)
            .OrderBy(oe => oe.StartTime)
            .ToListAsync(ct);

        var calendar = new Calendar();
        calendar.ProductId = "-//ADWAIS//Intranet Calendar//EN";

        foreach (var domainEvent in events)
        {
            var icalEvent = new Ical.Net.CalendarComponents.CalendarEvent
            {
                Uid = domainEvent.ExternalUid ?? domainEvent.Id.ToString(),
                Summary = domainEvent.Title,
                Description = domainEvent.Description,
                Location = domainEvent.Location,
                Start = new CalDateTime(domainEvent.StartTime.UtcDateTime),
                End = new CalDateTime(domainEvent.EndTime.UtcDateTime)
            };

            // Set categories/types
            icalEvent.Categories.Add(domainEvent.EventType.ToString());

            calendar.Events.Add(icalEvent);
        }

        var serializer = new CalendarSerializer();
        var icsString = serializer.SerializeToString(calendar)
            ?? throw new InvalidOperationException("Failed to serialize the calendar feed.");
        return Encoding.UTF8.GetBytes(icsString);
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToHexString(randomBytes).ToLowerInvariant();
    }
}
