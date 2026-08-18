// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Application.Common.Exceptions;

/// <summary>
/// The request is valid, but the required configuration is missing or
/// inconsistent. Maps to 409 Conflict by the global exception handler.
/// </summary>
public sealed class ConfigurationException(string message) : Exception(message);