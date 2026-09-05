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
    public partial class ConsolidateNewsletterAndCommunityPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Seed System User
            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "email", "entra_object_id", "name", "role" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000002"), "system@adwais.local", null, "System", "Employee" });

            // 2. Migrate existing newsletters to community posts
            migrationBuilder.Sql(@"
                INSERT INTO community_post (id, user_id, title, body, created_at, updated_at)
                SELECT id, '00000000-0000-0000-0000-000000000002', title, body, created_at, updated_at
                FROM newsletter;
            ");

            // 3. Drop newsletter table
            migrationBuilder.DropTable(
                name: "newsletter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.CreateTable(
                name: "newsletter",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    body = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_newsletter", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_newsletter_created_at",
                table: "newsletter",
                column: "created_at");
        }
    }
}
