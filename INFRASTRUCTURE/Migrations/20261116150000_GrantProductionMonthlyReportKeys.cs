using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

/// <summary>
/// The stock issued, production monthly, schedule summary and batch value reports get their own keys. Roles that run
/// warehouse reports receive the stock issued key, and roles that run QC and QA reports receive the three production
/// keys, with the same access types, so the reports open for the people who used them before.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261116150000_GrantProductionMonthlyReportKeys")]
public sealed class GrantProductionMonthlyReportKeys : Migration
{
    private const string Pairs = """
        ('CanViewWarehouseReport', 'CanViewStockIssuedReport'),
        ('CanViewQcAndQaReport', 'CanViewProductionMonthlyReport'),
        ('CanViewQcAndQaReport', 'CanViewProductionScheduleSummaryReport'),
        ('CanViewQcAndQaReport', 'CanViewProductionBatchValueReport')
        """;

    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql($$"""
        WITH keys(group_key, report_key) AS (VALUES {{Pairs}})
        INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
        SELECT DISTINCT source."RoleId", 'permission', keys.report_key
        FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.group_key
        WHERE NOT EXISTS (
            SELECT 1 FROM "roleclaims" existing WHERE existing."RoleId" = source."RoleId"
                AND existing."ClaimType" = 'permission' AND existing."ClaimValue" = keys.report_key);

        WITH keys(group_key, report_key) AS (VALUES {{Pairs}})
        INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
        SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), keys.report_key, target."Id", granted."Type"
        FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.group_key
        JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id" AND granted."Key" = keys.group_key
        JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
            AND target."ClaimValue" = keys.report_key
        WHERE NOT EXISTS (
            SELECT 1 FROM "PermissionTypes" existing WHERE existing."RoleClaimId" = target."Id"
                AND existing."Key" = keys.report_key AND existing."Type" = granted."Type");
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql($$"""
        WITH keys(group_key, report_key) AS (VALUES {{Pairs}})
        DELETE FROM "PermissionTypes" WHERE "Key" IN (SELECT report_key FROM keys);
        WITH keys(group_key, report_key) AS (VALUES {{Pairs}})
        DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" IN (SELECT report_key FROM keys);
        """);
}
