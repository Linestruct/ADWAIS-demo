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
    public partial class AddNewsletterAndFeedSourceObservability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_office_event_users_created_by_user_id",
                table: "office_event");

            migrationBuilder.DropTable(
                name: "office_message");

            migrationBuilder.DropTable(
                name: "office_visit");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                table: "office_event",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "ix_office_event_created_by_user_id",
                table: "office_event",
                newName: "ix_office_event_user_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_time",
                table: "office_event",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "recurrence",
                table: "office_event",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "None",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_time",
                table: "office_event",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "community_post",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_community_post", x => x.id);
                    table.ForeignKey(
                        name: "fk_community_post_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feed_source",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_polled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_success_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sync_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feed_source", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "newsletter",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_newsletter", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feed_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    feed_source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    link = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    publish_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    author = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    image_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feed_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_feed_item_feed_source_feed_source_id",
                        column: x => x.feed_source_id,
                        principalTable: "feed_source",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_office_event_end_time",
                table: "office_event",
                column: "end_time");

            migrationBuilder.CreateIndex(
                name: "ix_office_event_start_time",
                table: "office_event",
                column: "start_time");

            migrationBuilder.CreateIndex(
                name: "ix_community_post_created_at",
                table: "community_post",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_community_post_user_id",
                table: "community_post",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_feed_item_feed_source_id",
                table: "feed_item",
                column: "feed_source_id");

            migrationBuilder.CreateIndex(
                name: "ix_feed_item_link",
                table: "feed_item",
                column: "link",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_feed_item_publish_date",
                table: "feed_item",
                column: "publish_date");

            migrationBuilder.CreateIndex(
                name: "ix_feed_source_url",
                table: "feed_source",
                column: "url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_newsletter_created_at",
                table: "newsletter",
                column: "created_at");

            migrationBuilder.AddForeignKey(
                name: "fk_office_event_users_user_id",
                table: "office_event",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_office_event_users_user_id",
                table: "office_event");

            migrationBuilder.DropTable(
                name: "community_post");

            migrationBuilder.DropTable(
                name: "feed_item");

            migrationBuilder.DropTable(
                name: "newsletter");

            migrationBuilder.DropTable(
                name: "feed_source");

            migrationBuilder.DropIndex(
                name: "ix_office_event_end_time",
                table: "office_event");

            migrationBuilder.DropIndex(
                name: "ix_office_event_start_time",
                table: "office_event");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "office_event",
                newName: "created_by_user_id");

            migrationBuilder.RenameIndex(
                name: "ix_office_event_user_id",
                table: "office_event",
                newName: "ix_office_event_created_by_user_id");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "start_time",
                table: "office_event",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "recurrence",
                table: "office_event",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldDefaultValue: "None");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "end_time",
                table: "office_event",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "office_message",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    guest_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    logo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    visit_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "ix_office_message_created_by_user_id",
                table: "office_message",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_office_visit_created_by_user_id",
                table: "office_visit",
                column: "created_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_office_event_users_created_by_user_id",
                table: "office_event",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
