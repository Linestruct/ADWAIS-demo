// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.DTOs.GlobalConfig;
using Adwais.Application.Interfaces;
using Adwais.Application.Common.Interfaces;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.Helpers;
using Adwais.Infrastructure.Jobs;
using Adwais.Infrastructure.Jobs.Monitor;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Adwais.Infrastructure.Services;

public class GlobalConfigService(
    IApplicationDbContext dbContext,
    ISystemEventService eventService,
    IReportingRollupRefresher reportingRollupRefresher,
    IEnumerable<IMonitoringProvider> monitoringProviders) : IGlobalConfigService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ISystemEventService _eventService = eventService;
    private readonly IReportingRollupRefresher _reportingRollupRefresher = reportingRollupRefresher;
    private readonly IEnumerable<IMonitoringProvider> _monitoringProviders = monitoringProviders;

    public async Task<GlobalConfigResponseDto> GetConfigAsync(CancellationToken ct = default)
    {
        var config = await _dbContext.GlobalConfigs.AsNoTracking().SingleOrDefaultAsync(ct);
        if (config == null) throw new InvalidOperationException("Global configuration not found.");

        return MapToDto(config);
    }

    public async Task<GlobalConfigResponseDto> UpdateConfigAsync(UpdateGlobalConfigRequestDto request, CancellationToken ct = default)
    {
        var config = await _dbContext.GlobalConfigs.SingleOrDefaultAsync(ct);
        if (config == null) throw new InvalidOperationException("Global configuration not found.");

        if (request.OrderFetchEnabled.HasValue) config.OrderFetchEnabled = request.OrderFetchEnabled.Value;
        if (request.MonitoringFetchEnabled.HasValue) config.MonitoringFetchEnabled = request.MonitoringFetchEnabled.Value;
        if (!string.IsNullOrWhiteSpace(request.MonitoringProvider))
        {
            var provider = request.MonitoringProvider.Trim().ToLowerInvariant();
            _monitoringProviders.ForProvider(provider);
            config.MonitoringProvider = provider;
            config.MonitoringProviderSettings = null;
        }
        if (request.MonitoringProviderSettings != null)
        {
            config.MonitoringProviderSettings = _monitoringProviders
                .ForProvider(config.MonitoringProvider)
                .MergeSettings(config.MonitoringProviderSettings, request.MonitoringProviderSettings);
        }
        if (request.SystemEventRetentionDays.HasValue) config.SystemEventRetentionDays = request.SystemEventRetentionDays.Value;
        if (request.FeedFetchIntervalHours.HasValue)
        {
            config.FeedFetchIntervalHours = request.FeedFetchIntervalHours.Value;
            RecurringJob.AddOrUpdate<FeedAggregationJob>(
                "aggregate-intranet-feeds",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.HourInterval(request.FeedFetchIntervalHours.Value));
        }
        if (!string.IsNullOrWhiteSpace(request.WeatherLocation)) config.WeatherLocation = request.WeatherLocation.Trim();
        if (request.WeatherFetchIntervalMinutes.HasValue) config.WeatherFetchIntervalMinutes = request.WeatherFetchIntervalMinutes.Value;
        var reportingTimeZoneChanged = request.ReportingTimeZoneId is not null
            && !string.Equals(config.ReportingTimeZoneId, request.ReportingTimeZoneId.Trim(), StringComparison.Ordinal);
        if (request.ReportingTimeZoneId is not null) config.ReportingTimeZoneId = request.ReportingTimeZoneId.Trim();

        await _dbContext.SaveChangesAsync(ct);
        // Once the config is persisted, finish rebuilding even if the HTTP request is cancelled.
        if (reportingTimeZoneChanged) await _reportingRollupRefresher.RefreshAsync(CancellationToken.None);
        await _eventService.LogAsync(nameof(GlobalConfigService), "Global configuration updated.");

        return MapToDto(config);
    }

    public async Task TriggerFeedFetchAsync(CancellationToken ct = default)
    {
        await Task.Run(() => RecurringJob.TriggerJob("aggregate-intranet-feeds"), ct);
    }

    public async Task UpdateFeedIntervalAsync(int intervalHours, CancellationToken ct = default)
    {
        if (intervalHours <= 0) throw new ArgumentException("Interval must be at least 1 hour.", nameof(intervalHours));

        var config = await _dbContext.GlobalConfigs.SingleOrDefaultAsync(ct);
        if (config == null) throw new InvalidOperationException("Global configuration not found.");

        config.FeedFetchIntervalHours = intervalHours;
        await _dbContext.SaveChangesAsync(ct);

        RecurringJob.AddOrUpdate<FeedAggregationJob>(
            "aggregate-intranet-feeds",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.HourInterval(intervalHours));

        await _eventService.LogAsync(nameof(GlobalConfigService), $"Feed aggregation interval updated to {intervalHours} hours.");
    }

    public async Task<FetchIntervalsDto> GetFetchIntervalsAsync(CancellationToken ct = default)
    {
        var config = await _dbContext.GlobalConfigs
            .AsNoTracking()
            .Select(g => new
            {
                g.LatencyFetchIntervalMinutes,
                g.UptimeFetchIntervalMinutes,
                g.OrderFetchIntervalMinutes,
                g.UserStatsFetchIntervalMinutes,
                g.FeedFetchIntervalHours
            })
            .SingleOrDefaultAsync(ct);

        if (config == null) throw new InvalidOperationException("Global configuration not found.");

        var lowestInterval = await _dbContext.Monitors
            .Where(m => m.TenantId != IApplicationDbContext.SystemTenantGuid)
            .MinAsync(m => (int?)m.UpdateInterval, ct);

        var lowestIntervalMins = Math.Max(1, (lowestInterval ?? 300) / 60);

        return new FetchIntervalsDto
        {
            LatencyFetchIntervalMinutes = config.LatencyFetchIntervalMinutes,
            UptimeFetchIntervalMinutes = config.UptimeFetchIntervalMinutes,
            StatusFetchIntervalMinutes = lowestIntervalMins,
            OrderFetchIntervalMinutes = config.OrderFetchIntervalMinutes,
            UserStatsFetchIntervalMinutes = config.UserStatsFetchIntervalMinutes,
            FeedFetchIntervalHours = config.FeedFetchIntervalHours
        };
    }

    public async Task<FetchIntervalsDto> UpdateFetchIntervalsAsync(UpdateFetchIntervalsRequestDto request, CancellationToken ct = default)
    {
        var config = await _dbContext.GlobalConfigs.SingleOrDefaultAsync(ct);
        if (config == null) throw new InvalidOperationException("Global configuration not found.");

        if (request.UptimeFetchIntervalMinutes.HasValue)
        {
            config.UptimeFetchIntervalMinutes = request.UptimeFetchIntervalMinutes.Value;
            RecurringJob.AddOrUpdate<UptimeDispatcherJob>(
                "dispatch-monitoring-uptime",
                job => job.ExecuteAsync(), 
                CronHelper.FromMinutes(request.UptimeFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated Uptime Fetch Interval to {request.UptimeFetchIntervalMinutes.Value} minutes");
        }
            
        if (request.LatencyFetchIntervalMinutes.HasValue)
        {
            config.LatencyFetchIntervalMinutes = request.LatencyFetchIntervalMinutes.Value;
            RecurringJob.AddOrUpdate<LatencyDispatcherJob>(
                "dispatch-monitoring-latency",
                job => job.ExecuteAsync(), 
                CronHelper.FromMinutes(request.LatencyFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated Latency Fetch Interval to {request.LatencyFetchIntervalMinutes.Value} minutes");
        }

        if (request.UserStatsFetchIntervalMinutes.HasValue)
        {
            config.UserStatsFetchIntervalMinutes = request.UserStatsFetchIntervalMinutes.Value;
            RecurringJob.AddOrUpdate<UpdateGlobalMonitoringStatsJob>(
                "sync-monitoring-account-stats",
                job => job.ExecuteAsync(), 
                CronHelper.FromMinutes(request.UserStatsFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated User Stats Fetch Interval to {request.UserStatsFetchIntervalMinutes.Value} minutes");
        }
            
        if (request.OrderFetchIntervalMinutes.HasValue)
        {
            config.OrderFetchIntervalMinutes = request.OrderFetchIntervalMinutes.Value;
            RecurringJob.AddOrUpdate<OrderFetchDispatcherJob>(
                "dispatch-order-fetch",
                job => job.ExecuteAsync(), 
                CronHelper.FromMinutes(request.OrderFetchIntervalMinutes.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated order fetch interval to {request.OrderFetchIntervalMinutes.Value} minutes");
        }

        if (request.FeedFetchIntervalHours.HasValue)
        {
            config.FeedFetchIntervalHours = request.FeedFetchIntervalHours.Value;
            RecurringJob.AddOrUpdate<FeedAggregationJob>(
                "aggregate-intranet-feeds",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.HourInterval(request.FeedFetchIntervalHours.Value));
            await _eventService.LogAsync(nameof(GlobalConfigService), $"Updated Feed Fetch Interval to {request.FeedFetchIntervalHours.Value} hours");
        }

        await _dbContext.SaveChangesAsync(ct);

        var lowestInterval = await _dbContext.Monitors
            .Where(m => m.TenantId != IApplicationDbContext.SystemTenantGuid)
            .MinAsync(m => (int?)m.UpdateInterval, ct);

        var lowestIntervalMins = Math.Max(1, (lowestInterval ?? 300) / 60);

        return new FetchIntervalsDto
        {
            LatencyFetchIntervalMinutes = config.LatencyFetchIntervalMinutes,
            UptimeFetchIntervalMinutes = config.UptimeFetchIntervalMinutes,
            StatusFetchIntervalMinutes = lowestIntervalMins,
            OrderFetchIntervalMinutes = config.OrderFetchIntervalMinutes,
            UserStatsFetchIntervalMinutes = config.UserStatsFetchIntervalMinutes,
            FeedFetchIntervalHours = config.FeedFetchIntervalHours
        };
    }

    private GlobalConfigResponseDto MapToDto(GlobalConfig config)
    {
        return new GlobalConfigResponseDto(
            config.Id,
            config.LastPolled,
            config.OrderFetchEnabled,
            config.MonitoringFetchEnabled,
            config.OrderFetchIntervalMinutes,
            _monitoringProviders.ForProvider(config.MonitoringProvider).GetPublicSettings(config.MonitoringProviderSettings),
            _monitoringProviders.ForProvider(config.MonitoringProvider).GetConfiguredSecretKeys(config.MonitoringProviderSettings),
            config.UptimeFetchIntervalMinutes,
            config.LatencyFetchIntervalMinutes,
            config.UserStatsFetchIntervalMinutes,
            config.SystemEventRetentionDays,
            config.MonitorsCount,
            config.MonitorsLimit,
            config.ActiveSubscription,
            config.FeedFetchIntervalHours,
            config.WeatherLocation,
            config.WeatherFetchIntervalMinutes,
            config.ReportingTimeZoneId,
            config.MonitoringProvider
        );
    }

}
