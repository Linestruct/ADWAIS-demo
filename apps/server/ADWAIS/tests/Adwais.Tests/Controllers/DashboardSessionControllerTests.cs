// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Security.Claims;
using Adwais.Api.Controllers.Authentication;
using Adwais.Api.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Adwais.Tests.Controllers;

public class DashboardSessionControllerTests
{
    [Fact]
    public async Task Create_IssuesShortLivedAdminCookie()
    {
        var authentication = new Mock<IAuthenticationService>();
        var context = CreateHttpContext(authentication);
        var controller = new DashboardSessionController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = await controller.Create();

        Assert.IsType<NoContentResult>(result);
        authentication.Verify(service => service.SignInAsync(
            context,
            AuthenticationExtensions.DashboardCookieScheme,
            It.Is<ClaimsPrincipal>(principal =>
                principal.Identity!.IsAuthenticated && principal.IsInRole("Admin")),
            It.Is<AuthenticationProperties>(properties =>
                properties.AllowRefresh == false
                && properties.IsPersistent == false
                && properties.ExpiresUtc.HasValue)),
            Times.Once);
    }

    [Fact]
    public async Task Delete_RemovesDashboardCookie()
    {
        var authentication = new Mock<IAuthenticationService>();
        var context = CreateHttpContext(authentication);
        var controller = new DashboardSessionController
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = await controller.Delete();

        Assert.IsType<NoContentResult>(result);
        authentication.Verify(service => service.SignOutAsync(
            context,
            AuthenticationExtensions.DashboardCookieScheme,
            null),
            Times.Once);
    }

    private static DefaultHttpContext CreateHttpContext(Mock<IAuthenticationService> authentication)
    {
        var services = new ServiceCollection()
            .AddSingleton(authentication.Object)
            .BuildServiceProvider();
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "Admin User"),
                new Claim(ClaimTypes.Role, "Admin")
            ],
            "Bearer");

        return new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(identity)
        };
    }
}
