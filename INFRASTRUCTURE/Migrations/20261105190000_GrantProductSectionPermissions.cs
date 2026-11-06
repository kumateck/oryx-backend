using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Products get one permission per task and section, per context (permissions audit phase 2). Each new key is granted,
    /// with the same access types, to every role holding the key that opened that task before, so nobody loses access on
    /// deploy:
    /// - Production and QA views from <c>CanViewPlannedProducts</c> (both pages used it), QA views also from <c>CanVerifyProduct</c>;
    /// - creating and importing products from <c>CanCreateNewProductionPlan</c>;
    /// - every add, edit, delete, archive and import of a section, in Production and QA, from <c>CanEditProductionPlan</c>;
    /// - the FG product list from <c>CanViewApprovedProducts</c>.
    /// <c>CanDeleteProductionProduct</c> is granted to nobody: deleting a product had no key and is rare and sensitive.
    /// Administrators can now remove what each role should not do. Data only; the model is unchanged.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261105190000_GrantProductSectionPermissions")]
    public partial class GrantProductSectionPermissions : Migration
    {
        public static readonly (string Key, string From)[] Grants =
        [
            ("CanViewProductionProducts", "CanViewPlannedProducts"),
            ("CanExportProductionProducts", "CanViewPlannedProducts"),
            ("CanViewProductionProductInfo", "CanViewPlannedProducts"),
            ("CanViewProductionProductBom", "CanViewPlannedProducts"),
            ("CanViewProductionProductPackingMaterials", "CanViewPlannedProducts"),
            ("CanViewProductionProductPackingStyles", "CanViewPlannedProducts"),
            ("CanViewProductionProductPackingOrder", "CanViewPlannedProducts"),
            ("CanViewProductionProductProcedure", "CanViewPlannedProducts"),
            ("CanViewProductionProductSpecifications", "CanViewPlannedProducts"),
            ("CanViewProductionProductStps", "CanViewPlannedProducts"),
            ("CanViewProductionProductArds", "CanViewPlannedProducts"),
            ("CanPrintProductionProduct", "CanViewPlannedProducts"),
            ("CanCreateProductionProduct", "CanCreateNewProductionPlan"),
            ("CanImportProductionProducts", "CanCreateNewProductionPlan"),
            ("CanEditProductionProductInfo", "CanEditProductionPlan"),
            ("CanAddProductionProductBom", "CanEditProductionPlan"),
            ("CanEditProductionProductBom", "CanEditProductionPlan"),
            ("CanDeleteProductionProductBom", "CanEditProductionPlan"),
            ("CanArchiveProductionProductBom", "CanEditProductionPlan"),
            ("CanImportProductionProductBom", "CanEditProductionPlan"),
            ("CanAddProductionProductPackingMaterials", "CanEditProductionPlan"),
            ("CanEditProductionProductPackingMaterials", "CanEditProductionPlan"),
            ("CanDeleteProductionProductPackingMaterials", "CanEditProductionPlan"),
            ("CanImportProductionProductPackingMaterials", "CanEditProductionPlan"),
            ("CanAddProductionProductPackingStyles", "CanEditProductionPlan"),
            ("CanEditProductionProductPackingStyles", "CanEditProductionPlan"),
            ("CanDeleteProductionProductPackingStyles", "CanEditProductionPlan"),
            ("CanEditProductionProductPackingOrder", "CanEditProductionPlan"),
            ("CanAddProductionProductProcedure", "CanEditProductionPlan"),
            ("CanEditProductionProductProcedure", "CanEditProductionPlan"),
            ("CanDeleteProductionProductProcedure", "CanEditProductionPlan"),
            ("CanViewQaProducts", "CanViewPlannedProducts"),
            ("CanViewQaProductInfo", "CanViewPlannedProducts"),
            ("CanViewQaProductBom", "CanViewPlannedProducts"),
            ("CanViewQaProductPackingMaterials", "CanViewPlannedProducts"),
            ("CanViewQaProductPackingStyles", "CanViewPlannedProducts"),
            ("CanViewQaProductPackingOrder", "CanViewPlannedProducts"),
            ("CanViewQaProductProcedure", "CanViewPlannedProducts"),
            ("CanViewQaProductSpecifications", "CanViewPlannedProducts"),
            ("CanViewQaProductArds", "CanViewPlannedProducts"),
            ("CanViewQaProductStps", "CanViewPlannedProducts"),
            ("CanViewQaProducts", "CanVerifyProduct"),
            ("CanViewQaProductInfo", "CanVerifyProduct"),
            ("CanViewQaProductBom", "CanVerifyProduct"),
            ("CanViewQaProductPackingMaterials", "CanVerifyProduct"),
            ("CanViewQaProductPackingStyles", "CanVerifyProduct"),
            ("CanViewQaProductPackingOrder", "CanVerifyProduct"),
            ("CanViewQaProductProcedure", "CanVerifyProduct"),
            ("CanViewQaProductSpecifications", "CanVerifyProduct"),
            ("CanViewQaProductArds", "CanVerifyProduct"),
            ("CanViewQaProductStps", "CanVerifyProduct"),
            ("CanEditQaProductInfo", "CanEditProductionPlan"),
            ("CanAddQaProductBom", "CanEditProductionPlan"),
            ("CanEditQaProductBom", "CanEditProductionPlan"),
            ("CanDeleteQaProductBom", "CanEditProductionPlan"),
            ("CanArchiveQaProductBom", "CanEditProductionPlan"),
            ("CanAddQaProductPackingMaterials", "CanEditProductionPlan"),
            ("CanEditQaProductPackingMaterials", "CanEditProductionPlan"),
            ("CanDeleteQaProductPackingMaterials", "CanEditProductionPlan"),
            ("CanAddQaProductPackingStyles", "CanEditProductionPlan"),
            ("CanEditQaProductPackingStyles", "CanEditProductionPlan"),
            ("CanDeleteQaProductPackingStyles", "CanEditProductionPlan"),
            ("CanEditQaProductPackingOrder", "CanEditProductionPlan"),
            ("CanAddQaProductProcedure", "CanEditProductionPlan"),
            ("CanEditQaProductProcedure", "CanEditProductionPlan"),
            ("CanDeleteQaProductProcedure", "CanEditProductionPlan"),
            ("CanViewFgProducts", "CanViewApprovedProducts"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (key, from) in Grants)
                migrationBuilder.Sql(Grant(key, from));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Removes every grant of the new keys, including any an administrator made after Up.
            foreach (var key in Grants.Select(grant => grant.Key).Distinct())
                migrationBuilder.Sql($$"""
                    DELETE FROM "PermissionTypes" WHERE "Key" = '{{key}}';
                    DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = '{{key}}';
                    """);
        }

        /// <summary>The idempotent grant used by every permission split: same roles, same access types.</summary>
        internal static string Grant(string key, string from) => $$"""
            INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
            SELECT DISTINCT source."RoleId", 'permission', '{{key}}'
            FROM "roleclaims" source
            WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = '{{from}}'
              AND NOT EXISTS (
                  SELECT 1 FROM "roleclaims" existing
                  WHERE existing."RoleId" = source."RoleId" AND existing."ClaimType" = 'permission'
                    AND existing."ClaimValue" = '{{key}}');

            INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
            SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), '{{key}}', target."Id", granted."Type"
            FROM "roleclaims" source
            JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id"
            JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
                 AND target."ClaimValue" = '{{key}}'
            WHERE source."ClaimType" = 'permission' AND source."ClaimValue" = '{{from}}'
              AND NOT EXISTS (
                  SELECT 1 FROM "PermissionTypes" existing
                  WHERE existing."RoleClaimId" = target."Id" AND existing."Type" = granted."Type");
            """;
    }
}
