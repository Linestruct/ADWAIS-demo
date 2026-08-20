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
    public partial class AddProviderIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_monitor_tenant_id",
                table: "monitor");

            migrationBuilder.RenameColumn(
                name: "litium_order_id",
                table: "orders",
                newName: "order_number");

            migrationBuilder.AddColumn<string>(
                name: "order_provider",
                table: "tenant",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "litium");

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "monitor",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider",
                table: "monitor",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "monitoring_provider",
                table: "global_config",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "uptimerobot");

            migrationBuilder.UpdateData(
                table: "global_config",
                keyColumn: "id",
                keyValue: 1,
                column: "monitoring_provider",
                value: "uptimerobot");

            migrationBuilder.UpdateData(
                table: "tenant",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "order_provider",
                value: "litium");

            migrationBuilder.Sql("""
                UPDATE orders
                SET provider = 'litium', external_id = id::text;

                UPDATE monitor
                SET provider = CASE WHEN id < 0 THEN 'demo' ELSE 'uptimerobot' END,
                    external_id = id::text;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "external_id",
                table: "orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "external_id",
                table: "monitor",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "provider",
                table: "monitor",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_orders_tenant_id_provider_external_id",
                table: "orders",
                columns: new[] { "tenant_id", "provider", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_monitor_tenant_id_provider_external_id",
                table: "monitor",
                columns: new[] { "tenant_id", "provider", "external_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orders_tenant_id_provider_external_id",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_monitor_tenant_id_provider_external_id",
                table: "monitor");

            migrationBuilder.RenameColumn(
                name: "order_number",
                table: "orders",
                newName: "litium_order_id");

            migrationBuilder.DropColumn(
                name: "order_provider",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "provider",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "provider",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "monitoring_provider",
                table: "global_config");

            migrationBuilder.CreateIndex(
                name: "ix_monitor_tenant_id",
                table: "monitor",
                column: "tenant_id");
        }
    }
}
