// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Api.DTOs.Monitoring;

public sealed record MonitorAvailabilitySeriesResponseDto
{
    public required DateTimeOffset PeriodStart { get; init; }

    public required DateTimeOffset PeriodEnd { get; init; }

    public required double? AverageUptimePercentage { get; init; }

    public required double? LowestUptimePercentage { get; init; }

    public required IReadOnlyList<MonitorAvailabilityPointResponseDto> Points { get; init; }
}

public sealed record MonitorAvailabilityPointResponseDto
{
    public required DateOnly Date { get; init; }

    public required DateOnly EndDate { get; init; }

    public required double? UptimePercentage { get; init; }

    public required double? LowestMonitorUptimePercentage { get; init; }

    public required int MonitorCount { get; init; }

    public required bool IsPartial { get; init; }
}
