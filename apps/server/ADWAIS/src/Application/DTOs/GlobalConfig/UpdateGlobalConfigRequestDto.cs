// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

namespace Adwais.Application.DTOs.GlobalConfig;

public record UpdateGlobalConfigRequestDto(
    bool? OrderFetchEnabled = null,
    bool? MonitoringFetchEnabled = null,
    Dictionary<string, string?>? MonitoringProviderSettings = null,
    int? SystemEventRetentionDays = null,
    int? FeedFetchIntervalHours = null,
    string? WeatherLocation = null,
    int? WeatherFetchIntervalMinutes = null,
    string? ReportingTimeZoneId = null,
    string? MonitoringProvider = null
);
