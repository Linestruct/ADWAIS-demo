// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Security.Claims;
using Adwais.Api.Extensions;
using Adwais.Application.Common.Access;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Authentication;

/// <summary>
/// Exchanges an authenticated Admin API request for a short-lived Hangfire dashboard session.
/// </summary>
[ApiController]
[Route("api/dashboard-session")]
public class DashboardSessionController : ControllerBase
{
    /// <summary>
    /// Creates the HttpOnly dashboard cookie used by the server-rendered Hangfire UI.
    /// </summary>
    /// <remarks>
    /// The SPA sends its OIDC or kiosk bearer token to this endpoint. Browser navigation to
    /// <c>/hangfire</c> then uses the five-minute cookie because normal navigations do not carry
    /// the SPA's Authorization header.
    /// </remarks>
    [HttpPost]
    [Authorize(Policy = "PlatformAdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create()
    {
        // Build the cookie identity through the shared claims builder so the
        // claims transformation recognizes it as locally authoritative.
        var identity = AccessClaimsBuilder.Build(
            AnalyticsDbContext.SystemUserGuid,
            new AccessScope(null, null, [UserRole.PlatformAdmin]));
        identity.AddClaim(new Claim(ClaimTypes.Name, User.Identity?.Name ?? "PlatformAdmin"));

        await HttpContext.SignInAsync(
            AuthenticationExtensions.DashboardCookieScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                AllowRefresh = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                IsPersistent = false
            });

        return NoContent();
    }

    /// <summary>
    /// Clears the current browser's Hangfire dashboard session, if one exists.
    /// </summary>
    [HttpDelete]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete()
    {
        await HttpContext.SignOutAsync(AuthenticationExtensions.DashboardCookieScheme);
        return NoContent();
    }
}
