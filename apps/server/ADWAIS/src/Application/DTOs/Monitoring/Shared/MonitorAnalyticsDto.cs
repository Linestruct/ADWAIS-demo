// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Monitoring;

public record MonitorAnalyticsDto(
    double? GlobalAverageLatency,
    IReadOnlyList<LatencyPointDto> LatencyPoints,
    MonitorKpiDto Kpis);


