using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerOrganizationSystemTenants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                table: "tenant",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "tenant",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "is_system",
                value: true);

            migrationBuilder.Sql("""
                INSERT INTO tenant (
                    id, organization_id, name, type, order_provider, order_provider_settings,
                    image_url, fetched_from, fetched_until, last_polled,
                    order_fetching_enabled, currently_fetching, last_sync_error, is_system)
                SELECT
                    gen_random_uuid(), o.id, 'System (unassigned monitors)', 'Mixed', 'litium', NULL,
                    NULL, NULL, NULL, NULL,
                    FALSE, FALSE, NULL, TRUE
                FROM organization o
                WHERE NOT EXISTS (
                    SELECT 1 FROM tenant t WHERE t.organization_id = o.id AND t.is_system);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_system",
                table: "tenant");
        }
    }
}
