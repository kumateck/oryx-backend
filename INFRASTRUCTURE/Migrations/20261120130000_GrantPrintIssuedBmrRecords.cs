using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Printing the issued BMR/BPR register and the issued-by-product report gets its own key, <c>CanPrintIssuedBmrRecords</c>.
    /// Both print what the Issued BMR records page already shows, so every role holding <c>CanViewIssuedBmrRecords</c> gets the
    /// new key with the same access types. Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261120130000_GrantPrintIssuedBmrRecords")]
    public partial class GrantPrintIssuedBmrRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT source."RoleId", 'permission', 'CanPrintIssuedBmrRecords'
                FROM "roleclaims" source
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanViewIssuedBmrRecords'
                  AND NOT EXISTS (
                      SELECT 1 FROM "roleclaims" existing
                      WHERE existing."RoleId" = source."RoleId" AND existing."ClaimType" = 'permission'
                        AND existing."ClaimValue" = 'CanPrintIssuedBmrRecords');

                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), 'CanPrintIssuedBmrRecords', target."Id", granted."Type"
                FROM "roleclaims" source
                JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
                JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
                     AND target."ClaimValue" = 'CanPrintIssuedBmrRecords'
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanViewIssuedBmrRecords'
                  AND NOT EXISTS (
                      SELECT 1 FROM "PermissionTypes" existing
                      WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes every CanPrintIssuedBmrRecords grant, including any an administrator made after Up.
            migrationBuilder.Sql("""
                DELETE FROM "PermissionTypes" WHERE "Key" = 'CanPrintIssuedBmrRecords';
                DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = 'CanPrintIssuedBmrRecords';
                """);
        }
    }
}
