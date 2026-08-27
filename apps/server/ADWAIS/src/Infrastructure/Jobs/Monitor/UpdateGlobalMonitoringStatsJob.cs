// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

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

        var orgConfigs = await db.OrganizationConfigs
            .Where(c => c.MonitoringProviderSettings != null && c.MonitoringFetchEnabled)
            .ToListAsync();

        foreach (var orgConfig in orgConfigs)
        {
            try
            {
                var user = await monitoringProviders.ForProvider(orgConfig.MonitoringProvider)
                    .GetAccountDetailsAsync(orgConfig.OrganizationId);
                orgConfig.MonitorsCount = user.MonitorsCount;
                orgConfig.MonitorsLimit = user.MonitorLimit;
                orgConfig.ActiveSubscription = user.ActiveSubscriptionPlan;
                orgConfig.LastSyncError = null;
                await db.SaveChangesAsync();
                logger.LogInformation("Updated monitoring account stats for org {OrgId}: {Count}/{Limit} ({Sub})",
                    orgConfig.OrganizationId, user.MonitorsCount, user.MonitorLimit, user.ActiveSubscriptionPlan);
            }
            catch (Exception ex)
            {
                var detailedErrorMessage = $"Failed to update monitoring account stats for org {orgConfig.OrganizationId}: {ex.Message}";
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
                    orgConfig.LastSyncError = detailedErrorMessage;
                    await db.SaveChangesAsync();
                }
                catch
                {
                    // Suppress nested DB update failure
                }
            }
        }
    }
}