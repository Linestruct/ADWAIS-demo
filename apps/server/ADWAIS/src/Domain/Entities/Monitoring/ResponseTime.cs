// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Domain.Entities.Monitoring;

public class ResponseTime
{
    public Guid Id { get; set; }
    public int MonitorId { get; set; }
    public DateTimeOffset Date { get; set; }
    public double? Average { get; set; }
    public double? Lowest { get; set; }
    public double? Highest { get; set; }

    public UptimeMonitor? UptimeMonitor { get; set; }
}


