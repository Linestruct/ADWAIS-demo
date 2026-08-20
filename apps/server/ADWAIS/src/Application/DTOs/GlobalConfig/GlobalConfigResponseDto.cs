// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain;

namespace Adwais.Application.DTOs.GlobalConfig;

public record GlobalConfigResponseDto(
    int Id,
    DateTimeOffset? LastPolled,
    bool OrderFetchEnabled,
    bool MonitoringFetchEnabled,
    int OrderFetchIntervalMinutes,
    IReadOnlyDictionary<string, string?> MonitoringProviderSettings,
    IReadOnlyCollection<string> MonitoringProviderConfiguredSecretKeys,
    int UptimeFetchIntervalMinutes,
    int LatencyFetchIntervalMinutes,
    int UserStatsFetchIntervalMinutes,
    int SystemEventRetentionDays,
    int? MonitorsCount,
    int? MonitorsLimit,
    string? ActiveSubscription,
    int FeedFetchIntervalHours,
    string? WeatherLocation,
    int WeatherFetchIntervalMinutes,
    string ReportingTimeZoneId,
    string MonitoringProvider = IntegrationProviders.UptimeRobot
);
