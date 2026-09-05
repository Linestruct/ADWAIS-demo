// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.GlobalConfig;

public record FetchIntervalsDto
{
    public int LatencyFetchIntervalMinutes { get; init; }
    public int UptimeFetchIntervalMinutes { get; init; }
    public int StatusFetchIntervalMinutes { get; init; }
    public int OrderFetchIntervalMinutes { get; init; }
    public int UserStatsFetchIntervalMinutes { get; init; }
    public int FeedFetchIntervalHours { get; init; }
}
