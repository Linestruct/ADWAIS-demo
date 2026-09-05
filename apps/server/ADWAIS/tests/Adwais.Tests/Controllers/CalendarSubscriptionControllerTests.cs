// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.Controllers.Calendar;
using Adwais.Application.Common.Errors;
using Adwais.Application.DTOs.Intranet;
using Adwais.Application.Interfaces;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Adwais.Tests.Controllers;

public class CalendarSubscriptionControllerTests
{
    private readonly Mock<ICalendarSubscriptionService> _service = new();
    private readonly CalendarSubscriptionController _controller;

    public CalendarSubscriptionControllerTests()
    {
        _controller = new CalendarSubscriptionController(_service.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task CreateSubscription_ReturnsCreated()
    {
        var request = new CreateCalendarSubscriptionDto("Calendar", "https://example.com/calendar.ics", true);
        var created = new CalendarSubscriptionDto(Guid.NewGuid(), request.Name, request.Url, request.IsActive, null, null, null);
        _service.Setup(service => service.CreateSubscriptionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(created));

        var result = await _controller.CreateSubscription(request, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task DeleteSubscription_ReturnsNotFoundForMissingSubscription()
    {
        var id = Guid.NewGuid();
        _service.Setup(service => service.DeleteSubscriptionAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail(new NotFoundError("calendar subscription", id)));

        var result = await _controller.DeleteSubscription(id, CancellationToken.None);

        var problem = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }
}
