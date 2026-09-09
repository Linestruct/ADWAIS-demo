// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Domain.Entities;

public enum SystemEventLevel
{
    Information,
    Warning,
    Error,
    Critical
}

public enum SystemEventAudience
{
    Platform,
    Organization,
    Tenant
}

public class SystemEvent
{
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public SystemEventLevel Level { get; set; }
    public required string Source { get; set; }
    public required string Message { get; set; }
    public string? Details { get; set; }
    public string Code { get; set; } = "legacy";
    public SystemEventAudience Audience { get; set; } = SystemEventAudience.Platform;
    public Guid? PipelineRunId { get; set; }
    public string? TraceId { get; set; }
    public string? RequestId { get; set; }
    public string? SuggestedAction { get; set; }
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
}

