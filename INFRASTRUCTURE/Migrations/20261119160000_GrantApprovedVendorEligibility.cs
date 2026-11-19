using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Procurement screens now hide or disable suppliers and manufacturers the approved vendor list does not
    /// allow, reading <c>POST approved-vendor-list/eligibility</c> under the new <c>CanViewApprovedVendorEligibility</c>.
    /// Every role that can choose a supplier or manufacturer on one of those screens (sourcing, quotation prices,
    /// awarding quotations, creating or revising purchase orders, PO and shipment invoices) gets it with the same
    /// access types, so nobody loses the screen's choices on deploy. The approved vendor list's own keys
    /// (view, create, edit, revise, review, return, approve, reject, print) guard a new screen and are granted to
    /// nobody until an administrator assigns them. Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261119160000_GrantApprovedVendorEligibility")]
    public partial class GrantApprovedVendorEligibility : Migration
    {
        private const string Sources = """
            'CanSourcePurchaseRequisition', 'CanEditSourceRequisition',
            'CanSendForeignQuotationResponse', 'CanSendLocalQuotationResponse',
            'CanApplyChangesForForeignVendorPricingSelection', 'CanApplyChangesForLocalVendorPricingSelection',
            'CanCreateForeignPurchaseOrder', 'CanCreateLocalPurchaseOrder',
            'CanReviseForeignPurchaseOrder', 'CanReviseLocalPurchaseOrder',
            'CanCreatePurchaseOrderInvoice', 'CanCreateShipmentInvoice', 'CanEditShipmentInvoice'
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
                SELECT DISTINCT source."RoleId", 'permission', 'CanViewApprovedVendorEligibility'
                FROM "roleclaims" source
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" IN ({Sources})
                  AND NOT EXISTS (
                      SELECT 1 FROM "roleclaims" existing
                      WHERE existing."RoleId" = source."RoleId" AND existing."ClaimType" = 'permission'
                        AND existing."ClaimValue" = 'CanViewApprovedVendorEligibility');

                INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
                SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), 'CanViewApprovedVendorEligibility', target."Id", granted."Type"
                FROM "roleclaims" source
                JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
                JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
                     AND target."ClaimValue" = 'CanViewApprovedVendorEligibility'
                WHERE source."ClaimType" = 'permission' AND source."ClaimValue" IN ({Sources})
                  AND NOT EXISTS (
                      SELECT 1 FROM "PermissionTypes" existing
                      WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes every CanViewApprovedVendorEligibility grant, including any an administrator made after Up.
            migrationBuilder.Sql("""
                DELETE FROM "PermissionTypes" WHERE "Key" = 'CanViewApprovedVendorEligibility';
                DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = 'CanViewApprovedVendorEligibility';
                """);
        }
    }
}
