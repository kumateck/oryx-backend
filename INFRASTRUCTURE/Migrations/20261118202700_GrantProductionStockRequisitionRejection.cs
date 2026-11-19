using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace INFRASTRUCTURE.Migrations;
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261118202700_GrantProductionStockRequisitionRejection")]
public sealed class GrantProductionStockRequisitionRejection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT source."RoleId", 'permission', 'CanRejectRawMaterialStockRequisition' FROM "roleclaims" source
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanRejectPendingApproval'
                AND NOT EXISTS (SELECT 1 FROM "roleclaims" target WHERE target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission' AND target."ClaimValue" = 'CanRejectRawMaterialStockRequisition');
                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), 'CanRejectRawMaterialStockRequisition', target."Id", granted."Type"
                FROM "roleclaims" source JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id" AND granted."Key" = source."ClaimValue"
                JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission' AND target."ClaimValue" = 'CanRejectRawMaterialStockRequisition'
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanRejectPendingApproval'
                AND NOT EXISTS (SELECT 1 FROM "PermissionTypes" existing WHERE existing."RoleClaimId" = target."Id" AND existing."Key" = 'CanRejectRawMaterialStockRequisition' AND existing."Type" = granted."Type");
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT source."RoleId", 'permission', 'CanRejectPackagingMaterialStockRequisition' FROM "roleclaims" source
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanRejectPendingApproval'
                AND NOT EXISTS (SELECT 1 FROM "roleclaims" target WHERE target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission' AND target."ClaimValue" = 'CanRejectPackagingMaterialStockRequisition');
                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), 'CanRejectPackagingMaterialStockRequisition', target."Id", granted."Type"
                FROM "roleclaims" source JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id" AND granted."Key" = source."ClaimValue"
                JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission' AND target."ClaimValue" = 'CanRejectPackagingMaterialStockRequisition'
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanRejectPendingApproval'
                AND NOT EXISTS (SELECT 1 FROM "PermissionTypes" existing WHERE existing."RoleClaimId" = target."Id" AND existing."Key" = 'CanRejectPackagingMaterialStockRequisition' AND existing."Type" = granted."Type");
                """);
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Grants may have been independently changed by administrators; do not remove them on downgrade.
    }
}
