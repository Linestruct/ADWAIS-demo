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
using Adwais.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Services;

public class OrganizationConfigService(
    IApplicationDbContext dbContext,
    ICurrentAccess currentAccess,
    IEnumerable<IMonitoringProvider> monitoringProviders,
    IViewRefreshTracker viewRefreshTracker) : IOrganizationConfigService
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly ICurrentAccess _currentAccess = currentAccess;
    private readonly IEnumerable<IMonitoringProvider> _monitoringProviders = monitoringProviders;
    private readonly IViewRefreshTracker _viewRefreshTracker = viewRefreshTracker;

    public async Task<OrganizationConfigDto?> GetConfigAsync(CancellationToken ct = default)
    {
        var orgId = _currentAccess.Scope?.OrganizationId;
        if (orgId is null) return null;

        return await GetConfigAsync(orgId.Value, ct);
    }

    public async Task<OrganizationConfigDto?> GetConfigAsync(Guid organizationId, CancellationToken ct = default)
    {
        var config = await _dbContext.OrganizationConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.OrganizationId == organizationId, ct);
        return config is null ? null : MapToDto(config);
    }

    public async Task<OrganizationConfigDto> UpdateConfigAsync(Guid organizationId, UpdateOrganizationConfigRequestDto request, CancellationToken ct = default)
    {
        var config = await _dbContext.OrganizationConfigs
            .SingleOrDefaultAsync(c => c.OrganizationId == organizationId, ct);

        if (config is null)
        {
            config = new OrganizationConfig { OrganizationId = organizationId };
            _dbContext.OrganizationConfigs.Add(config);
        }

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
        if (!string.IsNullOrWhiteSpace(request.WeatherLocation)) config.WeatherLocation = request.WeatherLocation.Trim();
        if (request.WeatherFetchIntervalMinutes.HasValue) config.WeatherFetchIntervalMinutes = request.WeatherFetchIntervalMinutes.Value;
        var timezoneChanged = request.ReportingTimeZoneId is not null
            && !string.Equals(config.ReportingTimeZoneId, request.ReportingTimeZoneId.Trim(), StringComparison.Ordinal);
        if (request.ReportingTimeZoneId is not null) config.ReportingTimeZoneId = request.ReportingTimeZoneId.Trim();
        if (request.OrderFetchEnabled.HasValue) config.OrderFetchEnabled = request.OrderFetchEnabled.Value;
        if (request.MonitoringFetchEnabled.HasValue) config.MonitoringFetchEnabled = request.MonitoringFetchEnabled.Value;
        if (request.OrderFetchIntervalMinutes.HasValue) config.OrderFetchIntervalMinutes = request.OrderFetchIntervalMinutes.Value;
        if (request.UptimeFetchIntervalMinutes.HasValue) config.UptimeFetchIntervalMinutes = request.UptimeFetchIntervalMinutes.Value;
        if (request.LatencyFetchIntervalMinutes.HasValue) config.LatencyFetchIntervalMinutes = request.LatencyFetchIntervalMinutes.Value;
        if (request.UserStatsFetchIntervalMinutes.HasValue) config.UserStatsFetchIntervalMinutes = request.UserStatsFetchIntervalMinutes.Value;
        if (request.FeedFetchIntervalHours.HasValue) config.FeedFetchIntervalHours = request.FeedFetchIntervalHours.Value;

        await _dbContext.SaveChangesAsync(ct);
        // The timezone change invalidates the reporting calendar, so the views must
        // be rebuilt. Mark the organization dirty; the platform refresh job owns the rebuild.
        if (timezoneChanged) await _viewRefreshTracker.MarkDirtyAsync(organizationId, CancellationToken.None);
        return MapToDto(config);
    }

    private OrganizationConfigDto MapToDto(OrganizationConfig config)
    {
        var provider = _monitoringProviders.ForProvider(config.MonitoringProvider);
        return new OrganizationConfigDto(
            config.WeatherLocation,
            config.WeatherFetchIntervalMinutes,
            config.ReportingTimeZoneId,
            config.MonitoringProvider,
            provider.GetPublicSettings(config.MonitoringProviderSettings),
            provider.GetConfiguredSecretKeys(config.MonitoringProviderSettings),
            config.OrderFetchEnabled,
            config.MonitoringFetchEnabled,
            config.OrderFetchIntervalMinutes,
            config.UptimeFetchIntervalMinutes,
            config.LatencyFetchIntervalMinutes,
            config.UserStatsFetchIntervalMinutes,
            config.FeedFetchIntervalHours,
            config.MonitorsCount,
            config.MonitorsLimit,
            config.ActiveSubscription,
            config.LastSyncError);
    }
}
