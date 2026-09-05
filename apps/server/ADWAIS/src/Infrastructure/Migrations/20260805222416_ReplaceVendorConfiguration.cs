// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceVendorConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "litium_base_url",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "service_account_token",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "uptime_robot_api_key",
                table: "global_config");

            migrationBuilder.AddColumn<string>(
                name: "order_provider_settings",
                table: "tenant",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "monitoring_provider_settings",
                table: "global_config",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "global_config",
                keyColumn: "id",
                keyValue: 1,
                column: "monitoring_provider_settings",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenant",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "order_provider_settings",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "order_provider_settings",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "monitoring_provider_settings",
                table: "global_config");

            migrationBuilder.AddColumn<string>(
                name: "litium_base_url",
                table: "tenant",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "service_account_token",
                table: "tenant",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "uptime_robot_api_key",
                table: "global_config",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "global_config",
                keyColumn: "id",
                keyValue: 1,
                column: "uptime_robot_api_key",
                value: null);

            migrationBuilder.UpdateData(
                table: "tenant",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "litium_base_url", "service_account_token" },
                values: new object[] { null, null });
        }
    }
}
