// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GlobalConfigDefaultTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "global_config",
                columns: new[] { "id", "active_subscription", "default_uptime_sla", "last_polled", "last_sync_error", "latency_degraded_floor", "latency_fetch_interval_minutes", "litium_fetch_enabled", "litium_fetch_interval_minutes", "monitors_count", "monitors_limit", "system_event_retention_days", "uptime_fetch_interval_minutes", "uptime_robot_api_key", "uptime_robot_fetch_enabled", "user_stats_fetch_interval_minutes" },
                values: new object[] { 1, null, null, null, null, null, 10, true, 60, null, null, 30, 60, null, true, 60 });

            migrationBuilder.AddCheckConstraint(
                name: "CK_GlobalConfig_SingleRow",
                table: "global_config",
                sql: "\"id\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GlobalConfig_SingleRow",
                table: "global_config");

            migrationBuilder.DeleteData(
                table: "global_config",
                keyColumn: "id",
                keyValue: 1);
        }
    }
}
