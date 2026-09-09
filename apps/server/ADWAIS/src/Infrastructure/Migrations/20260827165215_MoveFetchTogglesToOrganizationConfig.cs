using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveFetchTogglesToOrganizationConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "monitoring_fetch_enabled",
                table: "global_config");

            migrationBuilder.DropColumn(
                name: "order_fetch_enabled",
                table: "global_config");

            migrationBuilder.AddColumn<bool>(
                name: "monitoring_fetch_enabled",
                table: "organization_config",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "order_fetch_enabled",
                table: "organization_config",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "monitoring_fetch_enabled",
                table: "organization_config");

            migrationBuilder.DropColumn(
                name: "order_fetch_enabled",
                table: "organization_config");

            migrationBuilder.AddColumn<bool>(
                name: "monitoring_fetch_enabled",
                table: "global_config",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "order_fetch_enabled",
                table: "global_config",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "global_config",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "monitoring_fetch_enabled", "order_fetch_enabled" },
                values: new object[] { true, true });
        }
    }
}
