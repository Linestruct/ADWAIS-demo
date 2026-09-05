// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGlobalMonitorThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "default_uptime_sla",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "latency_degraded_floor",
                table: "global_config");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "default_uptime_sla",
                table: "global_config",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "latency_degraded_floor",
                table: "global_config",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "global_config",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "default_uptime_sla", "latency_degraded_floor" },
                values: new object[] { null, null });
        }
    }
}
