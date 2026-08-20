// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Infrastructure.Persistence;
using Adwais.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Adwais.Infrastructure.Jobs.Monitor;

public class UpdateGlobalMonitoringStatsJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IEnumerable<IMonitoringProvider> monitoringProviders,
    ILogger<UpdateGlobalMonitoringStatsJob> logger,
    ISystemEventService eventService)
{
    public async Task ExecuteAsync()
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        var config = await db.GlobalConfigs.SingleOrDefaultAsync();

        if (config == null || string.IsNullOrWhiteSpace(config.MonitoringProviderSettings) || !config.MonitoringFetchEnabled)
        {
            return;
        }

        try
        {
            var user = await monitoringProviders.ForProvider(config.MonitoringProvider).GetAccountDetailsAsync();
            config.MonitorsCount = user.MonitorsCount;
            config.MonitorsLimit = user.MonitorLimit;
            config.ActiveSubscription = user.ActiveSubscriptionPlan;
            config.LastSyncError = null;
            
            await db.SaveChangesAsync();
            logger.LogInformation("Updated global monitoring account stats: {Count}/{Limit} ({Sub})", user.MonitorsCount, user.MonitorLimit, user.ActiveSubscriptionPlan);
        }
        catch (Exception ex)
        {
            var detailedErrorMessage = $"Failed to update global monitoring account stats: {ex.Message}";
            try
            {
                await eventService.LogErrorAsync(nameof(UpdateGlobalMonitoringStatsJob), detailedErrorMessage, ex);
            }
            catch
            {
                // Suppress logging service failure
            }

            try
            {
                config.LastSyncError = detailedErrorMessage;
                await db.SaveChangesAsync();
            }
            catch
            {
                // Suppress nested DB update failure
            }
            throw;
        }
    }
}



