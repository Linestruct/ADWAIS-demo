using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertVisibleRecurringJobsToKinds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "visible_recurring_jobs_csv",
                table: "global_config",
                type: "text",
                nullable: true,
                defaultValueSql: "'FinancialViewRefresh,MonitoringViewRefresh,StaleViewRefresh,SystemEventCleanup,CalendarSync'",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldDefaultValueSql: "'refresh-financial-materialized-views,refresh-monitoring-materialized-views,refresh-stale-materialized-views,system-event-cleanup,sync-intranet-calendars'");

            migrationBuilder.UpdateData(
                table: "global_config",
                keyColumn: "id",
                keyValue: 1,
                column: "visible_recurring_jobs_csv",
                value: "FinancialViewRefresh,MonitoringViewRefresh,StaleViewRefresh,SystemEventCleanup,CalendarSync");

            migrationBuilder.Sql("""
                UPDATE "global_config"
                SET "visible_recurring_jobs_csv" = COALESCE(
                    (SELECT string_agg(conv.kind, ',' ORDER BY t.ord)
                     FROM unnest(string_to_array("visible_recurring_jobs_csv", ',')) WITH ORDINALITY AS t(item, ord)
                     JOIN (VALUES
                         ('refresh-financial-materialized-views', 'FinancialViewRefresh'),
                         ('refresh-monitoring-materialized-views', 'MonitoringViewRefresh'),
                         ('refresh-stale-materialized-views', 'StaleViewRefresh'),
                         ('system-event-cleanup', 'SystemEventCleanup'),
                         ('sync-intranet-calendars', 'CalendarSync'),
                         ('dev-runtime-data-seeder', 'RuntimeDataSeeder')
                     ) AS conv(item, kind) ON conv.item = t.item),
                    'FinancialViewRefresh,MonitoringViewRefresh,StaleViewRefresh,SystemEventCleanup,CalendarSync')
                WHERE "id" = 1 AND "visible_recurring_jobs_csv" IS NOT NULL AND "visible_recurring_jobs_csv" <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "visible_recurring_jobs_csv",
                table: "global_config",
                type: "text",
                nullable: true,
                defaultValueSql: "'refresh-financial-materialized-views,refresh-monitoring-materialized-views,refresh-stale-materialized-views,system-event-cleanup,sync-intranet-calendars'",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldDefaultValueSql: "'FinancialViewRefresh,MonitoringViewRefresh,StaleViewRefresh,SystemEventCleanup,CalendarSync'");

            migrationBuilder.UpdateData(
                table: "global_config",
                keyColumn: "id",
                keyValue: 1,
                column: "visible_recurring_jobs_csv",
                value: "refresh-financial-materialized-views,refresh-monitoring-materialized-views,refresh-stale-materialized-views,system-event-cleanup,sync-intranet-calendars");
        }
    }
}
