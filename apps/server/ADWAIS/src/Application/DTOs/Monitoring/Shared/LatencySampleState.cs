// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.Monitoring;

/// <summary>
/// Describes whether a latency bucket contains observations. A bucket with no
/// samples is different from an endpoint response containing no buckets at all,
/// which means the selected scope has no monitors or no series to display.
/// </summary>
public enum LatencySampleState
{
    Observed,
    NoSamples
}
