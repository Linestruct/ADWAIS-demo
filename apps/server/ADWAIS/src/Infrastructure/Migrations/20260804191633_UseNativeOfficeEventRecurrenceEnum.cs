// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Adwais.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseNativeOfficeEventRecurrenceEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TYPE recurrence_type AS ENUM ('daily', 'monthly', 'none', 'weekly', 'yearly');

                ALTER TABLE office_event ALTER COLUMN recurrence DROP DEFAULT;
                ALTER TABLE office_event ALTER COLUMN recurrence TYPE recurrence_type
                    USING (CASE recurrence
                        WHEN 0 THEN 'none'
                        WHEN 1 THEN 'daily'
                        WHEN 2 THEN 'weekly'
                        WHEN 3 THEN 'monthly'
                        WHEN 4 THEN 'yearly'
                        ELSE recurrence::text
                    END)::recurrence_type;
                ALTER TABLE office_event ALTER COLUMN recurrence SET DEFAULT 'none'::recurrence_type;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE office_event ALTER COLUMN recurrence DROP DEFAULT;
                ALTER TABLE office_event ALTER COLUMN recurrence TYPE integer
                    USING CASE recurrence::text
                        WHEN 'none' THEN 0
                        WHEN 'daily' THEN 1
                        WHEN 'weekly' THEN 2
                        WHEN 'monthly' THEN 3
                        WHEN 'yearly' THEN 4
                    END;
                ALTER TABLE office_event ALTER COLUMN recurrence SET DEFAULT 0;

                DROP TYPE recurrence_type;
                """);
        }
    }
}
