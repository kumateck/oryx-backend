using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedMaterialDensity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DensityGramsPerMilliliter",
                table: "Materials",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DensityGramsPerMilliliter",
                table: "MaterialBatchReservedQuantities",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DensityGramsPerMilliliter",
                table: "HoldingMaterialTransferBatches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DestinationQuantity",
                table: "HoldingMaterialTransferBatches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DestinationUoMId",
                table: "HoldingMaterialTransferBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAt",
                table: "HoldingMaterialTransferBatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceivedById",
                table: "HoldingMaterialTransferBatches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TransferQuantity",
                table: "HoldingMaterialTransferBatches",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialDensityMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousDensity = table.Column<decimal>(type: "numeric", nullable: true),
                    Density = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialDensityMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialDensityMeasurements_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialDensityMeasurements_users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Material_Density",
                table: "Materials",
                sql: "\"DensityGramsPerMilliliter\" IS NULL OR \"DensityGramsPerMilliliter\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_HoldingMaterialTransferBatches_DestinationUoMId",
                table: "HoldingMaterialTransferBatches",
                column: "DestinationUoMId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialDensityMeasurements_ActorId",
                table: "MaterialDensityMeasurements",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialDensityMeasurements_MaterialId",
                table: "MaterialDensityMeasurements",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_HoldingMaterialTransferBatches_UnitOfMeasures_DestinationUo~",
                table: "HoldingMaterialTransferBatches",
                column: "DestinationUoMId",
                principalTable: "UnitOfMeasures",
                principalColumn: "Id");
            migrationBuilder.Sql("""
                UPDATE "Materials" material SET "DensityGramsPerMilliliter" = verified.density
                FROM (
                    SELECT link."MaterialId", MIN(link."Density") AS density
                    FROM "MaterialDepartments" link JOIN "UnitOfMeasures" unit ON unit."Id" = link."DensityUoMId"
                    WHERE link."DeletedAt" IS NULL AND unit."DeletedAt" IS NULL
                      AND LOWER(TRIM(unit."Symbol")) = 'g/ml' AND link."Density" > 0
                    GROUP BY link."MaterialId" HAVING COUNT(DISTINCT link."Density") = 1
                ) verified WHERE material."Id" = verified."MaterialId";
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT source."RoleId", 'permission', 'CanEditRawMaterialDensity' FROM "roleclaims" source
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanLinkRawMaterials'
                AND NOT EXISTS (SELECT 1 FROM "roleclaims" target WHERE target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission' AND target."ClaimValue" = 'CanEditRawMaterialDensity');
                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), 'CanEditRawMaterialDensity', target."Id", granted."Type"
                FROM "roleclaims" source JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
                JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimValue" = 'CanEditRawMaterialDensity' AND target."ClaimType" = 'permission'
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanLinkRawMaterials'
                AND NOT EXISTS (SELECT 1 FROM "PermissionTypes" old WHERE old."RoleClaimId" = target."Id" AND old."Type" = granted."Type");
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT source."RoleId", 'permission', 'CanEditPackagingMaterialDensity' FROM "roleclaims" source
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanLinkPackagingMaterials'
                AND NOT EXISTS (SELECT 1 FROM "roleclaims" target WHERE target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission' AND target."ClaimValue" = 'CanEditPackagingMaterialDensity');
                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), 'CanEditPackagingMaterialDensity', target."Id", granted."Type"
                FROM "roleclaims" source JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
                JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimValue" = 'CanEditPackagingMaterialDensity' AND target."ClaimType" = 'permission'
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanLinkPackagingMaterials'
                AND NOT EXISTS (SELECT 1 FROM "PermissionTypes" old WHERE old."RoleClaimId" = target."Id" AND old."Type" = granted."Type");
                CREATE FUNCTION public.oryx_material_density_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Material density measurements are append-only'; END; $$;
                CREATE TRIGGER material_density_immutable BEFORE UPDATE OR DELETE ON "MaterialDensityMeasurements"
                    FOR EACH ROW EXECUTE FUNCTION public.oryx_material_density_immutable();
                CREATE TRIGGER material_density_no_truncate BEFORE TRUNCATE ON "MaterialDensityMeasurements"
                    FOR EACH STATEMENT EXECUTE FUNCTION public.oryx_material_density_immutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS material_density_immutable ON \"MaterialDensityMeasurements\"; DROP TRIGGER IF EXISTS material_density_no_truncate ON \"MaterialDensityMeasurements\"; DROP FUNCTION IF EXISTS public.oryx_material_density_immutable();");
            migrationBuilder.DropForeignKey(
                name: "FK_HoldingMaterialTransferBatches_UnitOfMeasures_DestinationUo~",
                table: "HoldingMaterialTransferBatches");

            migrationBuilder.DropTable(
                name: "MaterialDensityMeasurements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Material_Density",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_HoldingMaterialTransferBatches_DestinationUoMId",
                table: "HoldingMaterialTransferBatches");

            migrationBuilder.DropColumn(
                name: "DensityGramsPerMilliliter",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "DensityGramsPerMilliliter",
                table: "MaterialBatchReservedQuantities");

            migrationBuilder.DropColumn(
                name: "DensityGramsPerMilliliter",
                table: "HoldingMaterialTransferBatches");

            migrationBuilder.DropColumn(
                name: "DestinationQuantity",
                table: "HoldingMaterialTransferBatches");

            migrationBuilder.DropColumn(
                name: "DestinationUoMId",
                table: "HoldingMaterialTransferBatches");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "HoldingMaterialTransferBatches");

            migrationBuilder.DropColumn(
                name: "ReceivedById",
                table: "HoldingMaterialTransferBatches");

            migrationBuilder.DropColumn(
                name: "TransferQuantity",
                table: "HoldingMaterialTransferBatches");
        }
    }
}
