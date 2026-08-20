// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Interfaces;

/// <summary>
/// Provides capabilities to generate locally signed JSON Web Tokens (JWT) for kiosk devices.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a locally signed 30-day JWT token for a kiosk display device with the "Viewer" role.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the kiosk device.</param>
    /// <returns>A signed JWT token string.</returns>
    /// <param name="role">The role claim to assign to the token.</param>
    string GenerateKioskToken(string deviceId, string role = "Viewer");
}
