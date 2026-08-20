// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations;

public partial class RenameProviderScheduling : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "litium_fetch_enabled",
            table: "global_config",
            newName: "order_fetch_enabled");

        migrationBuilder.RenameColumn(
            name: "litium_fetch_interval_minutes",
            table: "global_config",
            newName: "order_fetch_interval_minutes");

        migrationBuilder.RenameColumn(
            name: "uptime_robot_fetch_enabled",
            table: "global_config",
            newName: "monitoring_fetch_enabled");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "monitoring_fetch_enabled",
            table: "global_config",
            newName: "uptime_robot_fetch_enabled");

        migrationBuilder.RenameColumn(
            name: "order_fetch_interval_minutes",
            table: "global_config",
            newName: "litium_fetch_interval_minutes");

        migrationBuilder.RenameColumn(
            name: "order_fetch_enabled",
            table: "global_config",
            newName: "litium_fetch_enabled");
    }
}
