// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StoreOfficeEventTypeAsString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_important",
                table: "office_event");

            migrationBuilder.DropColumn(
                name: "is_special",
                table: "office_event");

            migrationBuilder.Sql("""
                ALTER TABLE office_event
                ALTER COLUMN event_type TYPE varchar(20)
                USING CASE event_type
                    WHEN 0 THEN 'General'
                    WHEN 1 THEN 'Meeting'
                    WHEN 2 THEN 'Fika'
                    WHEN 3 THEN 'Social'
                    WHEN 4 THEN 'Birthday'
                    WHEN 5 THEN 'GoLive'
                    WHEN 6 THEN 'ExternalSync'
                    ELSE 'General'
                END;
                ALTER TABLE office_event ALTER COLUMN event_type SET DEFAULT 'General';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_office_event_event_type",
                table: "office_event",
                sql: "\"event_type\" IN ('General', 'Meeting', 'Fika', 'Social', 'Birthday', 'GoLive', 'ExternalSync')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_office_event_event_type",
                table: "office_event");

            migrationBuilder.Sql("""
                ALTER TABLE office_event ALTER COLUMN event_type DROP DEFAULT;
                ALTER TABLE office_event
                ALTER COLUMN event_type TYPE integer
                USING CASE event_type
                    WHEN 'General' THEN 0
                    WHEN 'Meeting' THEN 1
                    WHEN 'Fika' THEN 2
                    WHEN 'Social' THEN 3
                    WHEN 'Birthday' THEN 4
                    WHEN 'GoLive' THEN 5
                    WHEN 'ExternalSync' THEN 6
                    ELSE 0
                END;
                """);

            migrationBuilder.AddColumn<bool>(
                name: "is_important",
                table: "office_event",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_special",
                table: "office_event",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
