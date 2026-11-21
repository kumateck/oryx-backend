using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261121124141_GrantCostingPriceListPreparation")]
public class GrantCostingPriceListPreparation : Migration
{
    // Preserve preparation/printing tasks for existing costing roles. New oversight tasks receive no grants.
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
        SELECT DISTINCT source."RoleId", 'permission', keys.key
        FROM "roleclaims" source CROSS JOIN (VALUES
            ('CanViewCostingPriceList'), ('CanCreateCostingPriceList'), ('CanEditCostingPriceList'),
            ('CanReviseCostingPriceList'), ('CanSubmitCostingPriceList'), ('CanPrintCostingPriceList')) keys(key)
        WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'ManageCosting'
        AND NOT EXISTS (SELECT 1 FROM "roleclaims" existing WHERE existing."RoleId" = source."RoleId"
            AND existing."ClaimType" = 'permission' AND existing."ClaimValue" = keys.key);

        INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
        SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), target."ClaimValue", target."Id", granted."Type"
        FROM "roleclaims" source JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
        JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
            AND target."ClaimValue" IN ('CanViewCostingPriceList','CanCreateCostingPriceList','CanEditCostingPriceList',
                'CanReviseCostingPriceList','CanSubmitCostingPriceList','CanPrintCostingPriceList')
        WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'ManageCosting'
        AND NOT EXISTS (SELECT 1 FROM "PermissionTypes" existing WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
        """);
    protected override void Down(MigrationBuilder migrationBuilder) { /* Retain administrator grants on rollback. */ }
}
