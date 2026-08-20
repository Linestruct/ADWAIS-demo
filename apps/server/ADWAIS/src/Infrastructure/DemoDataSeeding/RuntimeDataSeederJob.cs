// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Domain.Entities.OrderData;
using Adwais.Domain.Enums;
using Adwais.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Adwais.Infrastructure.DemoDataSeeding;

public class RuntimeDataSeederJob(
    IDbContextFactory<AnalyticsDbContext> dbContextFactory,
    ILogger<RuntimeDataSeederJob> logger)
{
    public const int FinancialSimulationIntervalMinutes = 1;
    public const int LatencySimulationIntervalMinutes = 30;
    public const int AvailabilitySimulationIntervalMinutes = 24 * 60;

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync()
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        var reportingTimeZoneId = await db.GlobalConfigs
            .Select(config => config.ReportingTimeZoneId)
            .SingleAsync();
        var reportingTimeZone = TimeZoneInfo.FindSystemTimeZoneById(reportingTimeZoneId);
        
        var demoTenantNames = DemoDataCatalog.Tenants.Select(profile => profile.Name).ToArray();
        var tenants = await db.Tenants
            .Where(t => demoTenantNames.Contains(t.Name))
            .ToListAsync();

        if (!tenants.Any()) return;

        var random = new Random();
        var now = DateTimeOffset.UtcNow;
        var orders = new List<Order>();

        foreach (var tenant in tenants)
        {
            var profile = DemoDataCatalog.FindTenant(tenant.Name);
            if (profile == null) continue;

            var count = DemoDataSimulation.GenerateOrderCount(
                profile,
                now,
                reportingTimeZone,
                random);

            if (count > 0)
            {
                AddOrders(orders, tenant.Id, profile, count, now, random);
            }
        }

        if (orders.Any())
        {
            db.Orders.AddRange(orders);
            await db.SaveChangesAsync();
            
            logger.LogInformation("Added {Count} new orders across {TenantCount} tenants during runtime seeding.", 
                orders.Count, orders.Select(o => o.TenantId).Distinct().Count());
        }

        var latencyTimestamp = DemoDataSimulation.FloorToLatencyInterval(now);
        var availabilityTimestamp = DemoDataSimulation.FloorToAvailabilityInterval(now);
        await SeedDemoMonitorLatencyAsync(db, latencyTimestamp, random);
        await SeedDemoMonitorAvailabilityAsync(db, availabilityTimestamp, random);
    }

    private static async Task SeedDemoMonitorAvailabilityAsync(AnalyticsDbContext db, DateTimeOffset now, Random random)
    {
        var seededMonitors = await db.Monitors
            .AsNoTracking()
            .Where(m => m.Id < 0)
            .ToListAsync();

        if (!seededMonitors.Any()) return;

        var monitorIds = seededMonitors.Select(m => m.Id).ToArray();
        var monitorIdsAlreadySeeded = (await db.MonitorAvailabilities
                .AsNoTracking()
                .Where(sample => monitorIds.Contains(sample.MonitorId) && sample.Date == now)
                .Select(sample => sample.MonitorId)
                .ToListAsync())
            .ToHashSet();

        var availabilities = seededMonitors
            .Where(m => !monitorIdsAlreadySeeded.Contains(m.Id))
            .Select(m =>
        {
            return new MonitorAvailability
            {
                MonitorId = m.Id,
                Date = now,
                UptimePercentage = DemoDataSimulation.GenerateAvailability(m.Id, random)
            };
        }).ToList();

        db.MonitorAvailabilities.AddRange(availabilities);
        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoMonitorLatencyAsync(AnalyticsDbContext db, DateTimeOffset now, Random random)
    {
        var seededMonitors = await db.Monitors
            .AsNoTracking()
            .Where(m => m.Id < 0)
            .ToListAsync();

        if (!seededMonitors.Any()) return;

        var monitorIds = seededMonitors.Select(m => m.Id).ToArray();
        var monitorIdsAlreadySeeded = (await db.ResponseTimes
                .AsNoTracking()
                .Where(sample => monitorIds.Contains(sample.MonitorId) && sample.Date == now)
                .Select(sample => sample.MonitorId)
                .ToListAsync())
            .ToHashSet();

        var responseTimes = seededMonitors
            .Where(m => !monitorIdsAlreadySeeded.Contains(m.Id))
            .Select(m =>
        {
            var sample = DemoDataSimulation.GenerateLatency(m.Id, random);

            return new ResponseTime
            {
                MonitorId = m.Id,
                Date = now,
                Average = sample.Average,
                Lowest = sample.Lowest,
                Highest = sample.Highest
            };
        }).ToList();

        db.ResponseTimes.AddRange(responseTimes);
        await db.SaveChangesAsync();
    }

    private static void AddOrders(
        List<Order> orders,
        Guid tenantId,
        DemoTenantProfile profile,
        int count,
        DateTimeOffset now,
        Random random)
    {
        for (int i = 0; i < count; i++)
        {
            var valueIncVat = DemoDataSimulation.GenerateOrderValue(profile, random);
            decimal valueExcVat = Math.Round(valueIncVat / 1.25m, 2);

            var externalId = $"RUNTIME-{tenantId.ToString()[..4]}-{now.Ticks}-{i}";
            orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Provider = IntegrationProviders.Demo,
                ExternalId = externalId,
                OrderNumber = externalId,
                OrderState = OrderState.Completed,
                CreatedDate = now,
                TotalValueIncVat = valueIncVat,
                TotalValueExcVat = valueExcVat,
                Currency = "SEK"
            });
        }
    }
}
