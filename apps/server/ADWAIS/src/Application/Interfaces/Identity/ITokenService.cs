// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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
    /// <param name="role">The role claim to assign to the token.</param>
    /// <param name="organizationId">The organization claim to embed in the token, when known.</param>
    /// <returns>A signed JWT token string.</returns>
    string GenerateKioskToken(string deviceId, string role = "Viewer", Guid? organizationId = null);
}
