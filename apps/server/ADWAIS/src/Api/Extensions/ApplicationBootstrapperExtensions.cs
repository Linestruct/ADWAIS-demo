// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

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

                if (seededRows)
                {
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
                }
                else
                {
                    progress.SkipStep(7, "Materialized views", "no history or order rows were seeded");
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
            
            recurringJobManager.AddOrUpdate<MonitorSynchronizationJob>(
                "sync-monitoring-fleet",
                newJob => newJob.ExecuteAsync(), 
                Cron.MinuteInterval(5));
            
            recurringJobManager.RemoveIfExists("dispatch-uptimerobot-metrics");
            recurringJobManager.RemoveIfExists("sync-uptimerobot-fleet");
            recurringJobManager.RemoveIfExists("dispatch-uptimerobot-uptime");
            recurringJobManager.RemoveIfExists("dispatch-uptimerobot-latency");
            recurringJobManager.RemoveIfExists("dispatch-litium-orders");
            recurringJobManager.RemoveIfExists("sync-uptimerobot-account-stats");

            using (var scope = app.Services.CreateScope())
            {
                var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AnalyticsDbContext>>();
                await using var context = await dbFactory.CreateDbContextAsync();
                var config = await context.GlobalConfigs.SingleOrDefaultAsync();
                
                var uptimeInterval = config?.UptimeFetchIntervalMinutes ?? 60;
                var latencyInterval = config?.LatencyFetchIntervalMinutes ?? 10;
                var orderFetchInterval = Math.Max(1, config?.OrderFetchIntervalMinutes ?? 10);
                var userStatsInterval = config?.UserStatsFetchIntervalMinutes ?? 60;
                var feedInterval = Math.Max(1, config?.FeedFetchIntervalHours ?? 2);
                
                recurringJobManager.AddOrUpdate<UptimeDispatcherJob>(
                        "dispatch-monitoring-uptime",
                        newJob => newJob.ExecuteAsync(),
                        CronHelper.FromMinutes(uptimeInterval));

                recurringJobManager.AddOrUpdate<LatencyDispatcherJob>(
                    "dispatch-monitoring-latency",
                    newJob => newJob.ExecuteAsync(),
                    CronHelper.FromMinutes(latencyInterval));

                recurringJobManager.AddOrUpdate<OrderFetchDispatcherJob>(
                    "dispatch-order-fetch",
                    newJob => newJob.ExecuteAsync(),
                    CronHelper.FromMinutes(orderFetchInterval));

                recurringJobManager.AddOrUpdate<UpdateGlobalMonitoringStatsJob>(
                    "sync-monitoring-account-stats",
                    newJob => newJob.ExecuteAsync(),
                    CronHelper.FromMinutes(userStatsInterval));

                recurringJobManager.AddOrUpdate<RefreshMonitoringMaterializedViewJob>(
                    "refresh-monitoring-materialized-views",
                    newJob => newJob.ExecuteAsync(),
                    Cron.Daily);

                recurringJobManager.AddOrUpdate<RefreshFinancialMaterializedViewJob>(
                    "refresh-financial-materialized-views",
                    newJob => newJob.ExecuteAsync(),
                    Cron.Daily);

                recurringJobManager.AddOrUpdate<SystemEventCleanupJob>(
                    "system-event-cleanup",
                    newJob => newJob.ExecuteAsync(),
                    Cron.Daily(2));

                recurringJobManager.AddOrUpdate<FeedAggregationJob>(
                    "aggregate-intranet-feeds",
                    newJob => newJob.ExecuteAsync(CancellationToken.None),
                    Cron.HourInterval(feedInterval));

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
