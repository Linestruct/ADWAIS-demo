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
    public partial class AddIntranetCalendarSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "calendar_feed_token",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "calendar_subscription_id",
                table: "office_event",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_uid",
                table: "office_event",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "calendar_subscription",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_polled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_success_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sync_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_subscription", x => x.id);
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "calendar_feed_token",
                value: null);

            migrationBuilder.CreateIndex(
                name: "ix_users_calendar_feed_token",
                table: "users",
                column: "calendar_feed_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_office_event_calendar_subscription_id",
                table: "office_event",
                column: "calendar_subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_office_event_external_uid",
                table: "office_event",
                column: "external_uid");

            migrationBuilder.AddForeignKey(
                name: "fk_office_event_calendar_subscriptions_calendar_subscription_id",
                table: "office_event",
                column: "calendar_subscription_id",
                principalTable: "calendar_subscription",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_office_event_calendar_subscriptions_calendar_subscription_id",
                table: "office_event");

            migrationBuilder.DropTable(
                name: "calendar_subscription");

            migrationBuilder.DropIndex(
                name: "ix_users_calendar_feed_token",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_office_event_calendar_subscription_id",
                table: "office_event");

            migrationBuilder.DropIndex(
                name: "ix_office_event_external_uid",
                table: "office_event");

            migrationBuilder.DropColumn(
                name: "calendar_feed_token",
                table: "users");

            migrationBuilder.DropColumn(
                name: "calendar_subscription_id",
                table: "office_event");

            migrationBuilder.DropColumn(
                name: "external_uid",
                table: "office_event");
        }
    }
}
