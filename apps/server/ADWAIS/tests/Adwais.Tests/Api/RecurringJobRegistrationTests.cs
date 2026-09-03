// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Adwais.Api.Extensions;
using Adwais.Application.Common.Jobs;
using Adwais.Domain.Entities;
using Adwais.Infrastructure.DemoDataSeeding;
using Adwais.Infrastructure.Jobs;
using Adwais.Infrastructure.Jobs.MaterializedViews;
using Adwais.Infrastructure.Jobs.Monitor;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Hangfire.Common;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Adwais.Tests.Api;

public class RecurringJobRegistrationTests
{
    private readonly DbContextOptions<AnalyticsDbContext> _dbOptions;
    private readonly Mock<IRecurringJobManager> _recurringJobManager = new();
    private readonly Guid _orgA = Guid.NewGuid();
    private readonly Guid _orgB = Guid.NewGuid();

    public RecurringJobRegistrationTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private async Task SeedAsync(
        OrganizationConfig configA,
        OrganizationConfig? configB = null,
        int matViewInterval = 45)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        db.OrganizationConfigs.Add(configA);
        if (configB is not null) db.OrganizationConfigs.Add(configB);
        db.GlobalConfigs.Add(new GlobalConfig
        {
            Id = 1,
            SystemEventRetentionDays = 2,
            MatViewRefreshIntervalMinutes = matViewInterval
        });
        await db.SaveChangesAsync();
    }

    private async Task RegisterAsync(bool seeding = false)
    {
        await using var db = new AnalyticsDbContext(_dbOptions);
        await RecurringJobRegistration.RegisterAsync(db, _recurringJobManager.Object, seeding);
    }

    private void VerifyPerOrgJob<T>(string jobId, string cron) where T : class
        => _recurringJobManager.Verify(
            manager => manager.AddOrUpdate(
                jobId,
                It.Is<Job>(job => job.Type == typeof(T)),
                cron,
                It.IsAny<RecurringJobOptions>()),
            Times.Once);

    [Fact]
    public async Task RegisterAsync_PerOrgJobsUseEachOrgsOwnIntervals()
    {
        await SeedAsync(
            new OrganizationConfig { OrganizationId = _orgA, OrderFetchIntervalMinutes = 60, UptimeFetchIntervalMinutes = 60, LatencyFetchIntervalMinutes = 10, UserStatsFetchIntervalMinutes = 60, FeedFetchIntervalHours = 2 },
            new OrganizationConfig { OrganizationId = _orgB, OrderFetchIntervalMinutes = 30, UptimeFetchIntervalMinutes = 30, LatencyFetchIntervalMinutes = 20, UserStatsFetchIntervalMinutes = 30, FeedFetchIntervalHours = 4 });

        await RegisterAsync();

        VerifyPerOrgJob<OrderFetchDispatchJob>(RecurringJobId.For(RecurringJobKind.OrderFetch, _orgA), "0 * * * *");
        VerifyPerOrgJob<OrderFetchDispatchJob>(RecurringJobId.For(RecurringJobKind.OrderFetch, _orgB), "*/30 * * * *");
        VerifyPerOrgJob<MonitorUptimeDispatchJob>(RecurringJobId.For(RecurringJobKind.UptimeFetch, _orgB), "*/30 * * * *");
        VerifyPerOrgJob<MonitorLatencyDispatchJob>(RecurringJobId.For(RecurringJobKind.LatencyFetch, _orgA), "*/10 * * * *");
        VerifyPerOrgJob<MonitorLatencyDispatchJob>(RecurringJobId.For(RecurringJobKind.LatencyFetch, _orgB), "*/20 * * * *");
        VerifyPerOrgJob<SyncOrganizationAccountStatsJob>(RecurringJobId.For(RecurringJobKind.UserStatsFetch, _orgA), "0 * * * *");
        VerifyPerOrgJob<SyncOrganizationFleetJob>(RecurringJobId.For(RecurringJobKind.FleetSync, _orgB), "*/5 * * * *");
        VerifyPerOrgJob<AggregateOrganizationFeedsJob>(RecurringJobId.For(RecurringJobKind.FeedFetch, _orgA), "0 */2 * * *");
        VerifyPerOrgJob<AggregateOrganizationFeedsJob>(RecurringJobId.For(RecurringJobKind.FeedFetch, _orgB), "0 */4 * * *");
    }

    [Fact]
    public async Task RegisterAsync_RegistersPlatformJobsFromStoredConfig()
    {
        await SeedAsync(new OrganizationConfig { OrganizationId = _orgA });

        await RegisterAsync();

        VerifyPerOrgJob<RefreshMonitoringMaterializedViewJob>(RecurringJobId.Platform(RecurringJobKind.MonitoringViewRefresh), "0 0 * * *");
        VerifyPerOrgJob<RefreshFinancialMaterializedViewJob>(RecurringJobId.Platform(RecurringJobKind.FinancialViewRefresh), "0 0 * * *");
        VerifyPerOrgJob<RefreshStaleMaterializedViewsJob>(RecurringJobId.Platform(RecurringJobKind.StaleViewRefresh), "*/45 * * * *");
        VerifyPerOrgJob<SystemEventCleanupJob>(RecurringJobId.Platform(RecurringJobKind.SystemEventCleanup), "0 2 * * *");
        VerifyPerOrgJob<CalendarSyncJob>(RecurringJobId.Platform(RecurringJobKind.CalendarSync), "*/30 * * * *");
    }

    [Fact]
    public async Task RegisterAsync_RemovesRetiredGlobalJobIds()
    {
        await SeedAsync(new OrganizationConfig { OrganizationId = _orgA });

        await RegisterAsync();

        foreach (var retired in new[]
            {
                "dispatch-uptimerobot-metrics", "sync-uptimerobot-fleet",
                "dispatch-uptimerobot-uptime", "dispatch-uptimerobot-latency",
                "dispatch-litium-orders", "sync-uptimerobot-account-stats",
                "sync-monitoring-fleet", "dispatch-monitoring-uptime",
                "dispatch-monitoring-latency", "dispatch-order-fetch",
                "sync-monitoring-account-stats", "aggregate-intranet-feeds"
            })
        {
            _recurringJobManager.Verify(manager => manager.RemoveIfExists(retired), Times.Once);
        }
    }

    [Fact]
    public async Task RegisterAsync_OrgWithoutConfigRow_GetsDefaultsRowAndJobs()
    {
        await using (var db = new AnalyticsDbContext(_dbOptions))
        {
            db.Organizations.Add(new Adwais.Domain.Entities.Organization
            {
                Id = _orgA,
                Name = "Org A",
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
            await db.SaveChangesAsync();
        }

        await RegisterAsync();

        await using (var verify = new AnalyticsDbContext(_dbOptions))
        {
            Assert.True(await verify.OrganizationConfigs.AnyAsync(c => c.OrganizationId == _orgA));
        }

        VerifyPerOrgJob<OrderFetchDispatchJob>(RecurringJobId.For(RecurringJobKind.OrderFetch, _orgA), "0 * * * *");
        VerifyPerOrgJob<MonitorUptimeDispatchJob>(RecurringJobId.For(RecurringJobKind.UptimeFetch, _orgA), "0 * * * *");
        VerifyPerOrgJob<MonitorLatencyDispatchJob>(RecurringJobId.For(RecurringJobKind.LatencyFetch, _orgA), "*/10 * * * *");
        VerifyPerOrgJob<SyncOrganizationAccountStatsJob>(RecurringJobId.For(RecurringJobKind.UserStatsFetch, _orgA), "0 * * * *");
        VerifyPerOrgJob<SyncOrganizationFleetJob>(RecurringJobId.For(RecurringJobKind.FleetSync, _orgA), "*/5 * * * *");
        VerifyPerOrgJob<AggregateOrganizationFeedsJob>(RecurringJobId.For(RecurringJobKind.FeedFetch, _orgA), "0 */2 * * *");
    }

    [Fact]
    public async Task RegisterAsync_SeedingEnabled_RegistersSeederJob()
    {
        await SeedAsync(new OrganizationConfig { OrganizationId = _orgA });

        await RegisterAsync(seeding: true);

        VerifyPerOrgJob<RuntimeDataSeederJob>(RecurringJobId.Platform(RecurringJobKind.RuntimeDataSeeder), "*/1 * * * *");
    }

    [Fact]
    public async Task RegisterAsync_SeedingDisabled_RemovesSeederJob()
    {
        await SeedAsync(new OrganizationConfig { OrganizationId = _orgA });

        await RegisterAsync(seeding: false);

        _recurringJobManager.Verify(
            manager => manager.RemoveIfExists(RecurringJobId.Platform(RecurringJobKind.RuntimeDataSeeder)),
            Times.Once);
    }
}