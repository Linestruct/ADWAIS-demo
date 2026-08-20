// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "global_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    last_polled = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    litium_fetch_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    uptime_robot_fetch_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    litium_fetch_interval_minutes = table.Column<int>(type: "integer", nullable: false),
                    latency_degraded_floor = table.Column<int>(type: "integer", nullable: true),
                    uptime_robot_api_key = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    uptime_fetch_interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    latency_fetch_interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    user_stats_fetch_interval_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    system_event_retention_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    monitors_count = table.Column<int>(type: "integer", nullable: true),
                    monitors_limit = table.Column<int>(type: "integer", nullable: true),
                    active_subscription = table.Column<string>(type: "text", nullable: true),
                    last_sync_error = table.Column<string>(type: "text", nullable: true),
                    default_uptime_sla = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_global_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "kiosk_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    device_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    activation_code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    activation_code_expires = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_authorized = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    authorized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kiosk_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Mixed"),
                    litium_base_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    service_account_token = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    fetched_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fetched_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_polled = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    order_fetching_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    currently_fetching = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_sync_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    entra_object_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "monitor",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    uptime_sla = table.Column<double>(type: "double precision", nullable: true),
                    uptime_monitor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    last_update = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_uptime_update = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_latency_update = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    update_interval = table.Column<int>(type: "integer", nullable: false, defaultValue: 300),
                    latency_degraded_floor = table.Column<int>(type: "integer", nullable: true),
                    tags = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'"),
                    last_sync_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monitor", x => x.id);
                    table.ForeignKey(
                        name: "fk_monitor_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_system_id = table.Column<Guid>(type: "uuid", nullable: true),
                    order_state = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    litium_order_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    total_value_inc_vat = table.Column<decimal>(type: "numeric", nullable: false),
                    total_value_exc_vat = table.Column<decimal>(type: "numeric", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "system_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    details = table.Column<string>(type: "text", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_event", x => x.id);
                    table.ForeignKey(
                        name: "fk_system_event_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "office_event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    location = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    start_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    end_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_important = table.Column<bool>(type: "boolean", nullable: false),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false),
                    is_special = table.Column<bool>(type: "boolean", nullable: false),
                    recurrence = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_office_event", x => x.id);
                    table.ForeignKey(
                        name: "fk_office_event_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "office_message",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    content = table.Column<string>(type: "text", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_office_message", x => x.id);
                    table.ForeignKey(
                        name: "fk_office_message_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "office_visit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    guest_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    company = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    logo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    visit_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_office_visit", x => x.id);
                    table.ForeignKey(
                        name: "fk_office_visit_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "monitor_availability",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    monitor_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    uptime_percentage = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_monitor_availability", x => x.id);
                    table.ForeignKey(
                        name: "fk_monitor_availability_monitors_monitor_id",
                        column: x => x.monitor_id,
                        principalTable: "monitor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "response_time",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    monitor_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    average = table.Column<double>(type: "double precision", nullable: true),
                    lowest = table.Column<double>(type: "double precision", nullable: true),
                    highest = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_response_time", x => x.id);
                    table.ForeignKey(
                        name: "fk_response_time_monitors_monitor_id",
                        column: x => x.monitor_id,
                        principalTable: "monitor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tenant",
                columns: new[] { "id", "fetched_from", "fetched_until", "last_polled", "last_sync_error", "litium_base_url", "name", "order_fetching_enabled", "service_account_token" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), null, null, null, null, null, "System (unassigned monitors)", false, null });

            migrationBuilder.CreateIndex(
                name: "ix_kiosk_devices_activation_code",
                table: "kiosk_devices",
                column: "activation_code");

            migrationBuilder.CreateIndex(
                name: "ix_kiosk_devices_device_id",
                table: "kiosk_devices",
                column: "device_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_monitor_tenant_id",
                table: "monitor",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_monitor_availability_monitor_id_date",
                table: "monitor_availability",
                columns: new[] { "monitor_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_office_event_created_by_user_id",
                table: "office_event",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_office_message_created_by_user_id",
                table: "office_message",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_office_visit_created_by_user_id",
                table: "office_visit",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_composite_dash",
                table: "orders",
                columns: new[] { "created_date", "tenant_id" })
                .Annotation("Npgsql:IndexInclude", new[] { "total_value_inc_vat" });

            migrationBuilder.CreateIndex(
                name: "idx_orders_tenant_isolated",
                table: "orders",
                columns: new[] { "tenant_id", "created_date" })
                .Annotation("Npgsql:IndexInclude", new[] { "total_value_inc_vat" });

            migrationBuilder.CreateIndex(
                name: "idx_orders_value_dist",
                table: "orders",
                columns: new[] { "tenant_id", "total_value_inc_vat" });

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_created_date_order_state",
                table: "orders",
                columns: new[] { "tenant_id", "created_date", "order_state" });

            migrationBuilder.CreateIndex(
                name: "ix_response_time_monitor_id",
                table: "response_time",
                column: "monitor_id");

            migrationBuilder.CreateIndex(
                name: "ix_system_event_tenant_id",
                table: "system_event",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_system_event_timestamp",
                table: "system_event",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "ix_users_entra_object_id",
                table: "users",
                column: "entra_object_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "global_config");

            migrationBuilder.DropTable(
                name: "kiosk_devices");

            migrationBuilder.DropTable(
                name: "monitor_availability");

            migrationBuilder.DropTable(
                name: "office_event");

            migrationBuilder.DropTable(
                name: "office_message");

            migrationBuilder.DropTable(
                name: "office_visit");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "response_time");

            migrationBuilder.DropTable(
                name: "system_event");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "monitor");

            migrationBuilder.DropTable(
                name: "tenant");
        }
    }
}
