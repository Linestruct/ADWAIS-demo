// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Adwais.Application.Interfaces;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Implementation of ISystemEventService that persists events to the database and logs them using ILogger.
/// </summary>
public class SystemEventService(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    ILogger<SystemEventService> logger) : ISystemEventService
{
    /// <inheritdoc />
    public async Task LogAsync(string source, string message, SystemEventLevel level = SystemEventLevel.Information, string? details = null, Guid? tenantId = null)
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();
            var systemEvent = new SystemEvent
            {
                Source = source,
                Message = message,
                Level = level,
                Details = details,
                TenantId = tenantId
            };

            db.SystemEvents.Add(systemEvent);
            await db.SaveChangesAsync();
            
            var logLevel = level switch
            {
                SystemEventLevel.Information => LogLevel.Information,
                SystemEventLevel.Warning => LogLevel.Warning,
                SystemEventLevel.Error => LogLevel.Error,
                SystemEventLevel.Critical => LogLevel.Critical,
                _ => LogLevel.Information
            };
            
            logger.Log(logLevel, "[{Source}] {Message} (Tenant: {TenantId})", source, message, tenantId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist SystemEvent from {Source}: {Message}", source, message);
        }
    }

    /// <inheritdoc />
    public Task LogWarningAsync(string source, string message, string? details = null, Guid? tenantId = null)
        => LogAsync(source, message, SystemEventLevel.Warning, details, tenantId);

    /// <inheritdoc />
    public Task LogErrorAsync(string source, string message, Exception? ex = null, Guid? tenantId = null)
        => LogAsync(source, message, SystemEventLevel.Error, ex?.ToString(), tenantId);
}

