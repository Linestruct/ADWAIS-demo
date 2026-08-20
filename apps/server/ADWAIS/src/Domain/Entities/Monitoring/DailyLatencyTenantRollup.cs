// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;

namespace Adwais.Domain.Entities.Monitoring;

public class DailyLatencyTenantRollup
{
    public required Guid TenantId { get; set; }
    public required DateTimeOffset Date { get; set; }
    public double? Average { get; set; }
    public double? P10 { get; set; }
    public double? P90 { get; set; }

    public Tenant Tenant { get; set; } = null!;
}


