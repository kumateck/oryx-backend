using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

/// <summary>
/// Linking an operation to its production department, and unlinking it, used to be part of editing the operation's
/// details. Every role that can edit operations today receives both new keys with the same access types.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261118230000_GrantOperationDepartmentLinkKeys")]
public sealed class GrantOperationDepartmentLinkKeys : Migration
{
    private const string Pairs = """
        ('CanEditOperation', 'CanLinkOperationDepartment'),
        ('CanEditOperation', 'CanUnlinkOperationDepartment')
        """;

    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql($$"""
        WITH keys(source_key, new_key) AS (VALUES {{Pairs}})
        INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
        SELECT DISTINCT source."RoleId", 'permission', keys.new_key
        FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.source_key
        WHERE NOT EXISTS (
            SELECT 1 FROM "roleclaims" existing WHERE existing."RoleId" = source."RoleId"
                AND existing."ClaimType" = 'permission' AND existing."ClaimValue" = keys.new_key);

        WITH keys(source_key, new_key) AS (VALUES {{Pairs}})
        INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
        SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), keys.new_key, target."Id", granted."Type"
        FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.source_key
        JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id" AND granted."Key" = keys.source_key
        JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
            AND target."ClaimValue" = keys.new_key
        WHERE NOT EXISTS (
            SELECT 1 FROM "PermissionTypes" existing WHERE existing."RoleClaimId" = target."Id"
                AND existing."Key" = keys.new_key AND existing."Type" = granted."Type");
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql($$"""
        WITH keys(source_key, new_key) AS (VALUES {{Pairs}})
        DELETE FROM "PermissionTypes" WHERE "Key" IN (SELECT new_key FROM keys);
        WITH keys(source_key, new_key) AS (VALUES {{Pairs}})
        DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" IN (SELECT new_key FROM keys);
        """);
}
