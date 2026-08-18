// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Security.Claims;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Adwais.Api.Middleware;

/// <summary>
/// Development only. Fabricates a platform admin principal when no
/// Authorization header is present, using the same claim builder as the
/// production claims transformation.
/// </summary>
public class DevMockAuthMiddleware(
    RequestDelegate next,
    IWebHostEnvironment env,
    ILogger<DevMockAuthMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly IWebHostEnvironment _env = env;
    private readonly ILogger<DevMockAuthMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var hasAuthHeader = context.Request.Headers.ContainsKey("Authorization");
        _logger.LogInformation(
            "DevMockAuth: environment={Environment}, hasAuthHeader={HasAuthHeader}",
            _env.EnvironmentName,
            hasAuthHeader);

        if (_env.IsDevelopment() && !hasAuthHeader)
        {
            var identity = AccessClaimsBuilder.Build(
                AnalyticsDbContext.SystemUserGuid,
                new AccessScope(null, null, [UserRole.Admin]));
            context.User = new ClaimsPrincipal(identity);
            _logger.LogInformation("DevMockAuth: injected platform admin principal");
        }

        await _next(context);
    }
}
