using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Changing a user's role now needs its own permission, <c>CanAssignUserRole</c>; before, any signed-in user
    /// could do it. The desktop's change-role action was guarded by <c>CanEditUser</c>, so every role holding
    /// that key gets the new one with the same access types and nobody who could do the task loses it.
    /// <c>CanDeleteUser</c> and <c>CanApproveMaterialBatch</c> are new and granted to nobody until assigned.
    /// Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261105170000_GrantAssignUserRoleToUserEditors")]
    public partial class GrantAssignUserRoleToUserEditors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT source."RoleId", 'permission', 'CanAssignUserRole'
                FROM "roleclaims" source
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanEditUser'
                  AND NOT EXISTS (
                      SELECT 1 FROM "roleclaims" existing
                      WHERE existing."RoleId" = source."RoleId" AND existing."ClaimType" = 'permission'
                        AND existing."ClaimValue" = 'CanAssignUserRole');

                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), 'CanAssignUserRole', target."Id", granted."Type"
                FROM "roleclaims" source
                JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
                JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
                     AND target."ClaimValue" = 'CanAssignUserRole'
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = 'CanEditUser'
                  AND NOT EXISTS (
                      SELECT 1 FROM "PermissionTypes" existing
                      WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes every CanAssignUserRole grant, including any an administrator made after Up.
            migrationBuilder.Sql("""
                DELETE FROM "PermissionTypes" WHERE "Key" = 'CanAssignUserRole';
                DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = 'CanAssignUserRole';
                """);
        }
    }
}
