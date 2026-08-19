// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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
                principal.Identity!.IsAuthenticated 
                && principal.IsInRole("Admin")
                && principal.HasClaim(Adwais.Application.Common.Access.AccessClaimTypes.IsPlatformAdmin, "true")),
            It.Is<AuthenticationProperties>(properties =>
                properties.AllowRefresh == false
                && properties.IsPersistent == false
                && properties.ExpiresUtc.HasValue)),
            Times.Once);
    }

    [Fact]
    public void AdminDashboardAuthorizationFilter_AllowsOnlyPlatformAdmin()
    {
        var filter = new Adwais.Api.Filters.AdminDashboardAuthorizationFilter();
        var storage = new Mock<Hangfire.JobStorage>();
        var options = new Hangfire.DashboardOptions();

        var services = new ServiceCollection().BuildServiceProvider();

        // 1. Unauthenticated
        var unauthHttp = new DefaultHttpContext { RequestServices = services };
        var unauthContext = new Hangfire.Dashboard.AspNetCoreDashboardContext(storage.Object, options, unauthHttp);
        Assert.False(filter.Authorize(unauthContext));

        // 2. Org Admin (Role = Admin, but NOT Platform Admin)
        var orgAdminHttp = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(Adwais.Application.Common.Access.AccessClaimTypes.OrganizationId, Guid.NewGuid().ToString())
            ], "Cookie"))
        };
        var orgAdminContext = new Hangfire.Dashboard.AspNetCoreDashboardContext(storage.Object, options, orgAdminHttp);
        Assert.False(filter.Authorize(orgAdminContext));

        // 3. Platform Admin (Role = Admin, IsPlatformAdmin = "true")
        var platformAdminHttp = new DefaultHttpContext
        {
            RequestServices = services,
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(Adwais.Application.Common.Access.AccessClaimTypes.IsPlatformAdmin, "true")
            ], "Cookie"))
        };
        var platformAdminContext = new Hangfire.Dashboard.AspNetCoreDashboardContext(storage.Object, options, platformAdminHttp);
        Assert.True(filter.Authorize(platformAdminContext));
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
