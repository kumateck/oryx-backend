using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Costing moved to its own Finance permission, <c>ManageCosting</c>. Every role that could manage
    /// customer pricing through <c>ManageCustomerContracts</c> gets it with the same access types, so no
    /// one loses access on deploy. Data only; the model is unchanged.
    /// </summary>
    public partial class GrantManageCostingToContractManagers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT source."RoleId", 'permission', 'ManageCosting'
                FROM "roleclaims" source
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'ManageCustomerContracts'
                  AND NOT EXISTS (
                      SELECT 1 FROM "roleclaims" existing
                      WHERE existing."RoleId" = source."RoleId" AND existing."ClaimType" = 'permission'
                        AND existing."ClaimValue" = 'ManageCosting');

                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), 'ManageCosting', target."Id", granted."Type"
                FROM "roleclaims" source
                JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
                JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
                     AND target."ClaimValue" = 'ManageCosting'
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'ManageCustomerContracts'
                  AND NOT EXISTS (
                      SELECT 1 FROM "PermissionTypes" existing
                      WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes every ManageCosting grant, including any an administrator made after Up.
            migrationBuilder.Sql("""
                DELETE FROM "PermissionTypes" WHERE "Key" = 'ManageCosting';
                DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = 'ManageCosting';
                """);
        }
    }
}
