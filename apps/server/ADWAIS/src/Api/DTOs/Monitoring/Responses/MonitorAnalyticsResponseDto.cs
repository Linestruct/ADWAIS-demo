// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Api.DTOs.Monitoring;

public sealed record MonitorAnalyticsResponseDto
{
    public required double? GlobalAverageLatency { get; init; }

    public required IReadOnlyList<LatencyPointResponseDto> LatencyPoints { get; init; }

    public required MonitorKpiResponseDto Kpis { get; init; }
}


