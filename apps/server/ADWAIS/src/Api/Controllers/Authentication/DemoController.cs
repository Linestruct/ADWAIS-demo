// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Api.DTOs.Kiosk;
using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Authentication;

/// <summary>
/// Provides the optional public demo access token.
/// </summary>
[ApiController]
[Route("api/demo")]
public class DemoController(ITokenService tokenService, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Returns a Viewer kiosk token when public demo access is enabled.
    /// </summary>
    /// <remarks>
    /// Demo access is controlled by <c>Authentication:EnableDemoAccess</c>. The endpoint is
    /// intentionally anonymous; the returned token is read-only and cannot perform mutations.
    /// </remarks>
    [HttpGet("token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(KioskTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<KioskTokenResponseDto> GetDemoToken()
    {
        if (!configuration.GetValue<bool>("Authentication:EnableDemoAccess"))
        {
            return NotFound();
        }

        var token = tokenService.GenerateKioskToken("demo-visitor", "Viewer", AnalyticsDbContext.DefaultOrganizationGuid);
        return Ok(new KioskTokenResponseDto { Token = token, ExpiresInHours = 1 });
    }
}
