using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

/// <summary>Preserve existing product specification view/edit access types for the new COA reported-tests tasks, as the microbial limit grant did.</summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261127141800_GrantProductCoaReportedTestKeys")]
public sealed class GrantProductCoaReportedTestKeys : Migration
{
    private static readonly string[] Keys = ["CanViewProductCoaReportedTests", "CanEditProductCoaReportedTests"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        Grant(migrationBuilder, "CanViewProductCoaReportedTests", ["CanViewProductSpecifications", "CanEditProductSpecification", "CanVerifyProductSpecification", "CanCreateProductSpecification"]);
        Grant(migrationBuilder, "CanEditProductCoaReportedTests", ["CanEditProductSpecification"]);
    }

    private static void Grant(MigrationBuilder migrationBuilder, string target, string[] sources)
    {
        var keys = string.Join(", ", sources.Select(key => "'" + key + "'"));
        migrationBuilder.Sql($$"""
            INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
            SELECT DISTINCT source."RoleId", 'permission', '{{target}}'
            FROM "roleclaims" source
            WHERE source."ClaimType" = 'permission' AND source."ClaimValue" IN ({{keys}})
              AND NOT EXISTS (SELECT 1 FROM "roleclaims" existing WHERE existing."RoleId" = source."RoleId"
                AND existing."ClaimType" = 'permission' AND existing."ClaimValue" = '{{target}}');

            INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
            SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), '{{target}}', target."Id", granted."Type"
            FROM "roleclaims" source
            JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
            JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
                AND target."ClaimValue" = '{{target}}'
            WHERE source."ClaimType" = 'permission' AND source."ClaimValue" IN ({{keys}})
              AND NOT EXISTS (SELECT 1 FROM "PermissionTypes" existing WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Includes assignments administrators made after this migration, as with the existing permission-grant migrations.
        var keys = string.Join(", ", Keys.Select(key => "'" + key + "'"));
        migrationBuilder.Sql($"""
            DELETE FROM "PermissionTypes" WHERE "Key" IN ({keys});
            DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" IN ({keys});
            """);
    }
}
