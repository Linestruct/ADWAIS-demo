// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Helpers;

public static class MaterializedViewOrchestrator
{
    public static async Task SyncViewsAsync(AnalyticsDbContext context)
    {
        // Read the catalog before rebuilding the rollups from the completed raw dataset.
        var existingViews = await context.Database
            .SqlQueryRaw<string>("SELECT matviewname FROM pg_matviews")
            .ToListAsync();

        // Rebuild all dependent rollups once, after raw data seeding has completed.
        await context.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW IF EXISTS v_mat_financial_daily_global_rollup CASCADE; DROP MATERIALIZED VIEW IF EXISTS v_mat_financial_daily_tenant_rollup CASCADE;");
        existingViews.Remove("v_mat_financial_daily_global_rollup");
        existingViews.Remove("v_mat_financial_daily_tenant_rollup");

        await context.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_latency_global_rollup CASCADE; DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_latency_tenant_rollup CASCADE; DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_latency_monitor_rollup CASCADE;");
        existingViews.Remove("v_mat_daily_latency_global_rollup");
        existingViews.Remove("v_mat_daily_latency_tenant_rollup");
        existingViews.Remove("v_mat_daily_latency_monitor_rollup");

        await context.Database.ExecuteSqlRawAsync("DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_availability_global_rollup CASCADE; DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_availability_tenant_rollup CASCADE; DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_availability_monitor_rollup CASCADE;");
        existingViews.Remove("v_mat_daily_availability_global_rollup");
        existingViews.Remove("v_mat_daily_availability_tenant_rollup");
        existingViews.Remove("v_mat_daily_availability_monitor_rollup");

        if (!existingViews.Contains("v_mat_financial_daily_tenant_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW v_mat_financial_daily_tenant_rollup AS
                WITH reporting AS (
                    SELECT time_zone_id,
                           current_local_day AT TIME ZONE time_zone_id AS current_day_start,
                           (current_local_day - '730 days'::interval) AT TIME ZONE time_zone_id AS retention_start
                    FROM (
                        SELECT reporting_time_zone_id AS time_zone_id,
                               date_trunc(
                                   'day',
                                   CURRENT_TIMESTAMP AT TIME ZONE reporting_time_zone_id
                               ) AS current_local_day
                        FROM global_config
                        WHERE id = 1
                    ) reporting_clock
                )
                SELECT date_trunc(
                           'day',
                           orders.created_date AT TIME ZONE reporting.time_zone_id
                       ) AT TIME ZONE reporting.time_zone_id AS created_date,
                       orders.tenant_id,
                       count(orders.id)                AS volume,
                       sum(orders.total_value_exc_vat) AS revenue
                FROM orders
                CROSS JOIN reporting
                WHERE orders.created_date >= reporting.retention_start
                  AND orders.created_date < reporting.current_day_start
                  AND orders.order_state != 'Cancelled'
                GROUP BY 1, 2
                ORDER BY 1 DESC, 2;
                CREATE UNIQUE INDEX uq_v_mat_fin_tenant_rollup ON v_mat_financial_daily_tenant_rollup (created_date, tenant_id);
            ");
        }

        if (!existingViews.Contains("v_mat_financial_daily_global_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW v_mat_financial_daily_global_rollup AS
                SELECT created_date,
                       sum(volume)  AS global_volume,
                       sum(revenue) AS global_revenue
                FROM v_mat_financial_daily_tenant_rollup
                GROUP BY 1
                ORDER BY 1 DESC;
                CREATE UNIQUE INDEX uq_v_mat_fin_global_rollup ON v_mat_financial_daily_global_rollup (created_date);
            ");
        }

        // --- Latency Domain ---
        if (!existingViews.Contains("v_mat_daily_latency_monitor_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW v_mat_daily_latency_monitor_rollup AS
                WITH reporting AS (
                    SELECT reporting_time_zone_id AS time_zone_id,
                           date_trunc('day', CURRENT_TIMESTAMP AT TIME ZONE reporting_time_zone_id)
                               AT TIME ZONE reporting_time_zone_id AS current_day_start,
                           (date_trunc('day', CURRENT_TIMESTAMP AT TIME ZONE reporting_time_zone_id)
                               - '730 days'::interval) AT TIME ZONE reporting_time_zone_id AS retention_start
                    FROM global_config
                    WHERE id = 1
                )
                SELECT date_trunc('day', response_time.date AT TIME ZONE reporting.time_zone_id)
                           AT TIME ZONE reporting.time_zone_id AS date,
                       monitor_id,
                       avg(average) AS average,
                       percentile_cont(0.10) WITHIN GROUP (ORDER BY average) AS p10,
                       percentile_cont(0.90) WITHIN GROUP (ORDER BY average) AS p90
                FROM response_time
                CROSS JOIN reporting
                WHERE response_time.date >= reporting.retention_start
                  AND response_time.date < reporting.current_day_start
                GROUP BY 1, 2
                ORDER BY 1 DESC, 2;
                CREATE UNIQUE INDEX uq_v_mat_lat_monitor_rollup ON v_mat_daily_latency_monitor_rollup (date, monitor_id);
            ");
        }

        if (!existingViews.Contains("v_mat_daily_latency_tenant_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW v_mat_daily_latency_tenant_rollup AS
                WITH reporting AS (
                    SELECT reporting_time_zone_id AS time_zone_id,
                           date_trunc('day', CURRENT_TIMESTAMP AT TIME ZONE reporting_time_zone_id)
                               AT TIME ZONE reporting_time_zone_id AS current_day_start
                    FROM global_config
                    WHERE id = 1
                )
                SELECT date_trunc('day', rt.date AT TIME ZONE reporting.time_zone_id)
                           AT TIME ZONE reporting.time_zone_id AS date,
                       m.tenant_id,
                       avg(rt.average) AS average,
                       percentile_cont(0.10) WITHIN GROUP (ORDER BY rt.average) AS p10,
                       percentile_cont(0.90) WITHIN GROUP (ORDER BY rt.average) AS p90
                FROM response_time rt
                         JOIN monitor m ON rt.monitor_id = m.id
                         CROSS JOIN reporting
                WHERE rt.date < reporting.current_day_start
                GROUP BY 1, 2
                ORDER BY 1 DESC, 2;
                CREATE UNIQUE INDEX uq_v_mat_lat_tenant_rollup ON v_mat_daily_latency_tenant_rollup (date, tenant_id);
            ");
        }

        if (!existingViews.Contains("v_mat_daily_latency_global_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW v_mat_daily_latency_global_rollup AS
                WITH reporting AS (
                    SELECT reporting_time_zone_id AS time_zone_id,
                           date_trunc('day', CURRENT_TIMESTAMP AT TIME ZONE reporting_time_zone_id)
                               AT TIME ZONE reporting_time_zone_id AS current_day_start
                    FROM global_config
                    WHERE id = 1
                )
                SELECT date_trunc('day', rt.date AT TIME ZONE reporting.time_zone_id)
                           AT TIME ZONE reporting.time_zone_id AS date,
                       avg(rt.average) AS average,
                       percentile_cont(0.10) WITHIN GROUP (ORDER BY rt.average) AS p10,
                       percentile_cont(0.90) WITHIN GROUP (ORDER BY rt.average) AS p90
                FROM response_time rt
                         JOIN monitor m ON rt.monitor_id = m.id
                         CROSS JOIN reporting
                WHERE rt.date < reporting.current_day_start
                  AND m.tenant_id != '00000000-0000-0000-0000-000000000001'::uuid
                GROUP BY 1
                ORDER BY 1 DESC;
                CREATE UNIQUE INDEX uq_v_mat_lat_global_rollup ON v_mat_daily_latency_global_rollup (date);
            ");
        }

        // --- Availability Domain ---
        if (!existingViews.Contains("v_mat_daily_availability_monitor_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW v_mat_daily_availability_monitor_rollup AS
                WITH reporting AS (
                    SELECT reporting_time_zone_id AS time_zone_id,
                           date_trunc('day', CURRENT_TIMESTAMP AT TIME ZONE reporting_time_zone_id)
                               AT TIME ZONE reporting_time_zone_id AS current_day_start,
                           (date_trunc('day', CURRENT_TIMESTAMP AT TIME ZONE reporting_time_zone_id)
                               - '730 days'::interval) AT TIME ZONE reporting_time_zone_id AS retention_start
                    FROM global_config
                    WHERE id = 1
                )
                SELECT date_trunc('day', monitor_availability.date AT TIME ZONE reporting.time_zone_id)
                           AT TIME ZONE reporting.time_zone_id AS date,
                       monitor_id,
                       avg(uptime_percentage) AS uptime_percentage
                FROM monitor_availability
                CROSS JOIN reporting
                WHERE monitor_availability.date >= reporting.retention_start
                  AND monitor_availability.date < reporting.current_day_start
                GROUP BY 1, 2
                ORDER BY 1 DESC, 2;
                CREATE UNIQUE INDEX uq_v_mat_avail_monitor_rollup ON v_mat_daily_availability_monitor_rollup (date, monitor_id);
            ");
        }

        if (!existingViews.Contains("v_mat_daily_availability_tenant_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW v_mat_daily_availability_tenant_rollup AS
                WITH reporting AS (
                    SELECT reporting_time_zone_id AS time_zone_id,
                           date_trunc('day', CURRENT_TIMESTAMP AT TIME ZONE reporting_time_zone_id)
                               AT TIME ZONE reporting_time_zone_id AS current_day_start
                    FROM global_config
                    WHERE id = 1
                )
                SELECT date_trunc('day', ma.date AT TIME ZONE reporting.time_zone_id)
                           AT TIME ZONE reporting.time_zone_id AS date,
                       m.tenant_id,
                       avg(ma.uptime_percentage) AS uptime_percentage
                FROM monitor_availability ma
                         JOIN monitor m ON ma.monitor_id = m.id
                         CROSS JOIN reporting
                WHERE ma.date < reporting.current_day_start
                GROUP BY 1, 2
                ORDER BY 1 DESC, 2;
                CREATE UNIQUE INDEX uq_v_mat_avail_tenant_rollup ON v_mat_daily_availability_tenant_rollup (date, tenant_id);
            ");
        }

        if (!existingViews.Contains("v_mat_daily_availability_global_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW v_mat_daily_availability_global_rollup AS
                SELECT date,
                       avg(uptime_percentage) AS uptime_percentage
                FROM v_mat_daily_availability_tenant_rollup
                GROUP BY 1
                ORDER BY 1 DESC;
                CREATE UNIQUE INDEX uq_v_mat_avail_global_rollup ON v_mat_daily_availability_global_rollup (date);
            ");
        }
    }
}


