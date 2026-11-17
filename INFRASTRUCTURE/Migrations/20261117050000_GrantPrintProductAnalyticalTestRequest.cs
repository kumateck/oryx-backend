using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Printing a product's Analytical Test Request form gets its own key, <c>CanPrintProductAnalyticalTestRequest</c>.
    /// The form is printed from the ATR results page, guarded by <c>CanCheckProductTest</c>, and from the certificate's
    /// approval page, guarded by <c>CanViewPendingApprovals</c>; every role holding either gets the new key with the same
    /// access types. Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261117050000_GrantPrintProductAnalyticalTestRequest")]
    public partial class GrantPrintProductAnalyticalTestRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT source."RoleId", 'permission', 'CanPrintProductAnalyticalTestRequest'
                FROM "roleclaims" source
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" IN ('CanCheckProductTest', 'CanViewPendingApprovals')
                  AND NOT EXISTS (
                      SELECT 1 FROM "roleclaims" existing
                      WHERE existing."RoleId" = source."RoleId" AND existing."ClaimType" = 'permission'
                        AND existing."ClaimValue" = 'CanPrintProductAnalyticalTestRequest');

                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), 'CanPrintProductAnalyticalTestRequest', target."Id", granted."Type"
                FROM "roleclaims" source
                JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
                JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
                     AND target."ClaimValue" = 'CanPrintProductAnalyticalTestRequest'
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" IN ('CanCheckProductTest', 'CanViewPendingApprovals')
                  AND NOT EXISTS (
                      SELECT 1 FROM "PermissionTypes" existing
                      WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes every CanPrintProductAnalyticalTestRequest grant, including any an administrator made after Up.
            migrationBuilder.Sql("""
                DELETE FROM "PermissionTypes" WHERE "Key" = 'CanPrintProductAnalyticalTestRequest';
                DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = 'CanPrintProductAnalyticalTestRequest';
                """);
        }
    }
}
