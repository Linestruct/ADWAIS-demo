// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Api.DTOs.Monitoring;

public record LatencyMetricsDto(
    DateTimeOffset Date, 
    double? Average, 
    double? Lowest, 
    double? Highest);

