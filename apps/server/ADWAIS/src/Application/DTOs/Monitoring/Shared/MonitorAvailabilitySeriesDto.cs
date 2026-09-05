// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Monitoring;

public sealed record MonitorAvailabilitySeriesDto(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    double? AverageUptimePercentage,
    double? LowestUptimePercentage,
    IReadOnlyList<MonitorAvailabilityPointDto> Points);

public sealed record MonitorAvailabilityPointDto(
    DateOnly Date,
    DateOnly EndDate,
    double? UptimePercentage,
    double? LowestMonitorUptimePercentage,
    int MonitorCount,
    bool IsPartial);
