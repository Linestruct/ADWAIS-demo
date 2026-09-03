// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System.Threading;
using System.Threading.Tasks;
using Adwais.Application.Common.Interfaces;
using Adwais.Application.Common.Jobs;
using Adwais.Infrastructure.DemoDataSeeding;
using Adwais.Infrastructure.Helpers;
using Adwais.Infrastructure.Jobs;
using Adwais.Infrastructure.Jobs.MaterializedViews;
using Adwais.Infrastructure.Jobs.Monitor;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Api.Extensions;

/// <summary>
/// Owns recurring job registration. Extracted from the bootstrapper so the
/// per-organization cadences and retired id cleanup are unit testable.
/// </summary>
public static class RecurringJobRegistration
{
    public static async Task RegisterAsync(
        IApplicationDbContext dbContext,
        IRecurringJobManager recurringJobManager,
        bool runtimeDataSeeding,
        CancellationToken ct = default)
    {
        recurringJobManager.RemoveIfExists("dispatch-uptimerobot-metrics");
        recurringJobManager.RemoveIfExists("sync-uptimerobot-fleet");
        recurringJobManager.RemoveIfExists("dispatch-uptimerobot-uptime");
        recurringJobManager.RemoveIfExists("dispatch-uptimerobot-latency");
        recurringJobManager.RemoveIfExists("dispatch-litium-orders");
        recurringJobManager.RemoveIfExists("sync-uptimerobot-account-stats");
        recurringJobManager.RemoveIfExists("sync-monitoring-fleet");
        recurringJobManager.RemoveIfExists("dispatch-monitoring-uptime");
        recurringJobManager.RemoveIfExists("dispatch-monitoring-latency");
        recurringJobManager.RemoveIfExists("dispatch-order-fetch");
        recurringJobManager.RemoveIfExists("sync-monitoring-account-stats");
        recurringJobManager.RemoveIfExists("aggregate-intranet-feeds");

        var orgConfigs = await dbContext.OrganizationConfigs.AsNoTracking().ToListAsync(ct);
        var globalConfig = await dbContext.GlobalConfigs.AsNoTracking().FirstOrDefaultAsync(ct);
        var matViewInterval = globalConfig?.MatViewRefreshIntervalMinutes ?? 60;

        foreach (var orgConfig in orgConfigs)
        {
            var orgId = orgConfig.OrganizationId;

            recurringJobManager.AddOrUpdate<OrderFetchDispatchJob>(
                RecurringJobId.For(RecurringJobKind.OrderFetch, orgId),
                newJob => newJob.ExecuteAsync(orgId),
                CronHelper.FromMinutes(Positive(orgConfig.OrderFetchIntervalMinutes, 60)));

            recurringJobManager.AddOrUpdate<MonitorUptimeDispatchJob>(
                RecurringJobId.For(RecurringJobKind.UptimeFetch, orgId),
                newJob => newJob.ExecuteAsync(orgId),
                CronHelper.FromMinutes(Positive(orgConfig.UptimeFetchIntervalMinutes, 60)));

            recurringJobManager.AddOrUpdate<MonitorLatencyDispatchJob>(
                RecurringJobId.For(RecurringJobKind.LatencyFetch, orgId),
                newJob => newJob.ExecuteAsync(orgId),
                CronHelper.FromMinutes(Positive(orgConfig.LatencyFetchIntervalMinutes, 10)));

            recurringJobManager.AddOrUpdate<SyncOrganizationAccountStatsJob>(
                RecurringJobId.For(RecurringJobKind.UserStatsFetch, orgId),
                newJob => newJob.ExecuteAsync(orgId),
                CronHelper.FromMinutes(Positive(orgConfig.UserStatsFetchIntervalMinutes, 60)));

            recurringJobManager.AddOrUpdate<SyncOrganizationFleetJob>(
                RecurringJobId.For(RecurringJobKind.FleetSync, orgId),
                newJob => newJob.ExecuteAsync(orgId),
                Cron.MinuteInterval(5));

            recurringJobManager.AddOrUpdate<AggregateOrganizationFeedsJob>(
                RecurringJobId.For(RecurringJobKind.FeedFetch, orgId),
                newJob => newJob.ExecuteAsync(orgId, CancellationToken.None),
                Cron.HourInterval(Positive(orgConfig.FeedFetchIntervalHours, 2)));
        }

        recurringJobManager.AddOrUpdate<RefreshMonitoringMaterializedViewJob>(
            RecurringJobId.Platform(RecurringJobKind.MonitoringViewRefresh),
            newJob => newJob.ExecuteAsync(),
            Cron.Daily);

        recurringJobManager.AddOrUpdate<RefreshFinancialMaterializedViewJob>(
            RecurringJobId.Platform(RecurringJobKind.FinancialViewRefresh),
            newJob => newJob.ExecuteAsync(),
            Cron.Daily);

        recurringJobManager.AddOrUpdate<RefreshStaleMaterializedViewsJob>(
            RecurringJobId.Platform(RecurringJobKind.StaleViewRefresh),
            newJob => newJob.ExecuteAsync(),
            CronHelper.FromMinutes(matViewInterval));

        recurringJobManager.AddOrUpdate<SystemEventCleanupJob>(
            RecurringJobId.Platform(RecurringJobKind.SystemEventCleanup),
            newJob => newJob.ExecuteAsync(),
            Cron.Daily(2));

        recurringJobManager.AddOrUpdate<CalendarSyncJob>(
            RecurringJobId.Platform(RecurringJobKind.CalendarSync),
            newJob => newJob.ExecuteAsync(CancellationToken.None),
            Cron.MinuteInterval(30));

        if (runtimeDataSeeding)
        {
            recurringJobManager.AddOrUpdate<RuntimeDataSeederJob>(
                RecurringJobId.Platform(RecurringJobKind.RuntimeDataSeeder),
                newJob => newJob.ExecuteAsync(),
                Cron.MinuteInterval(RuntimeDataSeederJob.FinancialSimulationIntervalMinutes));
        }
        else
        {
            recurringJobManager.RemoveIfExists(RecurringJobId.Platform(RecurringJobKind.RuntimeDataSeeder));
        }
    }

    private static int Positive(int value, int fallback) => value > 0 ? value : fallback;
}