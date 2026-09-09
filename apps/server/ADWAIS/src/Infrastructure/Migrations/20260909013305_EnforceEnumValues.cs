using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceEnumValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_tenant_type",
                table: "tenant",
                sql: "\"type\" IN ('Mixed', 'B2B', 'B2C')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_system_event_audience",
                table: "system_event",
                sql: "\"audience\" IN ('Platform', 'Organization', 'Tenant')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_system_event_level",
                table: "system_event",
                sql: "\"level\" IN ('Information', 'Warning', 'Error', 'Critical')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pipeline_run_kind",
                table: "pipeline_run",
                sql: "\"kind\" IN ('OrderIngestion', 'FeedRefresh', 'MonitorSync', 'AccountStats')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pipeline_run_state",
                table: "pipeline_run",
                sql: "\"state\" IN ('Pending', 'Queued', 'Running', 'RetryScheduled', 'Succeeded', 'Failed', 'Canceled', 'Skipped', 'Unknown')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pipeline_run_trigger",
                table: "pipeline_run",
                sql: "\"trigger\" IN ('Manual', 'Scheduled', 'Retry', 'System')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_order_state",
                table: "orders",
                sql: "\"order_state\" IN ('Unknown', 'Confirmed', 'PendingProcessing', 'Processing', 'Completed', 'Cancelled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_tenant_type",
                table: "tenant");

            migrationBuilder.DropCheckConstraint(
                name: "ck_system_event_audience",
                table: "system_event");

            migrationBuilder.DropCheckConstraint(
                name: "ck_system_event_level",
                table: "system_event");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pipeline_run_kind",
                table: "pipeline_run");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pipeline_run_state",
                table: "pipeline_run");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pipeline_run_trigger",
                table: "pipeline_run");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_order_state",
                table: "orders");
        }
    }
}
