// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Adwais.Application.DTOs.GlobalConfig;

public record OrganizationConfigDto(
    string? WeatherLocation,
    [property: Required] int WeatherFetchIntervalMinutes,
    [property: Required] string ReportingTimeZoneId,
    [property: Required] string MonitoringProvider,
    [property: Required] IReadOnlyDictionary<string, string?> MonitoringProviderSettings,
    [property: Required] IReadOnlyCollection<string> MonitoringProviderConfiguredSecretKeys,
    [property: Required] int OrderFetchIntervalMinutes,
    [property: Required] int UptimeFetchIntervalMinutes,
    [property: Required] int LatencyFetchIntervalMinutes,
    [property: Required] int UserStatsFetchIntervalMinutes,
    [property: Required] int FeedFetchIntervalHours,
    int? MonitorsCount,
    int? MonitorsLimit,
    string? ActiveSubscription,
    string? LastSyncError);
