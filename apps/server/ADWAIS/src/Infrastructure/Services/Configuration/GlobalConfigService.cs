// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Access;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Common.Jobs;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Adwais.Domain;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Helpers;
using Adwais.Infrastructure.Jobs;
using Adwais.Infrastructure.Jobs.MaterializedViews;
using Adwais.Infrastructure.Jobs.Monitor;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Adwais.Infrastructure.Services;

public class GlobalConfigService(
    IApplicationDbContext dbContext,
    ISystemEventService eventService,
    IOrganizationConfigService organizationConfigService,
    ICurrentAccess currentAccess,
    IJobTriggerService jobTriggerService) : IGlobalConfigService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly IJobTriggerService _jobTriggerService = jobTriggerService;
    private readonly ISystemEventService _eventService = eventService;
    private readonly IOrganizationConfigService _organizationConfigService = organizationConfigService;
    private readonly ICurrentAccess _currentAccess = currentAccess;

    public async Task<GlobalConfigResponseDto> GetConfigAsync(CancellationToken ct = default)
    {
        var config = await _dbContext.GlobalConfigs.AsNoTracking().SingleOrDefaultAsync(ct);
        if (config == null) throw new KeyNotFoundException("Global configuration not found.");

        return MapToDto(config);
    }

    public async Task<GlobalConfigResponseDto> UpdateConfigAsync(UpdateGlobalConfigRequestDto request, CancellationToken ct = default)
    {
        var config = await _dbContext.GlobalConfigs.SingleOrDefaultAsync(ct);
        if (config == null) throw new KeyNotFoundException("Global configuration not found.");

        if (request.SystemEventRetentionDays.HasValue) config.SystemEventRetentionDays = request.SystemEventRetentionDays.Value;
        if (request.MatViewRefreshIntervalMinutes.HasValue)
        {
            var interval = request.MatViewRefreshIntervalMinutes.Value;
            if (interval < 5) throw new ArgumentException("Interval must be at least 5 minutes.", nameof(request.MatViewRefreshIntervalMinutes));
            config.MatViewRefreshIntervalMinutes = interval;
        }
        if (request.VisibleRecurringJobs is not null)
        {
            config.VisibleRecurringJobsCsv = RecurringJobVisibility.JoinVisibleKinds(request.VisibleRecurringJobs);
        }
        await _dbContext.SaveChangesAsync(ct);

        if (request.MatViewRefreshIntervalMinutes.HasValue)
        {
            RecurringJob.AddOrUpdate<RefreshStaleMaterializedViewsJob>(
                RecurringJobId.Platform(RecurringJobKind.StaleViewRefresh),
                job => job.ExecuteAsync(),
                CronHelper.FromMinutes(config.MatViewRefreshIntervalMinutes));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Stale materialized view refresh interval updated to {config.MatViewRefreshIntervalMinutes} minutes.");
        }

        await _eventService.LogAsync(nameof(GlobalConfigService), "Global configuration updated.");

        return MapToDto(config);
    }

    public async Task TriggerFeedFetchAsync(CancellationToken ct = default)
    {
        await _jobTriggerService.TriggerFeedSyncAsync(_currentAccess.Scope?.OrganizationId, ct);
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

        RecurringJob.AddOrUpdate<AggregateOrganizationFeedsJob>(
            RecurringJobId.For(RecurringJobKind.FeedFetch, orgId),
            job => job.ExecuteAsync(orgId, CancellationToken.None),
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
                .Where(m => !_dbContext.Tenants.Any(s => s.IsSystem && s.Id == m.TenantId))
                .MinAsync(m => (int?)m.UpdateInterval, ct)
            : await _dbContext.Monitors
                .Where(m => !_dbContext.Tenants.Any(s => s.IsSystem && s.Id == m.TenantId)
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
            RecurringJob.AddOrUpdate<MonitorUptimeDispatchJob>(
                RecurringJobId.For(RecurringJobKind.UptimeFetch, orgId),
                job => job.ExecuteAsync(orgId),
                CronHelper.FromMinutes(request.UptimeFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated Uptime Fetch Interval to {request.UptimeFetchIntervalMinutes.Value} minutes");
        }

        if (request.LatencyFetchIntervalMinutes.HasValue)
        {
            RecurringJob.AddOrUpdate<MonitorLatencyDispatchJob>(
                RecurringJobId.For(RecurringJobKind.LatencyFetch, orgId),
                job => job.ExecuteAsync(orgId),
                CronHelper.FromMinutes(request.LatencyFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated Latency Fetch Interval to {request.LatencyFetchIntervalMinutes.Value} minutes");
        }

        if (request.UserStatsFetchIntervalMinutes.HasValue)
        {
            RecurringJob.AddOrUpdate<SyncOrganizationAccountStatsJob>(
                RecurringJobId.For(RecurringJobKind.UserStatsFetch, orgId),
                job => job.ExecuteAsync(orgId),
                CronHelper.FromMinutes(request.UserStatsFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated User Stats Fetch Interval to {request.UserStatsFetchIntervalMinutes.Value} minutes");
        }

        if (request.OrderFetchIntervalMinutes.HasValue)
        {
            RecurringJob.AddOrUpdate<OrderFetchDispatchJob>(
                RecurringJobId.For(RecurringJobKind.OrderFetch, orgId),
                job => job.ExecuteAsync(orgId),
                CronHelper.FromMinutes(request.OrderFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated order fetch interval to {request.OrderFetchIntervalMinutes.Value} minutes");
        }

        if (request.FeedFetchIntervalHours.HasValue)
        {
            RecurringJob.AddOrUpdate<AggregateOrganizationFeedsJob>(
                RecurringJobId.For(RecurringJobKind.FeedFetch, orgId),
                job => job.ExecuteAsync(orgId, CancellationToken.None),
                Cron.HourInterval(request.FeedFetchIntervalHours.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated Feed Fetch Interval to {request.FeedFetchIntervalHours.Value} hours");
        }

        var lowestInterval = await _dbContext.Monitors
            .Where(m => !_dbContext.Tenants.Any(s => s.IsSystem && s.Id == m.TenantId)
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

    private GlobalConfigResponseDto MapToDto(GlobalConfig config) =>
        new(config.Id, config.LastPolled, config.SystemEventRetentionDays, config.MatViewRefreshIntervalMinutes,
            RecurringJobVisibility.ParseVisibleKinds(config.VisibleRecurringJobsCsv));

    private static OrganizationConfigDto DefaultOrgConfig() => new(
        WeatherLocation: null,
        WeatherFetchIntervalMinutes: 15,
        ReportingTimeZoneId: "Europe/Stockholm",
        MonitoringProvider: IntegrationProviders.UptimeRobot,
        MonitoringProviderSettings: new Dictionary<string, string?>(),
        MonitoringProviderConfiguredSecretKeys: [],
        OrderFetchEnabled: true,
        MonitoringFetchEnabled: true,
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
