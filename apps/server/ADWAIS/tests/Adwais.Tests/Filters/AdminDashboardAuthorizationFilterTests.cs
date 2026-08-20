// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Reflection;
using System.Security.Claims;
using Adwais.Api.Filters;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Adwais.Tests.Filters;

public class AdminDashboardAuthorizationFilterTests
{
    [Fact]
    public void RejectsUnauthenticatedUsers()
    {
        Assert.False(new AdminDashboardAuthorizationFilter().Authorize(CreateDashboardContext(new ClaimsPrincipal())));
    }

    [Fact]
    public void RejectsAuthenticatedNonAdmins()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "Viewer")], "test");
        Assert.False(new AdminDashboardAuthorizationFilter().Authorize(CreateDashboardContext(new ClaimsPrincipal(identity))));
    }

    [Fact]
    public void AllowsAuthenticatedAdmins()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "test");
        Assert.True(new AdminDashboardAuthorizationFilter().Authorize(CreateDashboardContext(new ClaimsPrincipal(identity))));
    }

    private static DashboardContext CreateDashboardContext(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext
        {
            User = user,
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        var assembly = Assembly.Load("Hangfire.AspNetCore");
        var contextType = assembly.GetTypes().Single(type => type.Name == "AspNetCoreDashboardContext");
        var constructor = contextType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(candidate => candidate.GetParameters().Any(parameter => parameter.ParameterType == typeof(HttpContext)));
        var arguments = constructor.GetParameters().Select(parameter =>
        {
            if (parameter.ParameterType == typeof(HttpContext)) return (object)httpContext;
            if (parameter.ParameterType == typeof(JobStorage)) return new Mock<JobStorage>().Object;
            if (parameter.ParameterType == typeof(DashboardOptions)) return new DashboardOptions();
            throw new InvalidOperationException($"Unsupported dashboard context parameter: {parameter.ParameterType}");
        }).ToArray();

        return (DashboardContext)constructor.Invoke(arguments);
    }
}
