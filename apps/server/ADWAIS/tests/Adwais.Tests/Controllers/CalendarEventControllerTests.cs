// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.Controllers.Calendar;
using Adwais.Application.Common.Errors;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using Adwais.Domain.Enums;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Adwais.Tests.Controllers;

public class CalendarEventControllerTests
{
    private readonly Mock<ICalendarEventService> _service = new();
    private readonly CalendarEventController _controller;

    public CalendarEventControllerTests()
    {
        _controller = new CalendarEventController(_service.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task CreateEvent_ReturnsCreated()
    {
        var dto = CreateDto();
        var created = new CalendarEventDto(Guid.NewGuid(), dto.Title, dto.Description, dto.Location,
            dto.StartTime, dto.EndTime, dto.EventType, dto.IsRecurring, dto.Recurrence,
            null, null, null, null);
        _service.Setup(service => service.CreateEventAsync(null, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(created));

        var result = await _controller.CreateEvent(dto, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task UpdateEvent_ReturnsForbiddenForDeniedScope()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateCalendarEventDto(null, null, null, null, null, null, null, null);
        _service.Setup(service => service.UpdateEventAsync(id, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<CalendarEventDto>(new ScopeDeniedError("an organization scope", "none")));

        var result = await _controller.UpdateEvent(id, dto, CancellationToken.None);

        var problem = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, problem.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ReturnsNotFoundForMissingEvent()
    {
        var id = Guid.NewGuid();
        _service.Setup(service => service.DeleteEventAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(new NotFoundError("calendar event", id)));

        var result = await _controller.DeleteEvent(id, CancellationToken.None);

        var problem = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    private static CreateCalendarEventDto CreateDto() => new(
        "Event", null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1),
        EventType.Meeting, false, RecurrenceType.None);
}
