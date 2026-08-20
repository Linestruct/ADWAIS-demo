// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Api.DTOs.Kiosk;
using Adwais.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Adwais.Api.Controllers.Authentication;

/// <summary>
/// Handles authentication lifecycle for kiosk display devices, including registration,
/// activation (authorization by staff), and JWT token retrieval.
/// </summary>
[ApiController]
[Route("api/kiosk")]
public class KioskAuthController(IKioskService kioskService) : ControllerBase
{
    /// <summary>
    /// Registers a new kiosk device and generates a temporary case-insensitive activation code.
    /// </summary>
    /// <param name="request">The registration request containing the device identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A DTO containing the generated activation code.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterKioskRequestDto request, CancellationToken ct)
    {
        var code = await kioskService.RegisterDeviceAsync(request.DeviceId, ct);
        return Ok(new RegisterKioskResponseDto { ActivationCode = code });
    }

    /// <summary>
    /// Authorizes a registered kiosk device using its pending activation code.
    /// Restricted to administrators and employees.
    /// </summary>
    /// <param name="request">The activation request containing the code to authorize.</param>
    /// <returns>HTTP 200 OK on success, or HTTP 400 Bad Request if the code is invalid or expired.</returns>
    [HttpPost("activate")]
    [Authorize(Policy = "StaffAccess")]
    public async Task<IActionResult> Activate([FromBody] ActivateKioskRequestDto request)
    {
        var activated = await kioskService.ActivateDeviceAsync(request.ActivationCode);
        if (!activated)
        {
            return BadRequest("Activation code has expired or is invalid.");
        }
        return Ok();
    }

    /// <summary>
    /// Retrieves a valid 30-day JWT local token for an authorized kiosk device.
    /// </summary>
    /// <param name="deviceId">The unique device identifier.</param>
    /// <returns>The kiosk token response containing the JWT bearer token.</returns>
    [HttpGet("token")]
    public async Task<IActionResult> GetToken([FromQuery] string deviceId)
    {
        var token = await kioskService.GetTokenAsync(deviceId);
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized("Kiosk device is not authorized.");
        }
        return Ok(new KioskTokenResponseDto { Token = token, ExpiresInDays = 30 });
    }

    /// <summary>
    /// Developer endpoint to generate an Admin token directly via Swagger.
    /// Requires the exact KioskJwtSecret to be provided.
    /// </summary>
    [HttpPost("swagger-admin-token")]
    [AllowAnonymous]
    public IActionResult GenerateSwaggerAdminToken([FromBody] string secret, [FromServices] IConfiguration config, [FromServices] ITokenService tokenService, [FromServices] IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
        {
            return NotFound();
        }

        var configuredSecret = config["Authentication:KioskJwtSecret"];
        if (string.IsNullOrEmpty(secret) || secret != configuredSecret)
        {
            return Unauthorized("Invalid secret.");
        }
        
        var token = tokenService.GenerateKioskToken("swagger-admin", "Admin");
        return Ok(new KioskTokenResponseDto { Token = token, ExpiresInDays = 30 });
    }
}
