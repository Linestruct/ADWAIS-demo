// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;

namespace Adwais.Domain.Entities.Monitoring;

public class DailyAvailabilityMonitorRollup
{
    public required int MonitorId { get; set; }
    public required DateTimeOffset Date { get; set; }
    public double? UptimePercentage { get; set; }

    public UptimeMonitor UptimeMonitor { get; set; } = null!;
}


