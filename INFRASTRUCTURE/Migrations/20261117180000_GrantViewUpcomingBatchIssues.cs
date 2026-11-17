using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// The new Upcoming batch issues page (batches started but whose BMR is not issued, with the materials reserved for them)
    /// gets its own key, <c>CanViewUpcomingBatchIssues</c>. It serves the warehouse staff who issue production requisitions,
    /// so every role holding <c>CanIssueRawMaterialRequisitions</c> or <c>CanIssuePackagingMaterialRequisitions</c> gets it
    /// with the same access types. Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261117180000_GrantViewUpcomingBatchIssues")]
    public partial class GrantViewUpcomingBatchIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT source."RoleId", 'permission', 'CanViewUpcomingBatchIssues'
                FROM "roleclaims" source
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" IN ('CanIssueRawMaterialRequisitions', 'CanIssuePackagingMaterialRequisitions')
                  AND NOT EXISTS (
                      SELECT 1 FROM "roleclaims" existing
                      WHERE existing."RoleId" = source."RoleId" AND existing."ClaimType" = 'permission'
                        AND existing."ClaimValue" = 'CanViewUpcomingBatchIssues');

                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), 'CanViewUpcomingBatchIssues', target."Id", granted."Type"
                FROM "roleclaims" source
                JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
                JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
                     AND target."ClaimValue" = 'CanViewUpcomingBatchIssues'
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" IN ('CanIssueRawMaterialRequisitions', 'CanIssuePackagingMaterialRequisitions')
                  AND NOT EXISTS (
                      SELECT 1 FROM "PermissionTypes" existing
                      WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes every CanViewUpcomingBatchIssues grant, including any an administrator made after Up.
            migrationBuilder.Sql("""
                DELETE FROM "PermissionTypes" WHERE "Key" = 'CanViewUpcomingBatchIssues';
                DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = 'CanViewUpcomingBatchIssues';
                """);
        }
    }
}
