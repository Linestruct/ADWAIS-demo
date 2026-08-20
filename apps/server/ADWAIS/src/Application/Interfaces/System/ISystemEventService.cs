// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;

namespace Adwais.Application.Interfaces;

/// <summary>
/// Provides a service for logging system events to the database and standard logging.
/// </summary>
public interface ISystemEventService
{
    /// <summary>
    /// Logs a system event with a specific level, source, and message.
    /// </summary>
    /// <param name="source">The source of the event (e.g., service name).</param>
    /// <param name="message">The main message for the event.</param>
    /// <param name="level">The severity level of the event.</param>
    /// <param name="details">Optional detailed information or stack trace.</param>
    /// <param name="tenantId">Optional tenant ID associated with the event.</param>
    Task LogAsync(string source, string message, SystemEventLevel level = SystemEventLevel.Information, string? details = null, Guid? tenantId = null);

    /// <summary>
    /// Logs a warning event.
    /// </summary>
    /// <param name="source">The source of the event.</param>
    /// <param name="message">The main message.</param>
    /// <param name="details">Optional detailed information.</param>
    /// <param name="tenantId">Optional tenant ID.</param>
    Task LogWarningAsync(string source, string message, string? details = null, Guid? tenantId = null);

    /// <summary>
    /// Logs an error event, including exception details.
    /// </summary>
    /// <param name="source">The source of the event.</param>
    /// <param name="message">The main message.</param>
    /// <param name="ex">Optional exception that occurred.</param>
    /// <param name="tenantId">Optional tenant ID.</param>
    Task LogErrorAsync(string source, string message, Exception? ex = null, Guid? tenantId = null);
}
