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
    public partial class RenameEntraObjectIdToExternalSubjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "entra_object_id",
                table: "users",
                newName: "external_subject_id");

            migrationBuilder.RenameIndex(
                name: "ix_users_entra_object_id",
                table: "users",
                newName: "ix_users_external_subject_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE users
                ALTER COLUMN external_subject_id TYPE character varying(255)
                USING external_subject_id::text;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE users
                ALTER COLUMN external_subject_id TYPE uuid
                USING external_subject_id::uuid;
                """);

            migrationBuilder.RenameColumn(
                name: "external_subject_id",
                table: "users",
                newName: "entra_object_id");

            migrationBuilder.RenameIndex(
                name: "ix_users_external_subject_id",
                table: "users",
                newName: "ix_users_entra_object_id");
        }
    }
}
