// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using Adwais.Application.DTOs.Monitoring;

using System;

namespace Adwais.Api.DTOs.Monitoring;

public sealed record LatencyPointResponseDto
{
    public required DateTimeOffset Timestamp { get; init; }
    public required double? Average { get; init; }
    public required double? PreviousAverage { get; init; }
    public required double? P10 { get; init; }
    public required double? P90 { get; init; }
    public required LatencySampleState CurrentState { get; init; }
    public required LatencySampleState PreviousState { get; init; }
}


