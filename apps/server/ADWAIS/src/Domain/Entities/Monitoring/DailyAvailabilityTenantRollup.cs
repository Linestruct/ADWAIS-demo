// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;

namespace Adwais.Domain.Entities.Monitoring;

public class DailyAvailabilityTenantRollup
{
    public required Guid TenantId { get; set; }
    public required DateTimeOffset Date { get; set; }
    public required Guid OrganizationId { get; set; }
    public double? UptimePercentage { get; set; }

    public Tenant Tenant { get; set; } = null!;
}


