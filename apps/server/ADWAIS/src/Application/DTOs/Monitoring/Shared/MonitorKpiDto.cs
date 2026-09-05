// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Monitoring;

public record MonitorKpiDto(
    double? AverageUptime,
    double? PreviousAverageUptime,
    double? UptimeGrowthPercentage,
    double? AverageLatency,
    double? PreviousAverageLatency,
    double? LatencyGrowthPercentage,
    double? HighestLatency,
    double? PreviousHighestLatency,
    double? HighestLatencyGrowthPercentage,
    double? LowestLatency,
    double? PreviousLowestLatency,
    double? LowestLatencyGrowthPercentage);
