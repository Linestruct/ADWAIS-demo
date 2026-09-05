// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitorMetadataAndTenantImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "tenant",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "current_state_duration_seconds",
                table: "monitor",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "domain_expires_at",
                table: "monitor",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "http_method",
                table: "monitor",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_incident_cause",
                table: "monitor",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "last_incident_duration_seconds",
                table: "monitor",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_incident_id",
                table: "monitor",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_incident_reason",
                table: "monitor",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_incident_started_at",
                table: "monitor",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_incident_status",
                table: "monitor",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "monitored_regions",
                table: "monitor",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ssl_expires_at",
                table: "monitor",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "timeout_seconds",
                table: "monitor",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "tenant",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "image_url",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "current_state_duration_seconds",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "domain_expires_at",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "http_method",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "last_incident_cause",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "last_incident_duration_seconds",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "last_incident_id",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "last_incident_reason",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "last_incident_started_at",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "last_incident_status",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "monitored_regions",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "ssl_expires_at",
                table: "monitor");

            migrationBuilder.DropColumn(
                name: "timeout_seconds",
                table: "monitor");
        }
    }
}
