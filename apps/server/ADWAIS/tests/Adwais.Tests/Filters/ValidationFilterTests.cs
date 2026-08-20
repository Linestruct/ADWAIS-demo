// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Tenants;
using Adwais.Api.Filters;
using Adwais.Api.Validators.GlobalConfig;
using Adwais.Api.Validators.Tenants;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Adwais.Tests.Filters;

public class ValidationFilterTests
{
    [Fact]
    public async Task InvalidTenantProvider_ReturnsBadRequestBeforeTheActionRuns()
    {
        var source = new Mock<IOrderSource>();
        source.SetupGet(x => x.Provider).Returns("litium");

        var context = CreateContext(
            new CreateTenantRequestDto { Name = "Tenant", OrderProvider = "unsupported" },
            new CreateTenantRequestDtoValidator([source.Object]));

        var actionRan = await ExecuteAsync(context);

        Assert.False(actionRan);
        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task InvalidTenantProviderOnPatch_ReturnsBadRequestBeforeTheActionRuns()
    {
        var source = new Mock<IOrderSource>();
        source.SetupGet(x => x.Provider).Returns("litium");

        var context = CreateContext(
            new UpdateTenantRequestDto { OrderProvider = "unsupported" },
            new UpdateTenantRequestDtoValidator([source.Object]));

        Assert.False(await ExecuteAsync(context));
        Assert.IsType<BadRequestObjectResult>(context.Result);
    }

    [Fact]
    public async Task InvalidMonitoringProvider_ReturnsBadRequestBeforeTheActionRuns()
    {
        var provider = new Mock<IMonitoringProvider>();
        provider.SetupGet(x => x.Provider).Returns("uptimerobot");

        var context = CreateContext(
            new UpdateGlobalConfigRequestDto(MonitoringProvider: "unsupported"),
            new UpdateGlobalConfigRequestDtoValidator([provider.Object]));

        Assert.False(await ExecuteAsync(context));
        Assert.IsType<BadRequestObjectResult>(context.Result);
    }

    private static ActionExecutingContext CreateContext<T>(T request, IValidator<T> validator)
    {
        var services = new ServiceCollection()
            .AddSingleton(validator)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor { Parameters = [new ParameterDescriptor { Name = "request" }] });

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["request"] = request },
            new object());
    }

    private static async Task<bool> ExecuteAsync(ActionExecutingContext context)
    {
        var actionRan = false;
        await new ValidationFilter().OnActionExecutionAsync(context, () =>
        {
            actionRan = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        });
        return actionRan;
    }
}
