// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Jobs.Monitor;

/// <summary>
/// Platform scheduler for monitoring account stats. Fans out one per-org
/// stats job per enabled organization.
/// </summary>
public class UpdateGlobalMonitoringStatsJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IBackgroundJobClient backgroundJobClient)
{
    public async Task ExecuteAsync()
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var orgIds = await db.OrganizationConfigs
            .Where(c => c.MonitoringProviderSettings != null && c.MonitoringFetchEnabled)
            .Select(c => c.OrganizationId)
            .ToListAsync();

        foreach (var orgId in orgIds)
        {
            backgroundJobClient.Enqueue<SyncOrganizationAccountStatsJob>(
                job => job.ExecuteAsync(orgId));
        }
    }
}