using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUserAccessRoleScopeInvariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_user_access_role",
                table: "user_access");

            // Null-org Admin rows encoded platform status under the old
            // shape. The explicit PlatformAdmin role replaces them before
            // the new constraint takes effect.
            migrationBuilder.Sql(
                "UPDATE user_access SET \"role\" = 'PlatformAdmin' WHERE \"organization_id\" IS NULL AND \"role\" = 'Admin';");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_access_role",
                table: "user_access",
                sql: "\"role\" IN ('Admin', 'Viewer', 'Employee', 'TenantViewer', 'PlatformAdmin')\r\nAND (\r\n  (\"organization_id\" IS NULL AND \"role\" = 'PlatformAdmin')\r\n  OR (\"organization_id\" IS NOT NULL AND \"role\" <> 'PlatformAdmin')\r\n)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_user_access_role",
                table: "user_access");

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_access_role",
                table: "user_access",
                sql: "\"role\" IN ('Admin', 'Viewer', 'Employee', 'TenantViewer')");
        }
    }
}
