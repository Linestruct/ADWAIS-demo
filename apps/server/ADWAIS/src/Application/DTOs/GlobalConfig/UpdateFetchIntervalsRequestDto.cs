// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.GlobalConfig;

public record UpdateFetchIntervalsRequestDto(
    int? OrderFetchIntervalMinutes = null,
    int? UptimeFetchIntervalMinutes = null,
    int? UserStatsFetchIntervalMinutes = null,
    int? LatencyFetchIntervalMinutes = null,
    int? FeedFetchIntervalHours = null
);
