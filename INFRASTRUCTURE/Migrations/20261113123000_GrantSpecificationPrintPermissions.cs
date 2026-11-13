using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261113123000_GrantSpecificationPrintPermissions")]
public sealed class GrantSpecificationPrintPermissions : Migration
{
    private const string Pairs = """
        ('CanViewRawMaterialSpecifications', 'CanExportRawMaterialSpecification'),
        ('CanViewPackagingMaterialSpecifications', 'CanExportPackagingMaterialSpecification'),
        ('CanViewProductSpecifications', 'CanExportProductSpecification')
        """;

    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql($$"""
        WITH keys(view_key, print_key) AS (VALUES {{Pairs}})
        INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
        SELECT DISTINCT source."RoleId", 'permission', keys.print_key
        FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.view_key
        WHERE NOT EXISTS (
            SELECT 1 FROM "roleclaims" existing WHERE existing."RoleId" = source."RoleId"
                AND existing."ClaimType" = 'permission' AND existing."ClaimValue" = keys.print_key);

        WITH keys(view_key, print_key) AS (VALUES {{Pairs}})
        INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
        SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), keys.print_key, target."Id", granted."Type"
        FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.view_key
        JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id" AND granted."Key" = keys.view_key
        JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
            AND target."ClaimValue" = keys.print_key
        WHERE NOT EXISTS (
            SELECT 1 FROM "PermissionTypes" existing WHERE existing."RoleClaimId" = target."Id"
                AND existing."Key" = keys.print_key AND existing."Type" = granted."Type");
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql($$"""
        WITH keys(view_key, print_key) AS (VALUES {{Pairs}})
        DELETE FROM "PermissionTypes" WHERE "Key" IN (SELECT print_key FROM keys);
        WITH keys(view_key, print_key) AS (VALUES {{Pairs}})
        DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" IN (SELECT print_key FROM keys);
        """);
}
