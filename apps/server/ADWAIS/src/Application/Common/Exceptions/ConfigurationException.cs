// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.Common.Exceptions;

/// <summary>
/// The request is valid, but the required configuration is missing or
/// inconsistent. Maps to 409 Conflict by the global exception handler.
/// </summary>
public sealed class ConfigurationException(string message) : Exception(message);