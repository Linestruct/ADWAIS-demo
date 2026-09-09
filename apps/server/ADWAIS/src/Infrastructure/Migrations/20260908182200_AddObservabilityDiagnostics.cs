using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddObservabilityDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_system_event_organization_id",
                table: "system_event");

            migrationBuilder.DropIndex(
                name: "ix_system_event_tenant_id",
                table: "system_event");

            migrationBuilder.AddColumn<string>(
                name: "audience",
                table: "system_event",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Platform");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "system_event",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "legacy");

            migrationBuilder.AddColumn<Guid>(
                name: "pipeline_run_id",
                table: "system_event",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_id",
                table: "system_event",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suggested_action",
                table: "system_event",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trace_id",
                table: "system_event",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "pipeline_run",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    trigger = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    resource_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    resource_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    hangfire_job_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    trace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_state_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_retry_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    outcome_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    safe_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    work_count = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pipeline_run", x => x.id);
                    table.ForeignKey(
                        name: "fk_pipeline_run_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pipeline_run_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_system_event_organization_id_timestamp_id",
                table: "system_event",
                columns: new[] { "organization_id", "timestamp", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_system_event_tenant_id_timestamp_id",
                table: "system_event",
                columns: new[] { "tenant_id", "timestamp", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_run_organization_id_requested_at_id",
                table: "pipeline_run",
                columns: new[] { "organization_id", "requested_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_run_organization_id_tenant_id_kind_state",
                table: "pipeline_run",
                columns: new[] { "organization_id", "tenant_id", "kind", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_pipeline_run_tenant_id",
                table: "pipeline_run",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pipeline_run");

            migrationBuilder.DropIndex(
                name: "ix_system_event_organization_id_timestamp_id",
                table: "system_event");

            migrationBuilder.DropIndex(
                name: "ix_system_event_tenant_id_timestamp_id",
                table: "system_event");

            migrationBuilder.DropColumn(
                name: "audience",
                table: "system_event");

            migrationBuilder.DropColumn(
                name: "code",
                table: "system_event");

            migrationBuilder.DropColumn(
                name: "pipeline_run_id",
                table: "system_event");

            migrationBuilder.DropColumn(
                name: "request_id",
                table: "system_event");

            migrationBuilder.DropColumn(
                name: "suggested_action",
                table: "system_event");

            migrationBuilder.DropColumn(
                name: "trace_id",
                table: "system_event");

            migrationBuilder.CreateIndex(
                name: "ix_system_event_organization_id",
                table: "system_event",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_system_event_tenant_id",
                table: "system_event",
                column: "tenant_id");
        }
    }
}
