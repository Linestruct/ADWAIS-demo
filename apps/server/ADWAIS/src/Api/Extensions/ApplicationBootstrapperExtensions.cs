// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Infrastructure.Persistence;
using Adwais.Infrastructure.Helpers;
using Adwais.Infrastructure.Jobs.Monitor;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using System;
using System.Threading.Tasks;
using Adwais.Infrastructure.DemoDataSeeding;
using Adwais.Infrastructure.Jobs;
using Adwais.Infrastructure.Jobs.MaterializedViews;

namespace Adwais.Api.Extensions;

public static class ApplicationBootstrapperExtensions
{
    private static int Positive(int value, int fallback) => value > 0 ? value : fallback;

    public static async Task BootstrapApplicationAsync(this WebApplication app)
    {
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        var enableSeeding = configuration.GetValue<bool>("FeatureToggles:EnableRuntimeDataSeeding", false);

        using (var scope = app.Services.CreateScope())
        {
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AnalyticsDbContext>>();
            await using var context = await contextFactory.CreateDbContextAsync();
            context.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));

            if (enableSeeding)
            {
                var progress = new DemoSeedProgress(7);
                progress.StartStep(1, "Database migrations");
                try
                {
                    await context.Database.MigrateAsync();
                    progress.CompleteStep();
                }
                catch (Exception exception)
                {
                    progress.FailStep(exception);
                    throw;
                }

                var seededRows = await DatabaseSeeder.SeedSampleDataAsync(context, progress);

                // The views must exist on every boot, not only when this boot
                // seeded rows: migrations may have dropped them, and a first
                // boot against an already seeded database skips the seeder.
                progress.StartStep(7, "Materialized views");
                var previousCommandTimeout = context.Database.GetCommandTimeout();
                context.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
                try
                {
                    await MaterializedViewOrchestrator.SyncViewsAsync(context);
                    progress.CompleteStep();
                }
                catch (Exception exception)
                {
                    progress.FailStep(exception);
                    throw;
                }
                finally
                {
                    context.Database.SetCommandTimeout(previousCommandTimeout);
                }

                progress.Finish();
            }
            else
            {
                await context.Database.MigrateAsync();
                var previousCommandTimeout = context.Database.GetCommandTimeout();
                context.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
                try
                {
                    await MaterializedViewOrchestrator.SyncViewsAsync(context);
                }
                finally
                {
                    context.Database.SetCommandTimeout(previousCommandTimeout);
                }
            }
        }

        using (var connection = JobStorage.Current.GetConnection())
        {
            var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
            
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

            using (var scope = app.Services.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AnalyticsDbContext>>();
                await using var context = await dbFactory.CreateDbContextAsync();
                var orgConfigs = await context.OrganizationConfigs.AsNoTracking().ToListAsync();
                var globalConfig = await context.GlobalConfigs.AsNoTracking().FirstOrDefaultAsync();
                var matViewInterval = globalConfig?.MatViewRefreshIntervalMinutes ?? 60;

                foreach (var orgConfig in orgConfigs)
                {
                    var orgId = orgConfig.OrganizationId;

                    recurringJobManager.AddOrUpdate<OrderFetchDispatchJob>(
                        Adwais.Application.Common.Jobs.RecurringJobId.For(Adwais.Application.Common.Jobs.RecurringJobKind.OrderFetch, orgId),
                        newJob => newJob.ExecuteAsync(orgId),
                        CronHelper.FromMinutes(Positive(orgConfig.OrderFetchIntervalMinutes, 60)));

                    recurringJobManager.AddOrUpdate<MonitorUptimeDispatchJob>(
                        Adwais.Application.Common.Jobs.RecurringJobId.For(Adwais.Application.Common.Jobs.RecurringJobKind.UptimeFetch, orgId),
                        newJob => newJob.ExecuteAsync(orgId),
                        CronHelper.FromMinutes(Positive(orgConfig.UptimeFetchIntervalMinutes, 60)));

                    recurringJobManager.AddOrUpdate<MonitorLatencyDispatchJob>(
                        Adwais.Application.Common.Jobs.RecurringJobId.For(Adwais.Application.Common.Jobs.RecurringJobKind.LatencyFetch, orgId),
                        newJob => newJob.ExecuteAsync(orgId),
                        CronHelper.FromMinutes(Positive(orgConfig.LatencyFetchIntervalMinutes, 10)));

                    recurringJobManager.AddOrUpdate<SyncOrganizationAccountStatsJob>(
                        Adwais.Application.Common.Jobs.RecurringJobId.For(Adwais.Application.Common.Jobs.RecurringJobKind.UserStatsFetch, orgId),
                        newJob => newJob.ExecuteAsync(orgId),
                        CronHelper.FromMinutes(Positive(orgConfig.UserStatsFetchIntervalMinutes, 60)));

                    recurringJobManager.AddOrUpdate<SyncOrganizationFleetJob>(
                        Adwais.Application.Common.Jobs.RecurringJobId.For(Adwais.Application.Common.Jobs.RecurringJobKind.FleetSync, orgId),
                        newJob => newJob.ExecuteAsync(orgId),
                        Cron.MinuteInterval(5));

                    recurringJobManager.AddOrUpdate<AggregateOrganizationFeedsJob>(
                        Adwais.Application.Common.Jobs.RecurringJobId.For(Adwais.Application.Common.Jobs.RecurringJobKind.FeedFetch, orgId),
                        newJob => newJob.ExecuteAsync(orgId, CancellationToken.None),
                        Cron.HourInterval(Positive(orgConfig.FeedFetchIntervalHours, 2)));
                }

                recurringJobManager.AddOrUpdate<RefreshMonitoringMaterializedViewJob>(
                    "refresh-monitoring-materialized-views",
                    newJob => newJob.ExecuteAsync(),
                    Cron.Daily);

                recurringJobManager.AddOrUpdate<RefreshFinancialMaterializedViewJob>(
                    "refresh-financial-materialized-views",
                    newJob => newJob.ExecuteAsync(),
                    Cron.Daily);

                recurringJobManager.AddOrUpdate<RefreshStaleMaterializedViewsJob>(
                    "refresh-stale-materialized-views",
                    newJob => newJob.ExecuteAsync(),
                    CronHelper.FromMinutes(matViewInterval));

                recurringJobManager.AddOrUpdate<SystemEventCleanupJob>(
                    "system-event-cleanup",
                    newJob => newJob.ExecuteAsync(),
                    Cron.Daily(2));

                recurringJobManager.AddOrUpdate<CalendarSyncJob>(
                    "sync-intranet-calendars",
                    newJob => newJob.ExecuteAsync(CancellationToken.None),
                    Cron.MinuteInterval(30));

                if (enableSeeding)
                {
                    recurringJobManager.AddOrUpdate<RuntimeDataSeederJob>(
                        "dev-runtime-data-seeder",
                        newJob => newJob.ExecuteAsync(),
                        Cron.MinuteInterval(RuntimeDataSeederJob.FinancialSimulationIntervalMinutes));
                }
                else
                {
                    recurringJobManager.RemoveIfExists("dev-runtime-data-seeder");
                }
            }
        }
    }
}
