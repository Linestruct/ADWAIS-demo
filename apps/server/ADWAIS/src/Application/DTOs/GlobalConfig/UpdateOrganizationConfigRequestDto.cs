// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System.Collections.Generic;

namespace Adwais.Application.DTOs.GlobalConfig;

public record UpdateOrganizationConfigRequestDto(
    string? WeatherLocation,
    int? WeatherFetchIntervalMinutes,
    string? ReportingTimeZoneId,
    string? MonitoringProvider,
    Dictionary<string, string?>? MonitoringProviderSettings,
    int? OrderFetchIntervalMinutes,
    int? UptimeFetchIntervalMinutes,
    int? LatencyFetchIntervalMinutes,
    int? UserStatsFetchIntervalMinutes,
    int? FeedFetchIntervalHours,
    bool? OrderFetchEnabled = null,
    bool? MonitoringFetchEnabled = null);
