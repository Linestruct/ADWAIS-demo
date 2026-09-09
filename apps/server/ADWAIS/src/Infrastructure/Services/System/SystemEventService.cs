// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Entities;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Adwais.Application.Interfaces;
using System.Diagnostics;

namespace Adwais.Infrastructure.Services;

/// <summary>
/// Implementation of ISystemEventService that persists events to the database and logs them using ILogger.
/// </summary>
public class SystemEventService(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    ILogger<SystemEventService> logger) : ISystemEventService
{
    /// <inheritdoc />
    public async Task LogAsync(
        string source,
        string message,
        SystemEventLevel level = SystemEventLevel.Information,
        string? details = null,
        Guid? tenantId = null,
        Guid? organizationId = null,
        string? code = null,
        SystemEventAudience audience = SystemEventAudience.Platform,
        Guid? pipelineRunId = null,
        string? traceId = null,
        string? requestId = null,
        string? suggestedAction = null)
    {
        try
        {
            await using var db = await dbContextFactory.CreateDbContextAsync();

            if (tenantId.HasValue)
            {
                var tenant = await db.Tenants
                    .AsNoTracking()
                    .Where(tenant => tenant.Id == tenantId.Value)
                    .Select(tenant => new { tenant.OrganizationId })
                    .SingleOrDefaultAsync();

                if (tenant is null)
                {
                    // A deleted or guessed tenant must not make the whole
                    // event write fail on a foreign-key violation.
                    tenantId = null;
                }
                else if (organizationId is null)
                {
                    organizationId = tenant.OrganizationId;
                }
                else if (organizationId != tenant.OrganizationId)
                {
                    // Never attach a tenant from one organization to an
                    // otherwise valid event for another organization.
                    tenantId = null;
                    organizationId = null;
                }
            }

            if (organizationId is { } ownerId
                && !await db.Organizations.AsNoTracking().AnyAsync(organization => organization.Id == ownerId))
            {
                organizationId = null;
            }

            // An organization or tenant audience without verified ownership is
            // never written as an organization-visible event.
            if (organizationId is null || (audience == SystemEventAudience.Tenant && tenantId is null))
                audience = SystemEventAudience.Platform;

            var normalizedCode = Truncate(code ?? "legacy", 100)!;
            if (pipelineRunId is { } correlatedRunId
                && await db.SystemEvents.AsNoTracking().AnyAsync(systemEvent =>
                    systemEvent.PipelineRunId == correlatedRunId
                    && systemEvent.Code == normalizedCode))
            {
                // Retries of one logical run should not create a duplicate
                // durable incident for every attempt.
                return;
            }

            var systemEvent = new SystemEvent
            {
                Source = Truncate(source, 100) ?? string.Empty,
                Message = Truncate(message, 500) ?? string.Empty,
                Level = level,
                Details = audience == SystemEventAudience.Platform ? Truncate(details, 10_000) : null,
                Code = normalizedCode,
                Audience = audience,
                PipelineRunId = pipelineRunId,
                TraceId = Truncate(traceId ?? Activity.Current?.TraceId.ToHexString(), 64),
                RequestId = Truncate(requestId, 200),
                SuggestedAction = Truncate(suggestedAction, 500),
                OrganizationId = organizationId,
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
            
            logger.Log(logLevel,
                "[{Source}] {Message} (Organization: {OrganizationId}, Tenant: {TenantId}, Run: {PipelineRunId})",
                source, message, organizationId, tenantId, pipelineRunId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist SystemEvent from {Source}: {Message}", source, message);
        }
    }

    /// <inheritdoc />
    public Task LogWarningAsync(
        string source,
        string message,
        string? details = null,
        Guid? tenantId = null,
        Guid? organizationId = null,
        string? code = null,
        SystemEventAudience audience = SystemEventAudience.Platform,
        Guid? pipelineRunId = null,
        string? traceId = null,
        string? requestId = null,
        string? suggestedAction = null)
        => LogAsync(source, message, SystemEventLevel.Warning, details, tenantId, organizationId, code, audience,
            pipelineRunId, traceId, requestId, suggestedAction);

    /// <inheritdoc />
    public Task LogErrorAsync(
        string source,
        string message,
        Exception? ex = null,
        Guid? tenantId = null,
        Guid? organizationId = null,
        string? code = null,
        SystemEventAudience audience = SystemEventAudience.Platform,
        Guid? pipelineRunId = null,
        string? traceId = null,
        string? requestId = null,
        string? suggestedAction = null)
        => LogAsync(source, message, SystemEventLevel.Error, ex?.ToString(), tenantId, organizationId, code, audience,
            pipelineRunId, traceId, requestId, suggestedAction);

    private static string? Truncate(string? value, int maxLength)
        => value is null ? null : value.Length <= maxLength ? value : value[..maxLength];
}

