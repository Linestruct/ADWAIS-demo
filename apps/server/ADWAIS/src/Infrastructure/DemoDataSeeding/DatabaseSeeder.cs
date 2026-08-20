// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain;
using Adwais.Domain.Entities;
using Adwais.Domain.Entities.Intranet;
using Adwais.Domain.Entities.Monitoring;
using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Adwais.Infrastructure.DemoDataSeeding;

public static class DatabaseSeeder
{
    public static async Task<bool> SeedSampleDataAsync(
        AnalyticsDbContext context,
        DemoSeedProgress progress)
    {
        progress.StartStep(2, "Demo metadata and monitor definitions");
        var random = new Random(42);
        var forceReSeed = Environment.GetEnvironmentVariable("RESEED") == "true";
        var reportingTimeZoneId = await context.GlobalConfigs
            .Select(config => config.ReportingTimeZoneId)
            .SingleAsync();
        var reportingTimeZone = TimeZoneInfo.FindSystemTimeZoneById(reportingTimeZoneId);

        if (forceReSeed)
        {
            Console.WriteLine("Forcing re-seed of demo tenant, monitor, and order data...");
            var currentDemoNames = DemoDataCatalog.Tenants.Select(profile => profile.Name).ToArray();
            var legacyTenants = await context.Tenants
                .Where(tenant => tenant.Name.EndsWith(" [MOCK]"))
                .ToListAsync();
            var demoTenantIds = await context.Tenants
                .Where(tenant => currentDemoNames.Contains(tenant.Name) || tenant.Name.EndsWith(" [MOCK]"))
                .Select(tenant => tenant.Id)
                .ToListAsync();

            await context.Orders
                .Where(order => demoTenantIds.Contains(order.TenantId))
                .ExecuteDeleteAsync();
            await context.Monitors
                .Where(monitor => monitor.Id < 0)
                .ExecuteDeleteAsync();

            var legacyTenantIdsWithLiveMonitors = await context.Monitors
                .Where(monitor => monitor.Id > 0 && demoTenantIds.Contains(monitor.TenantId))
                .Select(monitor => monitor.TenantId)
                .Distinct()
                .ToListAsync();
            context.Tenants.RemoveRange(
                legacyTenants.Where(tenant => !legacyTenantIdsWithLiveMonitors.Contains(tenant.Id)));
            await context.SaveChangesAsync();
        }

        if (!await context.FeedSources.AnyAsync())
        {
            context.FeedSources.AddRange(
                new FeedSource { Id = Guid.NewGuid(), Name = "Litium Blog", Url = "https://www.litium.com/blog", IsActive = true },
                new FeedSource { Id = Guid.NewGuid(), Name = "Litium Reports & Guides", Url = "https://www.litium.com/reports-and-guides", IsActive = true },
                new FeedSource { Id = Guid.NewGuid(), Name = "Litium Cision News", Url = "https://news.cision.com/se/litium/ListItems?format=rss", IsActive = true },
                new FeedSource { Id = Guid.NewGuid(), Name = "Motillo Aktuellt", Url = "https://www.motillo.com/sv/aktuellt", IsActive = true },
                new FeedSource { Id = Guid.NewGuid(), Name = "Litium Nyhetsrum", Url = "https://www.litium.se/nyhetsrum", IsActive = true },
                new FeedSource { Id = Guid.NewGuid(), Name = "Litium Rapporter & Guider (SV)", Url = "https://www.litium.se/insikter/rapporter-guider", IsActive = true }
            );
            await context.SaveChangesAsync();
        }

        if (!forceReSeed && await context.Tenants.AnyAsync(tenant => tenant.Name.EndsWith(" [MOCK]")))
        {
            Console.WriteLine("Legacy demo data detected. Set RESEED=true to replace it with the current demo portfolio.");
            progress.CompleteStep("Legacy demo data requires RESEED=true.");
            progress.SkipStep(3, "Availability history", "legacy demo data");
            progress.SkipStep(4, "Latency history", "legacy demo data");
            progress.SkipStep(5, "Financial order history", "legacy demo data");
            progress.SkipStep(6, "Order indexes", "legacy demo data");
            return false;
        }

        var tenants = await SeedTenantsAsync(context);
        var monitoringHistorySeeded = await SeedMonitorsAndMetricsAsync(context, tenants, random, progress);

        var seededTenantIds = tenants.Select(tenant => tenant.Id).ToArray();
        if (!forceReSeed && await context.Orders.AnyAsync(order => seededTenantIds.Contains(order.TenantId)))
        {
            progress.SkipStep(5, "Financial order history", "demo orders already exist");
            progress.SkipStep(6, "Order indexes", "financial order history was not rebuilt");
            return monitoringHistorySeeded;
        }

        var endDate = DemoDataSimulation.FloorToFinancialInterval(DateTimeOffset.UtcNow);
        var startDate = endDate.AddMonths(-24);

        progress.StartStep(5, "Financial order history");
        var orderRowsSeeded = await BulkInsertOrdersAsync(
            context,
            tenants,
            startDate,
            endDate,
            random,
            reportingTimeZone,
            progress) > 0;

        return monitoringHistorySeeded || orderRowsSeeded;
    }

