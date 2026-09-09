using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterializedViewDirtyAndRefreshInterval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "mat_view_refresh_interval_minutes",
                table: "global_config",
                type: "integer",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.CreateTable(
                name: "materialized_view_dirty",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    marked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materialized_view_dirty", x => x.organization_id);
                    table.ForeignKey(
                        name: "fk_materialized_view_dirty_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "global_config",
                keyColumn: "id",
                keyValue: 1,
                column: "mat_view_refresh_interval_minutes",
                value: 60);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "materialized_view_dirty");

            migrationBuilder.DropColumn(
                name: "mat_view_refresh_interval_minutes",
                table: "global_config");
        }
    }
}
