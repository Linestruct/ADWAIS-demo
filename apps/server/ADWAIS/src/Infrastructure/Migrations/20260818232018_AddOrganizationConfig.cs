using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The rollups read global_config columns that this migration drops.
            // Deployments with existing views must drop them here; the view
            // orchestrator recreates them on startup.
            migrationBuilder.Sql("""
                DROP MATERIALIZED VIEW IF EXISTS v_mat_financial_daily_global_rollup CASCADE;
                DROP MATERIALIZED VIEW IF EXISTS v_mat_financial_daily_tenant_rollup CASCADE;
                DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_latency_global_rollup CASCADE;
                DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_latency_tenant_rollup CASCADE;
                DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_latency_monitor_rollup CASCADE;
                DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_availability_global_rollup CASCADE;
                DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_availability_tenant_rollup CASCADE;
                DROP MATERIALIZED VIEW IF EXISTS v_mat_daily_availability_monitor_rollup CASCADE;
                """);

            migrationBuilder.CreateTable(
                name: "organization_config",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    weather_location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    weather_fetch_interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    reporting_time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Europe/Stockholm"),
                    monitoring_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "uptimerobot"),
                    monitoring_provider_settings = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    order_fetch_interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    uptime_fetch_interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    latency_fetch_interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    user_stats_fetch_interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    feed_fetch_interval_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    monitors_count = table.Column<int>(type: "integer", nullable: true),
                    monitors_limit = table.Column<int>(type: "integer", nullable: true),
                    active_subscription = table.Column<string>(type: "text", nullable: true),
                    last_sync_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_config", x => x.organization_id);
                    table.ForeignKey(
                        name: "fk_organization_config_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO organization_config (
                    organization_id,
                    weather_location,
                    weather_fetch_interval_minutes,
                    reporting_time_zone_id,
                    monitoring_provider,
                    monitoring_provider_settings,
                    order_fetch_interval_minutes,
                    uptime_fetch_interval_minutes,
                    latency_fetch_interval_minutes,
                    user_stats_fetch_interval_minutes,
                    feed_fetch_interval_hours,
                    monitors_count,
                    monitors_limit,
                    active_subscription,
                    last_sync_error)
                SELECT
                    o.id,
                    g.weather_location,
                    g.weather_fetch_interval_minutes,
                    g.reporting_time_zone_id,
                    g.monitoring_provider,
                    g.monitoring_provider_settings,
                    g.order_fetch_interval_minutes,
                    g.uptime_fetch_interval_minutes,
                    g.latency_fetch_interval_minutes,
                    g.user_stats_fetch_interval_minutes,
                    g.feed_fetch_interval_hours,
                    g.monitors_count,
                    g.monitors_limit,
                    g.active_subscription,
                    g.last_sync_error
                FROM organization o
                CROSS JOIN global_config g
                WHERE g.id = (SELECT MIN(id) FROM global_config);
                """);

            migrationBuilder.DropColumn(
                name: "active_subscription",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "feed_fetch_interval_hours",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "last_sync_error",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "latency_fetch_interval_minutes",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "monitoring_provider",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "monitoring_provider_settings",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "monitors_count",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "monitors_limit",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "order_fetch_interval_minutes",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "reporting_time_zone_id",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "uptime_fetch_interval_minutes",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "user_stats_fetch_interval_minutes",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "weather_fetch_interval_minutes",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "weather_location",
                table: "global_config");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "active_subscription",
                table: "global_config",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "feed_fetch_interval_hours",
                table: "global_config",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "last_sync_error",
                table: "global_config",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "latency_fetch_interval_minutes",
                table: "global_config",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "monitoring_provider",
                table: "global_config",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "uptimerobot");

            migrationBuilder.AddColumn<string>(
                name: "monitoring_provider_settings",
                table: "global_config",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "monitors_count",
                table: "global_config",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "monitors_limit",
                table: "global_config",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "order_fetch_interval_minutes",
                table: "global_config",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "reporting_time_zone_id",
                table: "global_config",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Europe/Stockholm");

            migrationBuilder.AddColumn<int>(
                name: "uptime_fetch_interval_minutes",
                table: "global_config",
                type: "integer",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<int>(
                name: "user_stats_fetch_interval_minutes",
                table: "global_config",
                type: "integer",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<int>(
                name: "weather_fetch_interval_minutes",
                table: "global_config",
                type: "integer",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddColumn<string>(
                name: "weather_location",
                table: "global_config",
                type: "text",
                nullable: true,
                defaultValue: "Karlstad");

            migrationBuilder.Sql(@"
                UPDATE global_config g
                SET
                    active_subscription = oc.active_subscription,
                    feed_fetch_interval_hours = oc.feed_fetch_interval_hours,
                    last_sync_error = oc.last_sync_error,
                    latency_fetch_interval_minutes = oc.latency_fetch_interval_minutes,
                    monitoring_provider = oc.monitoring_provider,
                    monitoring_provider_settings = oc.monitoring_provider_settings,
                    monitors_count = oc.monitors_count,
                    monitors_limit = oc.monitors_limit,
                    order_fetch_interval_minutes = oc.order_fetch_interval_minutes,
                    reporting_time_zone_id = oc.reporting_time_zone_id,
                    uptime_fetch_interval_minutes = oc.uptime_fetch_interval_minutes,
                    user_stats_fetch_interval_minutes = oc.user_stats_fetch_interval_minutes,
                    weather_fetch_interval_minutes = oc.weather_fetch_interval_minutes,
                    weather_location = oc.weather_location
                FROM organization_config oc
                WHERE oc.organization_id = (SELECT MIN(organization_id) FROM organization_config)
                  AND g.id = 1;
            ");

            migrationBuilder.DropTable(
                name: "organization_config");
        }
    }
}