    private static async Task<long> BulkInsertOrdersAsync(
        AnalyticsDbContext context,
        IReadOnlyCollection<Tenant> tenants,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Random random,
        TimeZoneInfo reportingTimeZone,
        DemoSeedProgress progress)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();

        var previousCommandTimeout = context.Database.GetCommandTimeout();
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
        var indexesDropped = false;

        try
        {
            var dropStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await DropOrderSeedIndexesAsync(context);
            indexesDropped = true;
            Console.WriteLine($"Dropped order seed indexes in {dropStopwatch.Elapsed.TotalSeconds:F1} seconds.");

            long count = 0;
            using (var writer = connection.BeginBinaryImport(
                       "COPY orders (id, tenant_id, provider, external_id, order_state, order_number, created_date, total_value_inc_vat, total_value_exc_vat, currency) FROM STDIN (FORMAT BINARY)"))
            {
                writer.Timeout = TimeSpan.FromMinutes(10);
                var streamingStopwatch = System.Diagnostics.Stopwatch.StartNew();

                foreach (var tenant in tenants)
                {
                    var profile = DemoDataCatalog.FindTenant(tenant.Name)
                        ?? throw new InvalidOperationException($"No demo profile exists for tenant '{tenant.Name}'.");
                    count += WriteOrdersForTenant(
                        writer,
                        tenant,
                        profile,
                        startDate,
                        endDate,
                        random,
                        reportingTimeZone);
                }

                Console.WriteLine(
                    $"Generated and streamed {count:N0} orders in {streamingStopwatch.Elapsed.TotalSeconds:F1} seconds.");

                var completionStopwatch = System.Diagnostics.Stopwatch.StartNew();
                writer.Complete();
                Console.WriteLine(
                    $"PostgreSQL completed the order COPY in {completionStopwatch.Elapsed.TotalSeconds:F1} seconds.");
            }

            progress.CompleteStep($"{count:N0} orders copied.");
            return count;
        }
        catch (Exception exception)
        {
            progress.FailStep(exception);
            throw;
        }
        finally
        {
            if (indexesDropped)
            {
                progress.StartStep(6, "Order indexes");
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                    await connection.OpenAsync();
                }

                try
                {
                    await CreateOrderSeedIndexesAsync(context);
                    progress.CompleteStep();
                }
                catch (Exception exception)
                {
                    progress.FailStep(exception);
                    throw;
                }
            }

