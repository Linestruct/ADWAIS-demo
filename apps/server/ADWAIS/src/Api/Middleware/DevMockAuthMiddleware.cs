// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Adwais.Application.Interfaces;

namespace Adwais.Api.Middleware;

public class DevMockAuthMiddleware(RequestDelegate next, IWebHostEnvironment env)
{
    private readonly RequestDelegate _next = next;
    private readonly IWebHostEnvironment _env = env;

    public async Task InvokeAsync(HttpContext context)
    {
        if (_env.IsDevelopment() && !context.Request.Headers.ContainsKey("Authorization"))
        {
            var tokenService = context.RequestServices.GetRequiredService<ITokenService>();
            var token = tokenService.GenerateKioskToken("00000000-0000-0000-0000-000000000002", "Admin", isPlatformAdmin: true);
            context.Request.Headers.Authorization = $"Bearer {token}";
        }

        await _next(context);
    }
}
