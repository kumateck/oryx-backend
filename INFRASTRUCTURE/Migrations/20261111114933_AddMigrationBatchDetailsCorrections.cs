using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationBatchDetailsCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NewExpiryDate",
                table: "MigrationRetestDateChanges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewGrnNumber",
                table: "MigrationRetestDateChanges",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NewManufacturingDate",
                table: "MigrationRetestDateChanges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousGrnNumber",
                table: "MigrationRetestDateChanges",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreviousManufacturingDate",
                table: "MigrationRetestDateChanges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MigrationGrnNumber",
                table: "MaterialBatches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT source."RoleId", 'permission', 'CanUpdateMigrationBatchDetails'
                FROM "roleclaims" source
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanUpdateMigrationRetestDate'
                  AND NOT EXISTS (
                      SELECT 1 FROM "roleclaims" existing
                      WHERE existing."RoleId" = source."RoleId" AND existing."ClaimType" = 'permission'
                        AND existing."ClaimValue" = 'CanUpdateMigrationBatchDetails');

                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), 'CanUpdateMigrationBatchDetails', target."Id", granted."Type"
                FROM "roleclaims" source
                JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
                JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
                     AND target."ClaimValue" = 'CanUpdateMigrationBatchDetails'
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanUpdateMigrationRetestDate'
                  AND NOT EXISTS (
                      SELECT 1 FROM "PermissionTypes" existing
                      WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "PermissionTypes" WHERE "Key" = 'CanUpdateMigrationBatchDetails';
                DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = 'CanUpdateMigrationBatchDetails';
                """);

            migrationBuilder.DropColumn(
                name: "NewExpiryDate",
                table: "MigrationRetestDateChanges");

            migrationBuilder.DropColumn(
                name: "NewGrnNumber",
                table: "MigrationRetestDateChanges");

            migrationBuilder.DropColumn(
                name: "NewManufacturingDate",
                table: "MigrationRetestDateChanges");

            migrationBuilder.DropColumn(
                name: "PreviousGrnNumber",
                table: "MigrationRetestDateChanges");

            migrationBuilder.DropColumn(
                name: "PreviousManufacturingDate",
                table: "MigrationRetestDateChanges");

            migrationBuilder.DropColumn(
                name: "MigrationGrnNumber",
                table: "MaterialBatches");
        }
    }
}
