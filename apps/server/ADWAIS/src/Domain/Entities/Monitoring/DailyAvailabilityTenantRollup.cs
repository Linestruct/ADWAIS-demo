// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;

namespace Adwais.Domain.Entities.Monitoring;

public class DailyAvailabilityTenantRollup
{
    public required Guid TenantId { get; set; }
    public required DateTimeOffset Date { get; set; }
    public double? UptimePercentage { get; set; }

    public Tenant Tenant { get; set; } = null!;
}


