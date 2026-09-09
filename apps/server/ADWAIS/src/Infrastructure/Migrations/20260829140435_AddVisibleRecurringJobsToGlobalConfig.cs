using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisibleRecurringJobsToGlobalConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "visible_recurring_jobs_csv",
                table: "global_config",
                type: "text",
                nullable: true,
                defaultValueSql: "'refresh-financial-materialized-views,refresh-monitoring-materialized-views,refresh-stale-materialized-views,system-event-cleanup,sync-intranet-calendars'");

            migrationBuilder.UpdateData(
                table: "global_config",
                keyColumn: "id",
                keyValue: 1,
                column: "visible_recurring_jobs_csv",
                value: "refresh-financial-materialized-views,refresh-monitoring-materialized-views,refresh-stale-materialized-views,system-event-cleanup,sync-intranet-calendars");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "visible_recurring_jobs_csv",
                table: "global_config");
        }
    }
}
