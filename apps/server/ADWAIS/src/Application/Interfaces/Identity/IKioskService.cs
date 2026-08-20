// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

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
    /// </summary>
    /// <param name="activationCode">The case-insensitive code displayed on the kiosk screen.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if the activation was successful, otherwise false.</returns>
    Task<bool> ActivateDeviceAsync(string activationCode, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a valid 30-day JWT bearer token for the kiosk device if it is authorized.
    /// </summary>
    /// <param name="deviceId">The unique device identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The generated JWT local bearer token, or null if the device is unauthorized or not found.</returns>
    Task<string?> GetTokenAsync(string deviceId, CancellationToken ct = default);
}
