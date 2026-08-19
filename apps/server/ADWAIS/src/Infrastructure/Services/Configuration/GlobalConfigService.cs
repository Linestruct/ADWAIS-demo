// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Adwais.Domain;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Helpers;
using Adwais.Infrastructure.Jobs;
using Adwais.Infrastructure.Jobs.Monitor;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Adwais.Infrastructure.Services;

public class GlobalConfigService(
    IApplicationDbContext dbContext,
    ISystemEventService eventService,
    IReportingRollupRefresher reportingRollupRefresher,
    IOrganizationConfigService organizationConfigService,
    ICurrentAccess currentAccess) : IGlobalConfigService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ISystemEventService _eventService = eventService;
    private readonly IReportingRollupRefresher _reportingRollupRefresher = reportingRollupRefresher;
    private readonly IOrganizationConfigService _organizationConfigService = organizationConfigService;
    private readonly ICurrentAccess _currentAccess = currentAccess;

    public async Task<GlobalConfigResponseDto> GetConfigAsync(CancellationToken ct = default)
    {
        var config = await _dbContext.GlobalConfigs.AsNoTracking().SingleOrDefaultAsync(ct);
        if (config == null) throw new KeyNotFoundException("Global configuration not found.");

        var orgId = _currentAccess.Scope?.OrganizationId;
        var orgConfig = orgId is null
            ? null
            : await _organizationConfigService.GetConfigAsync(orgId.Value, ct);

        return MapToDto(config, orgConfig);
    }

    public async Task<GlobalConfigResponseDto> UpdateConfigAsync(UpdateGlobalConfigRequestDto request, CancellationToken ct = default)
    {
        var config = await _dbContext.GlobalConfigs.SingleOrDefaultAsync(ct);
        if (config == null) throw new KeyNotFoundException("Global configuration not found.");

        if (request.OrderFetchEnabled.HasValue) config.OrderFetchEnabled = request.OrderFetchEnabled.Value;
        if (request.MonitoringFetchEnabled.HasValue) config.MonitoringFetchEnabled = request.MonitoringFetchEnabled.Value;
        if (request.SystemEventRetentionDays.HasValue) config.SystemEventRetentionDays = request.SystemEventRetentionDays.Value;
        await _dbContext.SaveChangesAsync(ct);

        var orgId = _currentAccess.Scope?.OrganizationId;
        var previousOrgConfig = orgId is null
            ? null
            : await _organizationConfigService.GetConfigAsync(orgId.Value, ct);
        var reportingTimeZoneChanged = request.ReportingTimeZoneId is not null
            && !string.Equals(previousOrgConfig?.ReportingTimeZoneId, request.ReportingTimeZoneId.Trim(), StringComparison.Ordinal);

        var orgConfig = await UpdateOrgConfigAsync(request, ct);

        if (request.FeedFetchIntervalHours.HasValue)
        {
            RecurringJob.AddOrUpdate<FeedAggregationJob>(
                "aggregate-intranet-feeds",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.HourInterval(request.FeedFetchIntervalHours.Value));
        }

        await _eventService.LogAsync(nameof(GlobalConfigService), "Global configuration updated.");
        // Once the config is persisted, finish rebuilding even if the HTTP request is cancelled.
        if (reportingTimeZoneChanged) await _reportingRollupRefresher.RefreshAsync(CancellationToken.None);

        return MapToDto(config, orgConfig);
    }

    public async Task TriggerFeedFetchAsync(CancellationToken ct = default)
    {
        await Task.Run(() => RecurringJob.TriggerJob("aggregate-intranet-feeds"), ct);
    }

    public async Task UpdateFeedIntervalAsync(int intervalHours, CancellationToken ct = default)
    {
        if (intervalHours <= 0) throw new ArgumentException("Interval must be at least 1 hour.", nameof(intervalHours));

        var orgId = _currentAccess.Scope?.OrganizationId
            ?? throw new InvalidOperationException("Feed intervals require an organization scope.");

        var orgConfig = await _organizationConfigService.UpdateConfigAsync(orgId, new UpdateOrganizationConfigRequestDto(
            WeatherLocation: null,
            WeatherFetchIntervalMinutes: null,
            ReportingTimeZoneId: null,
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: null,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: intervalHours), ct);

        RecurringJob.AddOrUpdate<FeedAggregationJob>(
            "aggregate-intranet-feeds",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.HourInterval(intervalHours));

        await _eventService.LogAsync(nameof(GlobalConfigService), $"Feed aggregation interval updated to {intervalHours} hours.");
    }

    public async Task<FetchIntervalsDto> GetFetchIntervalsAsync(CancellationToken ct = default)
    {
        var orgId = _currentAccess.Scope?.OrganizationId;
        var orgConfig = orgId is null
            ? null
            : await _organizationConfigService.GetConfigAsync(orgId.Value, ct);
        var intervals = orgConfig ?? DefaultOrgConfig();

        var lowestInterval = orgId is null
            ? await _dbContext.Monitors
                .Where(m => m.TenantId != IApplicationDbContext.SystemTenantGuid)
                .MinAsync(m => (int?)m.UpdateInterval, ct)
            : await _dbContext.Monitors
                .Where(m => m.TenantId != IApplicationDbContext.SystemTenantGuid
                    && m.Tenant!.OrganizationId == orgId.Value)
                .MinAsync(m => (int?)m.UpdateInterval, ct);

        var lowestIntervalMins = Math.Max(1, (lowestInterval ?? 300) / 60);

        return new FetchIntervalsDto
        {
            LatencyFetchIntervalMinutes = intervals.LatencyFetchIntervalMinutes,
            UptimeFetchIntervalMinutes = intervals.UptimeFetchIntervalMinutes,
            StatusFetchIntervalMinutes = lowestIntervalMins,
            OrderFetchIntervalMinutes = intervals.OrderFetchIntervalMinutes,
            UserStatsFetchIntervalMinutes = intervals.UserStatsFetchIntervalMinutes,
            FeedFetchIntervalHours = intervals.FeedFetchIntervalHours
        };
    }

    public async Task<FetchIntervalsDto> UpdateFetchIntervalsAsync(UpdateFetchIntervalsRequestDto request, CancellationToken ct = default)
    {
        var orgId = _currentAccess.Scope?.OrganizationId
            ?? throw new InvalidOperationException("Fetch intervals require an organization scope.");

        var orgConfig = await _organizationConfigService.UpdateConfigAsync(orgId, new UpdateOrganizationConfigRequestDto(
            WeatherLocation: null,
            WeatherFetchIntervalMinutes: null,
            ReportingTimeZoneId: null,
            MonitoringProvider: null,
            MonitoringProviderSettings: null,
            OrderFetchIntervalMinutes: request.OrderFetchIntervalMinutes,
            UptimeFetchIntervalMinutes: request.UptimeFetchIntervalMinutes,
            LatencyFetchIntervalMinutes: request.LatencyFetchIntervalMinutes,
            UserStatsFetchIntervalMinutes: request.UserStatsFetchIntervalMinutes,
            FeedFetchIntervalHours: request.FeedFetchIntervalHours), ct);

        if (request.UptimeFetchIntervalMinutes.HasValue)
        {
            RecurringJob.AddOrUpdate<UptimeDispatcherJob>(
                "dispatch-monitoring-uptime",
                job => job.ExecuteAsync(),
                CronHelper.FromMinutes(request.UptimeFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated Uptime Fetch Interval to {request.UptimeFetchIntervalMinutes.Value} minutes");
        }

        if (request.LatencyFetchIntervalMinutes.HasValue)
        {
            RecurringJob.AddOrUpdate<LatencyDispatcherJob>(
                "dispatch-monitoring-latency",
                job => job.ExecuteAsync(),
                CronHelper.FromMinutes(request.LatencyFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated Latency Fetch Interval to {request.LatencyFetchIntervalMinutes.Value} minutes");
        }

        if (request.UserStatsFetchIntervalMinutes.HasValue)
        {
            RecurringJob.AddOrUpdate<UpdateGlobalMonitoringStatsJob>(
                "sync-monitoring-account-stats",
                job => job.ExecuteAsync(),
                CronHelper.FromMinutes(request.UserStatsFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated User Stats Fetch Interval to {request.UserStatsFetchIntervalMinutes.Value} minutes");
        }

        if (request.OrderFetchIntervalMinutes.HasValue)
        {
            RecurringJob.AddOrUpdate<OrderFetchDispatcherJob>(
                "dispatch-order-fetch",
                job => job.ExecuteAsync(),
                CronHelper.FromMinutes(request.OrderFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated order fetch interval to {request.OrderFetchIntervalMinutes.Value} minutes");
        }

        if (request.FeedFetchIntervalHours.HasValue)
        {
            RecurringJob.AddOrUpdate<FeedAggregationJob>(
                "aggregate-intranet-feeds",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.HourInterval(request.FeedFetchIntervalHours.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated Feed Fetch Interval to {request.FeedFetchIntervalHours.Value} hours");
        }

        var lowestInterval = await _dbContext.Monitors
            .Where(m => m.TenantId != IApplicationDbContext.SystemTenantGuid
                && m.Tenant!.OrganizationId == orgId)
            .MinAsync(m => (int?)m.UpdateInterval, ct);

        var lowestIntervalMins = Math.Max(1, (lowestInterval ?? 300) / 60);

        return new FetchIntervalsDto
        {
            LatencyFetchIntervalMinutes = orgConfig.LatencyFetchIntervalMinutes,
            UptimeFetchIntervalMinutes = orgConfig.UptimeFetchIntervalMinutes,
            StatusFetchIntervalMinutes = lowestIntervalMins,
            OrderFetchIntervalMinutes = orgConfig.OrderFetchIntervalMinutes,
            UserStatsFetchIntervalMinutes = orgConfig.UserStatsFetchIntervalMinutes,
            FeedFetchIntervalHours = orgConfig.FeedFetchIntervalHours
        };
    }

    private async Task<OrganizationConfigDto> UpdateOrgConfigAsync(UpdateGlobalConfigRequestDto request, CancellationToken ct)
    {
        var orgId = _currentAccess.Scope?.OrganizationId;
        var hasOrgFields = request.MonitoringProvider is not null
            || request.MonitoringProviderSettings is not null
            || request.WeatherLocation is not null
            || request.WeatherFetchIntervalMinutes.HasValue
            || request.ReportingTimeZoneId is not null
            || request.FeedFetchIntervalHours.HasValue;
        if (!hasOrgFields)
        {
            if (orgId is null) return DefaultOrgConfig();
            return await _organizationConfigService.GetConfigAsync(orgId.Value, ct) ?? DefaultOrgConfig();
        }

        if (orgId is null)
            throw new InvalidOperationException("Organization-scoped settings require an organization scope.");

        return await _organizationConfigService.UpdateConfigAsync(orgId.Value, new UpdateOrganizationConfigRequestDto(
            WeatherLocation: request.WeatherLocation,
            WeatherFetchIntervalMinutes: request.WeatherFetchIntervalMinutes,
            ReportingTimeZoneId: request.ReportingTimeZoneId,
            MonitoringProvider: request.MonitoringProvider,
            MonitoringProviderSettings: request.MonitoringProviderSettings,
            OrderFetchIntervalMinutes: null,
            UptimeFetchIntervalMinutes: null,
            LatencyFetchIntervalMinutes: null,
            UserStatsFetchIntervalMinutes: null,
            FeedFetchIntervalHours: request.FeedFetchIntervalHours), ct);
    }

    private GlobalConfigResponseDto MapToDto(GlobalConfig config, OrganizationConfigDto? orgConfig)
    {
        var org = orgConfig ?? DefaultOrgConfig();
        return new GlobalConfigResponseDto(
            config.Id,
            config.LastPolled,
            config.OrderFetchEnabled,
            config.MonitoringFetchEnabled,
            org.OrderFetchIntervalMinutes,
            org.MonitoringProviderSettings,
            org.MonitoringProviderConfiguredSecretKeys,
            org.UptimeFetchIntervalMinutes,
            org.LatencyFetchIntervalMinutes,
            org.UserStatsFetchIntervalMinutes,
            config.SystemEventRetentionDays,
            org.MonitorsCount,
            org.MonitorsLimit,
            org.ActiveSubscription,
            org.FeedFetchIntervalHours,
            org.WeatherLocation,
            org.WeatherFetchIntervalMinutes,
            org.ReportingTimeZoneId,
            org.MonitoringProvider
        );
    }

    private static OrganizationConfigDto DefaultOrgConfig() => new(
        WeatherLocation: null,
        WeatherFetchIntervalMinutes: 15,
        ReportingTimeZoneId: "Europe/Stockholm",
        MonitoringProvider: IntegrationProviders.UptimeRobot,
        MonitoringProviderSettings: new Dictionary<string, string?>(),
        MonitoringProviderConfiguredSecretKeys: [],
        OrderFetchIntervalMinutes: 60,
        UptimeFetchIntervalMinutes: 60,
        LatencyFetchIntervalMinutes: 10,
        UserStatsFetchIntervalMinutes: 60,
        FeedFetchIntervalHours: 2,
        MonitorsCount: null,
        MonitorsLimit: null,
        ActiveSubscription: null,
        LastSyncError: null);
}