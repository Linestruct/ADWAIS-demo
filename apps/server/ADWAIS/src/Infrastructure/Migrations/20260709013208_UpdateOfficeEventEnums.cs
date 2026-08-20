// Part of the ADWAIS project, licensed under the MIT License.
// Copyright (c) 2026 Marmenlind.
// See /LICENSE for license information.
// SPDX-License-Identifier: MIT

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOfficeEventEnums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE office_event SET recurrence = 
    CASE recurrence
        WHEN 'None' THEN '0'
        WHEN 'Daily' THEN '1'
        WHEN 'Weekly' THEN '2'
        WHEN 'Monthly' THEN '3'
        WHEN 'Yearly' THEN '4'
        ELSE '0'
    END;");

            migrationBuilder.Sql(@"
UPDATE office_event SET event_type = 
    CASE event_type
        WHEN 'General' THEN '0'
        WHEN 'Meeting' THEN '1'
        WHEN 'Fika' THEN '2'
        WHEN 'Social' THEN '3'
        WHEN 'Birthday' THEN '4'
        WHEN 'GoLive' THEN '5'
        WHEN 'ExternalSync' THEN '6'
        ELSE '0'
    END;");

            migrationBuilder.Sql("ALTER TABLE office_event ALTER COLUMN recurrence DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE office_event ALTER COLUMN recurrence TYPE integer USING recurrence::integer;");
            migrationBuilder.Sql("ALTER TABLE office_event ALTER COLUMN recurrence SET DEFAULT 0;");

            migrationBuilder.Sql("ALTER TABLE office_event ALTER COLUMN event_type TYPE integer USING event_type::integer;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE office_event ALTER COLUMN recurrence DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE office_event ALTER COLUMN recurrence TYPE varchar(50) USING recurrence::varchar;");
            migrationBuilder.Sql("ALTER TABLE office_event ALTER COLUMN recurrence SET DEFAULT 'None';");

            migrationBuilder.Sql("ALTER TABLE office_event ALTER COLUMN event_type TYPE varchar(50) USING event_type::varchar;");

            migrationBuilder.Sql(@"
UPDATE office_event SET recurrence = 
    CASE recurrence
        WHEN '0' THEN 'None'
        WHEN '1' THEN 'Daily'
        WHEN '2' THEN 'Weekly'
        WHEN '3' THEN 'Monthly'
        WHEN '4' THEN 'Yearly'
        ELSE 'None'
    END;");

            migrationBuilder.Sql(@"
UPDATE office_event SET event_type = 
    CASE event_type
        WHEN '0' THEN 'General'
        WHEN '1' THEN 'Meeting'
        WHEN '2' THEN 'Fika'
        WHEN '3' THEN 'Social'
        WHEN '4' THEN 'Birthday'
        WHEN '5' THEN 'GoLive'
        WHEN '6' THEN 'ExternalSync'
        ELSE 'General'
    END;");
        }
    }
}
