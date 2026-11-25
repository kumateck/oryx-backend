using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261125062500_GrantPendingCertificateChangeKeys")]
public sealed class GrantPendingCertificateChangeKeys : Migration
{
    private static readonly (string Key, string Source)[] Grants =
    [
        ("CanEditProductCertificateOfAnalysis", "CanCheckProductTest"),
        ("CanRequestProductCertificateReview", "CanApproveResponse"),
        ("CanRequestProductCertificateCorrection", "CanApproveResponse"),
        ("CanEditRawMaterialCertificateOfAnalysis", "CanCheckRawMaterialTestResult"),
        ("CanRequestRawMaterialCertificateReview", "CanApproveResponse"),
        ("CanRequestRawMaterialCertificateCorrection", "CanApproveResponse"),
        ("CanEditPackagingMaterialCertificateOfAnalysis", "CanCheckPackagingMaterialTestResult"),
        ("CanRequestPackagingMaterialCertificateReview", "CanApproveResponse"),
        ("CanRequestPackagingMaterialCertificateCorrection", "CanApproveResponse"),

    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var (key, source) in Grants)
            migrationBuilder.Sql($"""
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT claim."RoleId", 'permission', '{key}'
                FROM "roleclaims" claim
                WHERE claim."ClaimType" = 'permission' AND claim."ClaimValue" = '{source}'
                  AND NOT EXISTS (
                      SELECT 1 FROM "roleclaims" existing
                      WHERE existing."RoleId" = claim."RoleId" AND existing."ClaimType" = 'permission'
                        AND existing."ClaimValue" = '{key}');

                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), '{key}', target."Id", granted."Type"
                FROM "roleclaims" claim
                JOIN "PermissionTypes" granted ON granted."RoleClaimId" = claim."Id"
                JOIN "roleclaims" target ON target."RoleId" = claim."RoleId" AND target."ClaimType" = 'permission'
                     AND target."ClaimValue" = '{key}'
                WHERE claim."ClaimType" = 'permission' AND claim."ClaimValue" = '{source}'
                  AND NOT EXISTS (
                      SELECT 1 FROM "PermissionTypes" existing
                      WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
                """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var (key, _) in Grants)
            migrationBuilder.Sql($"""
                DELETE FROM "PermissionTypes" WHERE "Key" = '{key}';
                DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = '{key}';
                """);
    }
}
