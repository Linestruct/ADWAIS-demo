// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Domain;

namespace Adwais.Domain.Entities;

public class OrganizationConfig
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string? WeatherLocation { get; set; }
    public int WeatherFetchIntervalMinutes { get; set; } = 15;
    public string ReportingTimeZoneId { get; set; } = "Europe/Stockholm";
    public string MonitoringProvider { get; set; } = IntegrationProviders.UptimeRobot;
    public string? MonitoringProviderSettings { get; set; }
    public int OrderFetchIntervalMinutes { get; set; } = 60;
    public int UptimeFetchIntervalMinutes { get; set; } = 60;
    public int LatencyFetchIntervalMinutes { get; set; } = 10;
    public int UserStatsFetchIntervalMinutes { get; set; } = 60;
    public int FeedFetchIntervalHours { get; set; } = 2;
    public int? MonitorsCount { get; set; }
    public int? MonitorsLimit { get; set; }
    public string? ActiveSubscription { get; set; }
    public string? LastSyncError { get; set; }
}
