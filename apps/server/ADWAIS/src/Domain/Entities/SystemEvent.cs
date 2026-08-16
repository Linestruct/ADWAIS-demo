// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

namespace Adwais.Domain.Entities;

public enum SystemEventLevel
{
    Information,
    Warning,
    Error,
    Critical
}

public class SystemEvent
{
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public SystemEventLevel Level { get; set; }
    public required string Source { get; set; }
    public required string Message { get; set; }
    public string? Details { get; set; }
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
}

