// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain.Entities.Monitoring;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;

using Adwais.Domain;

namespace Adwais.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public TenantType Type { get; set; }
    public string OrderProvider { get; set; } = IntegrationProviders.Litium;
    public string? OrderProviderSettings { get; set; }
    public string? ImageUrl { get; set; }
    public DateTimeOffset? FetchedFrom { get; set; }
    public DateTimeOffset? FetchedUntil { get; set; }
    public DateTimeOffset? LastPolled { get; set; }
    public bool OrderFetchingEnabled { get; set; }
    public bool CurrentlyFetching { get; set; }
    public string? LastSyncError { get; set; }
    public bool IsSystem { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<UptimeMonitor> Monitors { get; set; } = new List<UptimeMonitor>();
    public Organization? Organization { get; set; }
}


