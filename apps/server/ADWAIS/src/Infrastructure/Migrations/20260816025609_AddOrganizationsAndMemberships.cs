using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Adwais.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationsAndMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_feed_source_url",
                table: "feed_source");

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "tenant",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "system_event",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "kiosk_devices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "feed_source",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "calendar_subscription",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "calendar_event",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "bulletin_post",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "organization",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_access", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_access_organization_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_access_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_access_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "organization",
                columns: new[] { "id", "created_at", "name" },
                values: new object[] { new Guid("00000000-0000-0000-0000-00000000000a"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Default Organization" });

            migrationBuilder.UpdateData(
                table: "tenant",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "organization_id",
                value: new Guid("00000000-0000-0000-0000-00000000000a"));

            migrationBuilder.Sql("""
                UPDATE tenant SET organization_id = '00000000-0000-0000-0000-00000000000a';
                UPDATE kiosk_devices SET organization_id = '00000000-0000-0000-0000-00000000000a';
                UPDATE feed_source SET organization_id = '00000000-0000-0000-0000-00000000000a';
                UPDATE calendar_subscription SET organization_id = '00000000-0000-0000-0000-00000000000a';
                UPDATE calendar_event SET organization_id = '00000000-0000-0000-0000-00000000000a';
                UPDATE bulletin_post SET organization_id = '00000000-0000-0000-0000-00000000000a';

                INSERT INTO user_access (id, user_id, organization_id, tenant_id, role, created_at)
                SELECT uuid_generate_v4(), id, NULL, NULL, role, now() FROM users WHERE role = 'Admin';

                INSERT INTO user_access (id, user_id, organization_id, tenant_id, role, created_at)
                SELECT uuid_generate_v4(), id, '00000000-0000-0000-0000-00000000000a', NULL, role, now() FROM users WHERE role <> 'Admin';
                """);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_organization_id",
                table: "tenant",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_system_event_organization_id",
                table: "system_event",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_kiosk_devices_organization_id",
                table: "kiosk_devices",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_feed_source_organization_id_url",
                table: "feed_source",
                columns: new[] { "organization_id", "url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_calendar_subscription_organization_id",
                table: "calendar_subscription",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_calendar_event_organization_id",
                table: "calendar_event",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_bulletin_post_organization_id",
                table: "bulletin_post",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_access_organization_id",
                table: "user_access",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_access_tenant_id",
                table: "user_access",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_access_user_id",
                table: "user_access",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_bulletin_post_organization_organization_id",
                table: "bulletin_post",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_calendar_event_organization_organization_id",
                table: "calendar_event",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_calendar_subscription_organization_organization_id",
                table: "calendar_subscription",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_feed_source_organization_organization_id",
                table: "feed_source",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_kiosk_devices_organization_organization_id",
                table: "kiosk_devices",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_system_event_organizations_organization_id",
                table: "system_event",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tenant_organizations_organization_id",
                table: "tenant",
                column: "organization_id",
                principalTable: "organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_bulletin_post_organization_organization_id",
                table: "bulletin_post");

            migrationBuilder.DropForeignKey(
                name: "fk_calendar_event_organization_organization_id",
                table: "calendar_event");

            migrationBuilder.DropForeignKey(
                name: "fk_calendar_subscription_organization_organization_id",
                table: "calendar_subscription");

            migrationBuilder.DropForeignKey(
                name: "fk_feed_source_organization_organization_id",
                table: "feed_source");

            migrationBuilder.DropForeignKey(
                name: "fk_kiosk_devices_organization_organization_id",
                table: "kiosk_devices");

            migrationBuilder.DropForeignKey(
                name: "fk_system_event_organizations_organization_id",
                table: "system_event");

            migrationBuilder.DropForeignKey(
                name: "fk_tenant_organizations_organization_id",
                table: "tenant");

            migrationBuilder.DropTable(
                name: "user_access");

            migrationBuilder.DropTable(
                name: "organization");

            migrationBuilder.DropIndex(
                name: "ix_tenant_organization_id",
                table: "tenant");

            migrationBuilder.DropIndex(
                name: "ix_system_event_organization_id",
                table: "system_event");

            migrationBuilder.DropIndex(
                name: "ix_kiosk_devices_organization_id",
                table: "kiosk_devices");

            migrationBuilder.DropIndex(
                name: "ix_feed_source_organization_id_url",
                table: "feed_source");

            migrationBuilder.DropIndex(
                name: "ix_calendar_subscription_organization_id",
                table: "calendar_subscription");

            migrationBuilder.DropIndex(
                name: "ix_calendar_event_organization_id",
                table: "calendar_event");

            migrationBuilder.DropIndex(
                name: "ix_bulletin_post_organization_id",
                table: "bulletin_post");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "system_event");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "kiosk_devices");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "feed_source");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "calendar_subscription");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "calendar_event");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "bulletin_post");

            migrationBuilder.CreateIndex(
                name: "ix_feed_source_url",
                table: "feed_source",
                column: "url",
                unique: true);
        }
    }
}
