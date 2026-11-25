using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Approved vendor lists now start as drafts. Saving the list and sending it for review was one step, guarded by
    /// <c>CanCreateApprovedVendorList</c> (first list) or <c>CanReviseApprovedVendorList</c> (new revision). It is now
    /// two tasks: <c>CanEditApprovedVendorListDraft</c> (save a draft), <c>CanSubmitApprovedVendorList</c> (submit it
    /// for review). Every role holding either
    /// start key gets both with the same access types, so the people who prepare the list can carry it through.
    /// Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261125060000_GrantApprovedVendorListDraftKeys")]
    public partial class GrantApprovedVendorListDraftKeys : Migration
    {
        private static readonly string[] Keys = ["CanEditApprovedVendorListDraft", "CanSubmitApprovedVendorList"];
        private const string Sources = "'CanCreateApprovedVendorList', 'CanReviseApprovedVendorList'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var key in Keys)
                migrationBuilder.Sql($"""
                    INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                    SELECT DISTINCT source."RoleId", 'permission', '{key}'
                    FROM "roleclaims" source
                    WHERE source."ClaimType" = 'permission' AND source."ClaimValue" IN ({Sources})
                      AND NOT EXISTS (
                          SELECT 1 FROM "roleclaims" existing
                          WHERE existing."RoleId" = source."RoleId" AND existing."ClaimType" = 'permission'
                            AND existing."ClaimValue" = '{key}');

                    INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                    SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), '{key}', target."Id", granted."Type"
                    FROM "roleclaims" source
                    JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
                    JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
                         AND target."ClaimValue" = '{key}'
                    WHERE source."ClaimType" = 'permission' AND source."ClaimValue" IN ({Sources})
                      AND NOT EXISTS (
                          SELECT 1 FROM "PermissionTypes" existing
                          WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
                    """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes every grant of the two new keys, including any an administrator made after Up.
            foreach (var key in Keys)
                migrationBuilder.Sql($"""
                    DELETE FROM "PermissionTypes" WHERE "Key" = '{key}';
                    DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = '{key}';
                    """);
        }
    }
}
