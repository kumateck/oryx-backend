using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Production schedule writes now check their keys on the server. Batch-board actions (step status, returns, final
    /// packing, FGTN, extra packing) and starting production were gated on the desktop by a view key; each now has its own
    /// key, granted with the same access types to every role holding that view key, so the people who could act still
    /// can. Batch packaging records get their own key from the BMR issue key, and rejecting an incoming transfer is granted
    /// to everyone who could approve one (the desktop used the approve key for both). Deleting a schedule and completing a
    /// return note get keys granted to nobody: no page offers them. Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261106050000_GrantProductionScheduleTasks")]
    public partial class GrantProductionScheduleTasks : Migration
    {
        public static readonly (string Key, string From)[] Grants =
        [
            ("CanStartProduction", "CanViewProductSchedules"),
            ("CanUpdateProductionStep", "CanViewPlannedProducts"),
            ("CanReturnProductionMaterials", "CanViewPlannedProducts"),
            ("CanRecordFinalPacking", "CanViewPlannedProducts"),
            ("CanCreateFgtn", "CanViewPlannedProducts"),
            ("CanRequestExtraPacking", "CanViewPlannedProducts"),
            ("CanIssueBpr", "CanIssueBmr"),
            ("CanRejectIncomingStockTransferRequest", "CanApproveIncomingStockTransferRequest"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (key, from) in Grants)
                migrationBuilder.Sql(GrantProductSectionPermissions.Grant(key, from));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // CanRejectIncomingStockTransferRequest existed before; only the new keys are removed.
            foreach (var (key, _) in Grants.Where(grant => grant.Key != "CanRejectIncomingStockTransferRequest"))
                migrationBuilder.Sql($$"""
                    DELETE FROM "PermissionTypes" WHERE "Key" = '{{key}}';
                    DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = '{{key}}';
                    """);
        }
    }
}
