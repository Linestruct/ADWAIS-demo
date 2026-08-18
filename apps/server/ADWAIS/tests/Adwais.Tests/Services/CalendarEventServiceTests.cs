// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.Intranet;
using Adwais.Domain.Entities.Intranet;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Application.Common.Access;
using Moq;
using Adwais.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Adwais.Tests.Services;

public class CalendarEventServiceTests
{
    private DbContextOptions<AnalyticsDbContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetEventsAsync_MonthlyRecurrence_ClampsWithoutDriftingAndKeepsSeriesId()
    {
        var options = CreateNewContextOptions();
        using var dbContext = new AnalyticsDbContext(options);
        var calendarEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Month end",
            StartTime = new DateTimeOffset(2025, 1, 31, 10, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2025, 1, 31, 11, 0, 0, TimeSpan.Zero),
            EventType = EventType.Meeting,
            IsRecurring = true,
            Recurrence = RecurrenceType.Monthly
        };
        dbContext.CalendarEvents.Add(calendarEvent);
        await dbContext.SaveChangesAsync();

        var result = (await new CalendarEventService(dbContext, PlatformAccess()).GetEventsAsync(
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 3, 31, 23, 59, 59, TimeSpan.Zero))).ToArray();

        Assert.Equal(new[] { 31, 28, 31 }, result.Select(e => e.StartTime.Day));
        Assert.All(result, occurrence => Assert.Equal(calendarEvent.Id, occurrence.Id));
    }

    [Fact]
    public async Task UpdateEventAsync_WithValidTimes_UpdatesSuccessfully()
    {
        // Arrange
        var options = CreateNewContextOptions();
        using (var dbContext = new AnalyticsDbContext(options))
        {
            var calendarEvent = new CalendarEvent
            {
                Id = Guid.NewGuid(),
                Title = "Original Title",
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow.AddHours(1),
                EventType = EventType.Meeting
            };
            dbContext.CalendarEvents.Add(calendarEvent);
            await dbContext.SaveChangesAsync();

            var service = new CalendarEventService(dbContext, PlatformAccess());
            var updatedStartTime = DateTimeOffset.UtcNow.AddHours(2);
            var updatedEndTime = DateTimeOffset.UtcNow.AddHours(3);
            var dto = new UpdateCalendarEventDto(
                Title: "Updated Title",
                Description: null,
                Location: null,
                StartTime: updatedStartTime,
                EndTime: updatedEndTime,
                EventType: null,
                IsRecurring: null,
                Recurrence: null
            );

            // Act
            var result = await service.UpdateEventAsync(calendarEvent.Id, dto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal(updatedStartTime.ToUniversalTime(), result.StartTime);
            Assert.Equal(updatedEndTime.ToUniversalTime(), result.EndTime);
        }
    }

    [Fact]
    public async Task UpdateEventAsync_WithEndTimeLessThanStartTime_ThrowsArgumentException()
    {
        // Arrange
        var options = CreateNewContextOptions();
        using (var dbContext = new AnalyticsDbContext(options))
        {
            var calendarEvent = new CalendarEvent
            {
                Id = Guid.NewGuid(),
                Title = "Original Title",
                StartTime = DateTimeOffset.UtcNow,
                EndTime = DateTimeOffset.UtcNow.AddHours(1),
                EventType = EventType.Meeting
            };
            dbContext.CalendarEvents.Add(calendarEvent);
            await dbContext.SaveChangesAsync();

            var service = new CalendarEventService(dbContext, PlatformAccess());
            var updatedStartTime = DateTimeOffset.UtcNow.AddHours(2);
            var updatedEndTime = DateTimeOffset.UtcNow.AddHours(1); // Less than StartTime
            var dto = new UpdateCalendarEventDto(
                Title: null,
                Description: null,
                Location: null,
                StartTime: updatedStartTime,
                EndTime: updatedEndTime,
                EventType: null,
                IsRecurring: null,
                Recurrence: null
            );

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UpdateEventAsync(calendarEvent.Id, dto, CancellationToken.None));

            Assert.Equal("End time must be greater than or equal to start time.", exception.Message);
        }
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsOnlyOrganizationEvents()
    {
        var options = CreateNewContextOptions();
        using var dbContext = new AnalyticsDbContext(options);
        var ownOrgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();

        var ownEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Own event",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            EventType = EventType.Meeting,
            OrganizationId = ownOrgId
        };
        var otherEvent = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            Title = "Other event",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            EventType = EventType.Meeting,
            OrganizationId = otherOrgId
        };
        dbContext.CalendarEvents.AddRange(ownEvent, otherEvent);
        await dbContext.SaveChangesAsync();

        var orgMock = new Mock<ICurrentAccess>();
        orgMock.Setup(access => access.Scope).Returns(new AccessScope(ownOrgId, null, [UserRole.Admin]));

        var result = (await new CalendarEventService(dbContext, orgMock.Object).GetEventsAsync(
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddHours(2))).ToArray();

        Assert.Single(result);
        Assert.Equal("Own event", result[0].Title);
    }

    private static ICurrentAccess PlatformAccess()
    {
        var mock = new Mock<ICurrentAccess>();
        mock.Setup(access => access.Scope).Returns(new AccessScope(null, null, [UserRole.Admin]));
        return mock.Object;
    }
}
