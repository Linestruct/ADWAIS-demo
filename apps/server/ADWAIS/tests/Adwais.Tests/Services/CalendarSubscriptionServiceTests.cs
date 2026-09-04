// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Errors;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Adwais.Domain.Entities.Intranet;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Adwais.Tests.Services;

public class CalendarSubscriptionServiceTests
{
    private DbContextOptions<AnalyticsDbContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task TriggerSyncAsync_SetsEventOrganizationIdFromSubscription()
    {
        var options = CreateNewContextOptions();
        using var dbContext = new AnalyticsDbContext(options);

        var orgId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var subscription = new CalendarSubscription
        {
            Id = subscriptionId,
            Name = "Org Calendar",
            Url = "https://example.com/calendar.ics",
            IsActive = true,
            OrganizationId = orgId
        };
        dbContext.CalendarSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var icsPayload = @"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Example Corp.//EN
BEGIN:VEVENT
UID:event-12345@example.com
DTSTAMP:20260101T000000Z
DTSTART:20260820T090000Z
DTEND:20260820T100000Z
SUMMARY:Board Meeting
DESCRIPTION:Quarterly Review
LOCATION:Conference Room A
END:VEVENT
END:VCALENDAR";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(icsPayload)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var loggerMock = new Mock<ILogger<CalendarSubscriptionService>>();
        var eventServiceMock = new Mock<ISystemEventService>();
        var accessMock = new Mock<ICurrentAccess>();
        accessMock.Setup(a => a.Scope).Returns(new AccessScope(orgId, null, [UserRole.Admin]));

        var service = new CalendarSubscriptionService(
            dbContext,
            httpClient,
            loggerMock.Object,
            eventServiceMock.Object,
            accessMock.Object);

        await service.TriggerSyncAsync(subscriptionId);

        var syncedEvent = await dbContext.CalendarEvents
            .FirstOrDefaultAsync(e => e.CalendarSubscriptionId == subscriptionId);

        Assert.NotNull(syncedEvent);
        Assert.Equal("Board Meeting", syncedEvent.Title);
        Assert.Equal("event-12345@example.com", syncedEvent.ExternalUid);
        Assert.Equal(orgId, syncedEvent.OrganizationId);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_WithoutOrganizationScope_ReturnsScopeDenied()
    {
        var options = CreateNewContextOptions();
        using var dbContext = new AnalyticsDbContext(options);
        var access = new Mock<ICurrentAccess>();
        access.Setup(candidate => candidate.Scope).Returns(new AccessScope(null, null, [UserRole.Admin]));

        var result = await CreateService(dbContext, access.Object).CreateSubscriptionAsync(
            new CreateCalendarSubscriptionDto("Calendar", "https://example.com/calendar.ics", true));

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
    }

    [Fact]
    public async Task DeleteSubscriptionAsync_OutsideOrganizationScope_ReturnsScopeDenied()
    {
        var options = CreateNewContextOptions();
        using var dbContext = new AnalyticsDbContext(options);
        var subscription = new CalendarSubscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Other calendar",
            Url = "https://example.com/calendar.ics",
            IsActive = true
        };
        dbContext.CalendarSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var access = new Mock<ICurrentAccess>();
        access.Setup(candidate => candidate.Scope).Returns(new AccessScope(Guid.NewGuid(), null, [UserRole.Admin]));
        var result = await CreateService(dbContext, access.Object).DeleteSubscriptionAsync(subscription.Id);

        Assert.True(result.IsFailed);
        Assert.IsType<ScopeDeniedError>(Assert.Single(result.Errors));
    }

    private static CalendarSubscriptionService CreateService(AnalyticsDbContext dbContext, ICurrentAccess access)
        => new(dbContext, new HttpClient(), new Mock<ILogger<CalendarSubscriptionService>>().Object,
            new Mock<ISystemEventService>().Object, access);
}
