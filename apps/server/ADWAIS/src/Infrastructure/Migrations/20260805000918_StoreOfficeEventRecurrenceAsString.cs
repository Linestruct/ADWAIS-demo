// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StoreOfficeEventRecurrenceAsString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE office_event ALTER COLUMN recurrence DROP DEFAULT;
                ALTER TABLE office_event ALTER COLUMN recurrence TYPE varchar(20)
                    USING CASE recurrence::text
                        WHEN 'none' THEN 'None'
                        WHEN 'daily' THEN 'Daily'
                        WHEN 'weekly' THEN 'Weekly'
                        WHEN 'monthly' THEN 'Monthly'
                        WHEN 'yearly' THEN 'Yearly'
                    END;
                ALTER TABLE office_event ALTER COLUMN recurrence SET DEFAULT 'None';
                ALTER TABLE office_event ADD CONSTRAINT ck_office_event_recurrence
                    CHECK (recurrence IN ('None', 'Daily', 'Weekly', 'Monthly', 'Yearly'));
                DROP TYPE recurrence_type;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TYPE recurrence_type AS ENUM ('daily', 'monthly', 'none', 'weekly', 'yearly');

                ALTER TABLE office_event DROP CONSTRAINT ck_office_event_recurrence;
                ALTER TABLE office_event ALTER COLUMN recurrence DROP DEFAULT;
                ALTER TABLE office_event ALTER COLUMN recurrence TYPE recurrence_type
                    USING (CASE recurrence
                        WHEN 'None' THEN 'none'
                        WHEN 'Daily' THEN 'daily'
                        WHEN 'Weekly' THEN 'weekly'
                        WHEN 'Monthly' THEN 'monthly'
                        WHEN 'Yearly' THEN 'yearly'
                    END)::recurrence_type;
                ALTER TABLE office_event ALTER COLUMN recurrence SET DEFAULT 'none'::recurrence_type;
                """);
        }
    }
}
