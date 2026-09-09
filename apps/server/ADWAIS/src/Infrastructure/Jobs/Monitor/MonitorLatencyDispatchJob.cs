// Part of the ADWAIS project, licensed under the MIT License.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Application.Interfaces;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Jobs.Monitor;

/// <summary>
/// Enqueues latency collection jobs for one organization's monitors.
/// Runs on the organization's own recurring cadence.
/// </summary>
public class MonitorLatencyDispatchJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    IBackgroundJobClient backgroundJobClient) : IOrgScopedJob
{
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync(Guid organizationId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var orgConfig = await dbContext.OrganizationConfigs
            .SingleOrDefaultAsync(c => c.OrganizationId == organizationId
                && c.MonitoringProviderSettings != null
                && c.MonitoringFetchEnabled);
        if (orgConfig is null) return;

        var monitors = await dbContext.Monitors
            .Where(m => m.Id > 0 && m.UptimeMonitorEnabled && m.Tenant!.OrganizationId == organizationId)
            .Select(m => new { m.Id, m.LastLatencyUpdate })
            .ToListAsync();

        var end = DateTimeOffset.UtcNow;
        int index = 0;

        foreach (var monitor in monitors)
        {
            var start = monitor.LastLatencyUpdate ?? end.AddMinutes(-orgConfig.LatencyFetchIntervalMinutes);

            backgroundJobClient.Schedule<UpdateMonitorLatencyJob>(
                x => x.ExecuteAsync(organizationId, monitor.Id, start, end),
                TimeSpan.FromSeconds(index * 2));
            index++;
        }
    }
}