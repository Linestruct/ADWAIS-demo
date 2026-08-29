// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Adwais.Infrastructure.Jobs.Monitor;

/// <summary>
/// Syncs one organization's monitor fleet with its upstream provider.
/// A single org's failure is isolated to this job and retried by Hangfire.
/// </summary>
public class SyncOrganizationAccountStatsJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IEnumerable<IMonitoringProvider> monitoringProviders,
    ILogger<SyncOrganizationAccountStatsJob> logger,
    ISystemEventService eventService) : IOrgScopedJob
{
    public async Task ExecuteAsync(Guid organizationId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var orgConfig = await db.OrganizationConfigs
            .SingleOrDefaultAsync(c => c.OrganizationId == organizationId
                && c.MonitoringProviderSettings != null
                && c.MonitoringFetchEnabled);
        if (orgConfig is null) return;

        try
        {
            var user = await monitoringProviders.ForProvider(orgConfig.MonitoringProvider)
                .GetAccountDetailsAsync(organizationId);
            orgConfig.MonitorsCount = user.MonitorsCount;
            orgConfig.MonitorsLimit = user.MonitorLimit;
            orgConfig.ActiveSubscription = user.ActiveSubscriptionPlan;
            orgConfig.LastSyncError = null;
            await db.SaveChangesAsync();
            logger.LogInformation("Updated monitoring account stats for org {OrgId}: {Count}/{Limit} ({Sub})",
                organizationId, user.MonitorsCount, user.MonitorLimit, user.ActiveSubscriptionPlan);
        }
        catch (Exception ex)
        {
            var detailedErrorMessage = $"Failed to update monitoring account stats for org {organizationId}: {ex.Message}";
            try
            {
                await eventService.LogErrorAsync(nameof(SyncOrganizationAccountStatsJob), detailedErrorMessage, ex);
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