// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;

namespace Adwais.Application.DTOs.Monitoring;

public record LatencyPointDto(
    DateTimeOffset Timestamp,
    double? Average,
    double? PreviousAverage,
    double? Lowest,
    double? Highest,
    LatencySampleState CurrentState,
    LatencySampleState PreviousState);


