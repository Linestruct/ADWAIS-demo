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

namespace Adwais.Api.Middleware;

/// <summary>
/// Development only. Fabricates a platform admin principal when no
/// Authorization header is present, using the same claim builder as the
/// production claims transformation.
/// </summary>
public class DevMockAuthMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    private readonly RequestDelegate _next = next;
    private readonly IWebHostEnvironment _env = env;

    public async Task InvokeAsync(HttpContext context)
    {
        if (_env.IsDevelopment() && !context.Request.Headers.ContainsKey("Authorization"))
        {
            var identity = AccessClaimsBuilder.Build(
                AnalyticsDbContext.SystemUserGuid,
                new AccessScope(null, null, [UserRole.Admin]));
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}
