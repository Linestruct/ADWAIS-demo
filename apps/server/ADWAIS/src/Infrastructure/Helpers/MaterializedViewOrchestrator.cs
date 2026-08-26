// Part of the ADWAIS project, under the Business Source License 1.1.
// See /LICENSE for license information.
// SPDX-License-Identifier: BUSL-1.1

using Adwais.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adwais.Infrastructure.Helpers;

public static class MaterializedViewOrchestrator
{
    // Each rollup row carries an organization_id and is bucketed in that
    // organization's reporting timezone. One view per domain serves org
    // dashboards (filter by organization_id) and the platform total (group by
    // date over the same view).
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
            await context.Database.ExecuteSqlRawAsync("""
                CREATE MATERIALIZED VIEW v_mat_financial_daily_tenant_rollup AS
                SELECT date_trunc(
                           'day',
                           orders.created_date AT TIME ZONE reporting.time_zone_id
                       ) AT TIME ZONE reporting.time_zone_id AS created_date,
                       reporting.organization_id,
                       orders.tenant_id,
                       count(orders.id)                AS volume,
                       sum(orders.total_value_exc_vat) AS revenue
                FROM orders
                JOIN tenant ON tenant.id = orders.tenant_id
                CROSS JOIN LATERAL (
                    SELECT tenant.organization_id,
                           COALESCE(config.reporting_time_zone_id, 'UTC') AS time_zone_id,
                           date_trunc(
                               'day',
                               CURRENT_TIMESTAMP AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC')
                           ) AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC') AS current_day_start
                    FROM organization_config config
                    WHERE config.organization_id = tenant.organization_id
                ) reporting
                WHERE orders.created_date >= (reporting.current_day_start - '730 days'::interval)
                  AND orders.created_date < reporting.current_day_start
                  AND orders.order_state != 'Cancelled'
                GROUP BY 1, 2, 3
                ORDER BY 1 DESC, 2, 3;
                CREATE UNIQUE INDEX uq_v_mat_fin_tenant_rollup ON v_mat_financial_daily_tenant_rollup (created_date, organization_id, tenant_id);
                """);
        }

        if (!existingViews.Contains("v_mat_financial_daily_global_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync("""
                CREATE MATERIALIZED VIEW v_mat_financial_daily_global_rollup AS
                SELECT created_date,
                       organization_id,
                       sum(volume)  AS global_volume,
                       sum(revenue) AS global_revenue
                FROM v_mat_financial_daily_tenant_rollup
                GROUP BY 1, 2
                ORDER BY 1 DESC;
                CREATE UNIQUE INDEX uq_v_mat_fin_global_rollup ON v_mat_financial_daily_global_rollup (created_date, organization_id);
                """);
        }

        // --- Latency Domain ---
        if (!existingViews.Contains("v_mat_daily_latency_monitor_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync("""
                CREATE MATERIALIZED VIEW v_mat_daily_latency_monitor_rollup AS
                SELECT date_trunc('day', response_time.date AT TIME ZONE reporting.time_zone_id)
                           AT TIME ZONE reporting.time_zone_id AS date,
                       reporting.organization_id,
                       response_time.monitor_id,
                       avg(response_time.average) AS average,
                       percentile_cont(0.10) WITHIN GROUP (ORDER BY response_time.average) AS p10,
                       percentile_cont(0.90) WITHIN GROUP (ORDER BY response_time.average) AS p90
                FROM response_time
                JOIN monitor ON monitor.id = response_time.monitor_id
                CROSS JOIN LATERAL (
                    SELECT t.organization_id AS organization_id,
                           COALESCE(config.reporting_time_zone_id, 'UTC') AS time_zone_id,
                           date_trunc(
                               'day',
                               CURRENT_TIMESTAMP AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC')
                           ) AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC') AS current_day_start
                    FROM tenant t
                    JOIN organization_config config ON config.organization_id = t.organization_id
                    WHERE t.id = monitor.tenant_id
                ) reporting
                WHERE response_time.date >= (reporting.current_day_start - '730 days'::interval)
                  AND response_time.date < reporting.current_day_start
                GROUP BY 1, 2, 3
                ORDER BY 1 DESC, 3;
                CREATE UNIQUE INDEX uq_v_mat_lat_monitor_rollup ON v_mat_daily_latency_monitor_rollup (date, organization_id, monitor_id);
                """);
        }

        if (!existingViews.Contains("v_mat_daily_latency_tenant_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync("""
                CREATE MATERIALIZED VIEW v_mat_daily_latency_tenant_rollup AS
                SELECT date_trunc('day', rt.date AT TIME ZONE reporting.time_zone_id)
                           AT TIME ZONE reporting.time_zone_id AS date,
                       reporting.organization_id,
                       m.tenant_id,
                       avg(rt.average) AS average,
                       percentile_cont(0.10) WITHIN GROUP (ORDER BY rt.average) AS p10,
                       percentile_cont(0.90) WITHIN GROUP (ORDER BY rt.average) AS p90
                FROM response_time rt
                         JOIN monitor m ON rt.monitor_id = m.id
                CROSS JOIN LATERAL (
                    SELECT t.organization_id AS organization_id,
                           COALESCE(config.reporting_time_zone_id, 'UTC') AS time_zone_id,
                           date_trunc(
                               'day',
                               CURRENT_TIMESTAMP AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC')
                           ) AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC') AS current_day_start
                    FROM tenant t
                    JOIN organization_config config ON config.organization_id = t.organization_id
                    WHERE t.id = m.tenant_id
                ) reporting
                WHERE rt.date < reporting.current_day_start
                GROUP BY 1, 2, 3
                ORDER BY 1 DESC, 3;
                CREATE UNIQUE INDEX uq_v_mat_lat_tenant_rollup ON v_mat_daily_latency_tenant_rollup (date, organization_id, tenant_id);
                """);
        }

        if (!existingViews.Contains("v_mat_daily_latency_global_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync("""
                CREATE MATERIALIZED VIEW v_mat_daily_latency_global_rollup AS
                SELECT date_trunc('day', rt.date AT TIME ZONE reporting.time_zone_id)
                           AT TIME ZONE reporting.time_zone_id AS date,
                       reporting.organization_id,
                       avg(rt.average) AS average,
                       percentile_cont(0.10) WITHIN GROUP (ORDER BY rt.average) AS p10,
                       percentile_cont(0.90) WITHIN GROUP (ORDER BY rt.average) AS p90
                FROM response_time rt
                         JOIN monitor m ON rt.monitor_id = m.id
                CROSS JOIN LATERAL (
                    SELECT t.organization_id AS organization_id,
                           COALESCE(config.reporting_time_zone_id, 'UTC') AS time_zone_id,
                           date_trunc(
                               'day',
                               CURRENT_TIMESTAMP AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC')
                           ) AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC') AS current_day_start
                    FROM tenant t
                    JOIN organization_config config ON config.organization_id = t.organization_id
                    WHERE t.id = m.tenant_id
                ) reporting
                WHERE rt.date < reporting.current_day_start
                  AND NOT EXISTS (
                      SELECT 1 FROM tenant sys_tenant
                      WHERE sys_tenant.id = m.tenant_id AND sys_tenant.is_system)
                GROUP BY 1, 2
                ORDER BY 1 DESC;
                CREATE UNIQUE INDEX uq_v_mat_lat_global_rollup ON v_mat_daily_latency_global_rollup (date, organization_id);
                """);
        }

        // --- Availability Domain ---
        if (!existingViews.Contains("v_mat_daily_availability_monitor_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync("""
                CREATE MATERIALIZED VIEW v_mat_daily_availability_monitor_rollup AS
                SELECT date_trunc('day', monitor_availability.date AT TIME ZONE reporting.time_zone_id)
                           AT TIME ZONE reporting.time_zone_id AS date,
                       reporting.organization_id,
                       monitor_availability.monitor_id,
                       avg(monitor_availability.uptime_percentage) AS uptime_percentage
                FROM monitor_availability
                JOIN monitor ON monitor.id = monitor_availability.monitor_id
                CROSS JOIN LATERAL (
                    SELECT t.organization_id AS organization_id,
                           COALESCE(config.reporting_time_zone_id, 'UTC') AS time_zone_id,
                           date_trunc(
                               'day',
                               CURRENT_TIMESTAMP AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC')
                           ) AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC') AS current_day_start
                    FROM tenant t
                    JOIN organization_config config ON config.organization_id = t.organization_id
                    WHERE t.id = monitor.tenant_id
                ) reporting
                WHERE monitor_availability.date >= (reporting.current_day_start - '730 days'::interval)
                  AND monitor_availability.date < reporting.current_day_start
                GROUP BY 1, 2, 3
                ORDER BY 1 DESC, 3;
                CREATE UNIQUE INDEX uq_v_mat_avail_monitor_rollup ON v_mat_daily_availability_monitor_rollup (date, organization_id, monitor_id);
                """);
        }

        if (!existingViews.Contains("v_mat_daily_availability_tenant_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync("""
                CREATE MATERIALIZED VIEW v_mat_daily_availability_tenant_rollup AS
                SELECT date_trunc('day', ma.date AT TIME ZONE reporting.time_zone_id)
                           AT TIME ZONE reporting.time_zone_id AS date,
                       reporting.organization_id,
                       m.tenant_id,
                       avg(ma.uptime_percentage) AS uptime_percentage
                FROM monitor_availability ma
                         JOIN monitor m ON ma.monitor_id = m.id
                CROSS JOIN LATERAL (
                    SELECT t.organization_id AS organization_id,
                           COALESCE(config.reporting_time_zone_id, 'UTC') AS time_zone_id,
                           date_trunc(
                               'day',
                               CURRENT_TIMESTAMP AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC')
                           ) AT TIME ZONE COALESCE(config.reporting_time_zone_id, 'UTC') AS current_day_start
                    FROM tenant t
                    JOIN organization_config config ON config.organization_id = t.organization_id
                    WHERE t.id = m.tenant_id
                ) reporting
                WHERE ma.date < reporting.current_day_start
                GROUP BY 1, 2, 3
                ORDER BY 1 DESC, 3;
                CREATE UNIQUE INDEX uq_v_mat_avail_tenant_rollup ON v_mat_daily_availability_tenant_rollup (date, organization_id, tenant_id);
                """);
        }

        if (!existingViews.Contains("v_mat_daily_availability_global_rollup"))
        {
            await context.Database.ExecuteSqlRawAsync("""
                CREATE MATERIALIZED VIEW v_mat_daily_availability_global_rollup AS
                SELECT date,
                       organization_id,
                       avg(uptime_percentage) AS uptime_percentage
                FROM v_mat_daily_availability_tenant_rollup
                GROUP BY 1, 2
                ORDER BY 1 DESC;
                CREATE UNIQUE INDEX uq_v_mat_avail_global_rollup ON v_mat_daily_availability_global_rollup (date, organization_id);
                """);
        }
    }
}