            context.Database.SetCommandTimeout(previousCommandTimeout);
        }
    }

    private static int WriteOrdersForTenant(
        NpgsqlBinaryImporter writer,
        Tenant tenant,
        DemoTenantProfile profile,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        Random random,
        TimeZoneInfo reportingTimeZone)
    {
        var count = 0;
        var intervalMinutes = RuntimeDataSeederJob.FinancialSimulationIntervalMinutes;
        if (60 % intervalMinutes != 0)
            throw new InvalidOperationException("The financial simulation interval must divide evenly into one hour.");

        var slotsPerHour = 60 / intervalMinutes;
        var firstHour = new DateTimeOffset(
            startDate.Year,
            startDate.Month,
            startDate.Day,
            startDate.Hour,
            0,
            0,
            TimeSpan.Zero);

        for (var hourStart = firstHour; hourStart <= endDate; hourStart = hourStart.AddHours(1))
        {
            var firstSlot = hourStart < startDate
                ? (int)Math.Ceiling((startDate - hourStart).TotalMinutes / intervalMinutes)
                : 0;
            var lastSlotExclusive = hourStart.AddHours(1) > endDate
                ? (int)Math.Floor((endDate - hourStart).TotalMinutes / intervalMinutes) + 1
                : slotsPerHour;
            var slotCount = lastSlotExclusive - firstSlot;
            if (slotCount <= 0) continue;

            var expectedOrdersPerRun = DemoDataSimulation.GetExpectedOrderCountPerRun(
                profile,
                hourStart.AddMinutes(firstSlot * intervalMinutes),
                reportingTimeZone);
            var guaranteedOrdersPerRun = (int)expectedOrdersPerRun;
            var fractionalProbability = expectedOrdersPerRun - guaranteedOrdersPerRun;

            if (guaranteedOrdersPerRun > 0)
            {
                for (var slot = 0; slot < slotCount; slot++)
                {
                    var timestamp = hourStart.AddMinutes((firstSlot + slot) * intervalMinutes);
                    for (var orderIndex = 0; orderIndex < guaranteedOrdersPerRun; orderIndex++)
                        WriteOrder(writer, tenant.Id, profile, timestamp, orderIndex, random);
                    count += guaranteedOrdersPerRun;
                }
            }

            if (fractionalProbability <= 0) continue;

            var successfulSlot = -1;
            while (true)
            {
                successfulSlot += SampleFailuresBeforeSuccess(random, fractionalProbability) + 1;
                if (successfulSlot >= slotCount) break;

                var timestamp = hourStart.AddMinutes((firstSlot + successfulSlot) * intervalMinutes);
                WriteOrder(
                    writer,
                    tenant.Id,
                    profile,
                    timestamp,
                    guaranteedOrdersPerRun,
                    random);
                count++;
            }
        }

        return count;
    }

    private static int SampleFailuresBeforeSuccess(Random random, double successProbability)
    {
        if (successProbability >= 1) return 0;

        return (int)Math.Floor(
            Math.Log(1.0 - random.NextDouble()) /
            Math.Log(1.0 - successProbability));
    }

    private static void WriteOrder(
        NpgsqlBinaryImporter writer,
        Guid tenantId,
        DemoTenantProfile profile,
        DateTimeOffset timestamp,
        int orderIndex,
        Random random)
    {
        var valueIncVat = DemoDataSimulation.GenerateOrderValue(profile, random);
        var valueExcVat = Math.Round(valueIncVat / 1.25m, 2);

        var orderId = Guid.NewGuid();
        var externalId = $"SEED-{tenantId.ToString()[..4]}-{timestamp.Ticks}-{orderIndex}";

        writer.StartRow();
        writer.Write(orderId);
        writer.Write(tenantId);
        writer.Write(IntegrationProviders.Demo);
        writer.Write(externalId);
        writer.Write("Completed");
        writer.Write(externalId);
        writer.Write(timestamp);
        writer.Write(valueIncVat);
        writer.Write(valueExcVat);
        writer.Write("SEK");
    }

    private static Task DropOrderSeedIndexesAsync(AnalyticsDbContext context)
        => context.Database.ExecuteSqlRawAsync("""
            DROP INDEX IF EXISTS ix_orders_tenant_id_created_date_order_state;
            DROP INDEX IF EXISTS ix_orders_tenant_id_provider_external_id;
            DROP INDEX IF EXISTS idx_orders_composite_dash;
            DROP INDEX IF EXISTS idx_orders_value_dist;
            DROP INDEX IF EXISTS idx_orders_tenant_isolated;
            """);

    private static Task CreateOrderSeedIndexesAsync(AnalyticsDbContext context)
        => context.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS ix_orders_tenant_id_created_date_order_state
                ON orders (tenant_id, created_date, order_state);
            CREATE UNIQUE INDEX IF NOT EXISTS ix_orders_tenant_id_provider_external_id
                ON orders (tenant_id, provider, external_id);
            CREATE INDEX IF NOT EXISTS idx_orders_composite_dash
                ON orders (created_date, tenant_id) INCLUDE (total_value_inc_vat);
            CREATE INDEX IF NOT EXISTS idx_orders_value_dist
                ON orders (tenant_id, total_value_inc_vat);
            CREATE INDEX IF NOT EXISTS idx_orders_tenant_isolated
                ON orders (tenant_id, created_date) INCLUDE (total_value_inc_vat);
            """);

    private static async Task<List<Tenant>> SeedTenantsAsync(AnalyticsDbContext context)
    {
        var profileNames = DemoDataCatalog.Tenants.Select(profile => profile.Name).ToArray();
        var existingTenants = await context.Tenants
            .Where(tenant => profileNames.Contains(tenant.Name))
            .ToDictionaryAsync(tenant => tenant.Name);
        var tenants = new List<Tenant>();
        foreach (var profile in DemoDataCatalog.Tenants)
        {
            if (!existingTenants.TryGetValue(profile.Name, out var tenant))
            {
                tenant = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = profile.Name,
                    Type = profile.Type,
                    OrderProviderSettings = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        endpointUrl = profile.BaseUrl,
                        authorization = $"seed-token-{profile.Name.GetHashCode()}"
                    }),
                    OrderFetchingEnabled = false
                };
                context.Tenants.Add(tenant);
            }
            else
            {
                tenant.Type = profile.Type;
                tenant.OrderProviderSettings = System.Text.Json.JsonSerializer.Serialize(new
                {
                    endpointUrl = profile.BaseUrl,
                    authorization = $"seed-token-{profile.Name.GetHashCode()}"
                });
                tenant.OrderFetchingEnabled = false;
            }
            tenants.Add(tenant);
        }
        await context.SaveChangesAsync();
        return tenants;
    }

    private static async Task<bool> SeedMonitorsAndMetricsAsync(
        AnalyticsDbContext context,
        IReadOnlyCollection<Tenant> tenants,
        Random random,
        DemoSeedProgress progress)
    {
        var tenantIds = tenants.Select(tenant => tenant.Id).ToArray();
        var existingMonitorRows = await context.Monitors
            .AsNoTracking()
            .Where(monitor => tenantIds.Contains(monitor.TenantId))
            .Select(monitor => new { monitor.TenantId, monitor.Name })
            .ToListAsync();
        var existingMonitorKeys = existingMonitorRows
            .Select(monitor => (monitor.TenantId, monitor.Name))
            .ToHashSet();
        var lowestExistingId = await context.Monitors.Where(m => m.Id < 0).MinAsync(m => (int?)m.Id) ?? 0;
        var nextId = lowestExistingId - 1;
        var monitorOrdinal = 0;

        foreach (var tenant in tenants)
        {
            var profile = DemoDataCatalog.FindTenant(tenant.Name)
                ?? throw new InvalidOperationException($"No demo profile exists for tenant '{tenant.Name}'.");

            foreach (var spec in DemoDataCatalog.GetMonitors(profile))
            {
                var currentOrdinal = monitorOrdinal++;
                if (!existingMonitorKeys.Add((tenant.Id, spec.Name)))
                    continue;

                var monitorId = nextId--;
                var reliability = DemoDataCatalog.GetReliability((int)(Math.Abs((long)monitorId) - 1));

                var lastIncidentStartedAt = currentOrdinal % 6 == 0
                    ? DateTimeOffset.UtcNow.AddDays(-random.Next(4, 75)).AddMinutes(-random.Next(5, 240))
                    : (DateTimeOffset?)null;
                var lastIncidentDurationSeconds = lastIncidentStartedAt.HasValue
                    ? random.Next(90, 7_200)
                    : (long?)null;
                var degradedFloor = reliability.TypicalUptime switch
                {
                    >= 99.98 => random.Next(380, 650),
                    >= 99.95 => random.Next(280, 480),
                    >= 99.80 => random.Next(170, 260),
                    _ => random.Next(125, 190)
                };

                context.Monitors.Add(new UptimeMonitor
                {
                    Id = monitorId,
                    TenantId = tenant.Id,
                    Provider = IntegrationProviders.Demo,
                    ExternalId = monitorId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Name = spec.Name,
                    Url = spec.Url,
                    Type = UptimeMonitorTypes.Http,
                    UptimeSla = reliability.UptimeSla,
                    LatencyDegradedFloor = degradedFloor,
                    UptimeMonitorEnabled = true,
                    CreatedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(730, 1_800)),
                    UpdateInterval = 300,
                    HttpMethod = spec.HttpMethod,
                    TimeoutSeconds = spec.TimeoutSeconds,
                    SslExpiresAt = DateTimeOffset.UtcNow.AddDays(random.Next(25, 260)),
                    DomainExpiresAt = DateTimeOffset.UtcNow.AddDays(random.Next(90, 700)),
                    MonitoredRegions = spec.Regions.ToList(),
                    CurrentStateDurationSeconds = random.NextInt64(3_600, 5_000_000),
                    Tags = spec.Tags.ToList(),
                    LastIncidentId = lastIncidentStartedAt.HasValue ? $"seed-{tenant.Id:N}-{currentOrdinal}" : null,
                    LastIncidentStatus = lastIncidentStartedAt.HasValue ? "Resolved" : null,
                    LastIncidentCause = lastIncidentStartedAt.HasValue ? "HTTP timeout" : null,
                    LastIncidentReason = lastIncidentStartedAt.HasValue ? "The endpoint exceeded its response timeout." : null,
                    LastIncidentStartedAt = lastIncidentStartedAt,
                    LastIncidentDurationSeconds = lastIncidentDurationSeconds
                });
            }
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var allMonitors = await context.Monitors
            .AsNoTracking()
            .Where(monitor => monitor.Id < 0)
            .OrderByDescending(monitor => monitor.Id)
            .ToListAsync();
        var monitorIds = allMonitors.Select(monitor => monitor.Id).ToArray();
        var monitorIdsWithAvailability = (await context.MonitorAvailabilities
                .AsNoTracking()
                .Where(availability => monitorIds.Contains(availability.MonitorId))
                .Select(availability => availability.MonitorId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();
        var monitorIdsWithLatency = (await context.ResponseTimes
                .AsNoTracking()
                .Where(responseTime => monitorIds.Contains(responseTime.MonitorId))
                .Select(responseTime => responseTime.MonitorId)
                .Distinct()
                .ToListAsync())
            .ToHashSet();
        var latencySeedNow = DemoDataSimulation.FloorToLatencyInterval(DateTimeOffset.UtcNow);
        var latencyHistoryStartDate = latencySeedNow.AddMonths(-24);
        var availabilitySeedNow = DemoDataSimulation.FloorToAvailabilityInterval(DateTimeOffset.UtcNow);
        var availabilityHistoryStartDate = availabilitySeedNow.AddMonths(-24);
        progress.CompleteStep($"{allMonitors.Count:N0} demo monitors ready.");

        var availabilityTargets = allMonitors
            .Where(monitor => !monitorIdsWithAvailability.Contains(monitor.Id))
            .ToArray();
        var historyRowsSeeded = false;
        progress.StartStep(3, "Availability history");
        try
        {
            if (availabilityTargets.Length > 0)
            {
                var count = await CopyMonitorAvailabilityHistoryAsync(
                    context,
                    availabilityTargets,
                    availabilityHistoryStartDate,
                    availabilitySeedNow,
                    random);
                historyRowsSeeded = count > 0;
                progress.CompleteStep($"{count:N0} rows copied.");
            }
            else
            {
                progress.CompleteStep("Already present; no rows copied.");
            }
        }
        catch (Exception exception)
        {
            progress.FailStep(exception);
            throw;
        }

        var latencyTargets = allMonitors
            .Where(monitor => !monitorIdsWithLatency.Contains(monitor.Id))
            .ToArray();
        progress.StartStep(4, "Latency history");
        try
        {
            if (latencyTargets.Length > 0)
            {
                var count = await CopyResponseTimeHistoryAsync(
                    context,
                    latencyTargets,
                    latencyHistoryStartDate,
                    latencySeedNow,
                    random);
                historyRowsSeeded |= count > 0;
                progress.CompleteStep($"{count:N0} rows copied.");
            }
            else
            {
                progress.CompleteStep("Already present; no rows copied.");
            }
        }
        catch (Exception exception)
        {
            progress.FailStep(exception);
            throw;
        }

        return historyRowsSeeded;
    }

    private static async Task<long> CopyMonitorAvailabilityHistoryAsync(
        AnalyticsDbContext context,
        IReadOnlyCollection<UptimeMonitor> monitors,
        DateTimeOffset historyStartDate,
        DateTimeOffset historyEnd,
        Random random)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();

        using var writer = connection.BeginBinaryImport(
            "COPY monitor_availability (monitor_id, date, uptime_percentage, is_finalized) FROM STDIN (FORMAT BINARY)");
        writer.Timeout = TimeSpan.FromMinutes(10);
        long count = 0;
        var currentUtcDayStart = new DateTimeOffset(historyEnd.UtcDateTime.Date, TimeSpan.Zero);
        var simulationInterval = TimeSpan.FromMinutes(RuntimeDataSeederJob.AvailabilitySimulationIntervalMinutes);

        foreach (var monitor in monitors)
        {
            for (var timestamp = historyStartDate; timestamp <= historyEnd; timestamp = timestamp.Add(simulationInterval))
            {
                writer.StartRow();
                writer.Write(monitor.Id);
                writer.Write(timestamp);
                writer.Write(DemoDataSimulation.GenerateAvailability(monitor.Id, random));
                writer.Write(timestamp < currentUtcDayStart);
                count++;
            }
        }

        var completionStopwatch = System.Diagnostics.Stopwatch.StartNew();
        writer.Complete();
        Console.WriteLine(
            $"PostgreSQL completed the availability COPY in {completionStopwatch.Elapsed.TotalSeconds:F1} seconds.");
        return count;
    }

    private static async Task<long> CopyResponseTimeHistoryAsync(
        AnalyticsDbContext context,
        IReadOnlyCollection<UptimeMonitor> monitors,
        DateTimeOffset historyStartDate,
        DateTimeOffset historyEnd,
        Random random)
    {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();

        using var writer = connection.BeginBinaryImport(
            "COPY response_time (monitor_id, date, average, lowest, highest) FROM STDIN (FORMAT BINARY)");
        writer.Timeout = TimeSpan.FromMinutes(10);
        long count = 0;
        var simulationInterval = TimeSpan.FromMinutes(RuntimeDataSeederJob.LatencySimulationIntervalMinutes);

        foreach (var monitor in monitors)
        {
            for (var timestamp = historyStartDate; timestamp <= historyEnd; timestamp = timestamp.Add(simulationInterval))
            {
                var sample = DemoDataSimulation.GenerateLatency(monitor.Id, random);
                writer.StartRow();
                writer.Write(monitor.Id);
                writer.Write(timestamp);
                writer.Write(sample.Average);
                writer.Write(sample.Lowest);
                writer.Write(sample.Highest);
                count++;
            }
        }

        var completionStopwatch = System.Diagnostics.Stopwatch.StartNew();
        writer.Complete();
        Console.WriteLine(
            $"PostgreSQL completed the response-time COPY in {completionStopwatch.Elapsed.TotalSeconds:F1} seconds.");
        return count;
    }
}
