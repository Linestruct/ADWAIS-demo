// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Threading;
using System.Threading.Tasks;

namespace Adwais.Application.Interfaces;

/// <summary>
/// Manages registration, activation (authorization by staff), and JWT retrieval lifecycle for Kiosk display devices.
/// </summary>
public interface IKioskService
{
    /// <summary>
    /// Registers a new kiosk device and generates a temporary, high-entropy activation code.
    /// If the device was already registered, updates its activation code and resets authorization status.
    /// </summary>
    /// <param name="deviceId">The unique device identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The generated activation code.</returns>
    Task<string> RegisterDeviceAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Activates a registered kiosk device by verifying and matching a valid, non-expired activation code.
    /// The device joins the given organization.
    /// </summary>
    /// <param name="activationCode">The case-insensitive code displayed on the kiosk screen.</param>
    /// <param name="organizationId">The organization the activating staff member belongs to.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if the activation was successful, otherwise false.</returns>
    Task<bool> ActivateDeviceAsync(string activationCode, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a valid 30-day JWT bearer token for the kiosk device if it is authorized.
    /// </summary>
    /// <param name="deviceId">The unique device identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The generated JWT local bearer token, or null if the device is unauthorized or not found.</returns>
    Task<string?> GetTokenAsync(string deviceId, CancellationToken ct = default);
}
