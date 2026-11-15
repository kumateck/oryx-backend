using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace INFRASTRUCTURE.Migrations;

/// <summary>
/// Each report in the reports hub now has its own key. Every role that could run a report through its group key
/// (warehouse, procurement, finished goods, stores, people, QC and QA) receives that report's key with the same access
/// types, so nobody loses a report on deploy. The group keys stay for the register reports that still use them.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20261115120000_GrantPerReportViewKeys")]
public sealed class GrantPerReportViewKeys : Migration
{
    private const string Pairs = """
        ('CanViewProcurementReport', 'CanViewApprovedManufacturerListReport'),
        ('CanViewProcurementReport', 'CanViewSupplierMappingSummaryReport'),
        ('CanViewProcurementReport', 'CanViewSupplierMappingDetailReport'),
        ('CanViewProcurementReport', 'CanViewPurchaseOrdersReport'),
        ('CanViewProcurementReport', 'CanViewInvoicedPurchaseOrdersReport'),
        ('CanViewProcurementReport', 'CanViewShipmentsReport'),
        ('CanViewQcAndQaReport', 'CanViewMaterialListReport'),
        ('CanViewWarehouseReport', 'CanViewMaterialsPerDepartmentReport'),
        ('CanViewWarehouseReport', 'CanViewMaterialStockSummaryReport'),
        ('CanViewWarehouseReport', 'CanViewMaterialStockDetailReport'),
        ('CanViewWarehouseReport', 'CanViewStockCountSheetReport'),
        ('CanViewWarehouseReport', 'CanViewExpiredStockReport'),
        ('CanViewWarehouseReport', 'CanViewInventoryValuationReport'),
        ('CanViewQcAndQaReport', 'CanViewMonthlyProductionScheduleReport'),
        ('CanViewQcAndQaReport', 'CanViewDailyProductionScheduleReport'),
        ('CanViewQcAndQaReport', 'CanViewProductReadinessReport'),
        ('CanViewQcAndQaReport', 'CanViewBmrIssuedReport'),
        ('CanViewQcAndQaReport', 'CanViewBmrIssuedByProductReport'),
        ('CanViewFgtnReport', 'CanViewProductListReport'),
        ('CanViewQcAndQaReport', 'CanViewDepartmentProductListReport'),
        ('CanViewFgtnReport', 'CanViewProductStockSummaryReport'),
        ('CanViewFgtnReport', 'CanViewProductStockDetailReport'),
        ('CanViewFgtnReport', 'CanViewFinishedGoodsTransferSummaryReport'),
        ('CanViewFgtnReport', 'CanViewFinishedGoodsTransferDetailReport'),
        ('CanViewFgtnReport', 'CanViewInvoicedProductsReport'),
        ('CanViewStoreAndInventoryReport', 'CanViewStoreItemsReport'),
        ('CanViewStoreAndInventoryReport', 'CanViewStoreStockReport'),
        ('CanViewStoreAndInventoryReport', 'CanViewVendorItemMappingSummaryReport'),
        ('CanViewStoreAndInventoryReport', 'CanViewVendorItemMappingDetailReport'),
        ('CanViewHumanResourcesReport', 'CanViewEmployeeMasterListReport'),
        ('CanViewHumanResourcesReport', 'CanViewStaffGradeReport'),
        ('CanViewHumanResourcesReport', 'CanViewStaffTotalReport'),
        ('CanViewHumanResourcesReport', 'CanViewStaffGenderRatioReport'),
        ('CanViewHumanResourcesReport', 'CanViewNewHiresAndExitsReport'),
        ('CanViewHumanResourcesReport', 'CanViewStaffTurnoverReport'),
        ('CanViewHumanResourcesReport', 'CanViewLeaveOverviewReport'),
        ('CanViewHumanResourcesReport', 'CanViewDailyAttendanceReport'),
        ('CanViewHumanResourcesReport', 'CanViewDepartmentAttendanceReport')
        """;

    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql($$"""
        WITH keys(group_key, report_key) AS (VALUES {{Pairs}})
        INSERT INTO "roleclaims" ("RoleId", "ClaimType", "ClaimValue")
        SELECT DISTINCT source."RoleId", 'permission', keys.report_key
        FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.group_key
        WHERE NOT EXISTS (
            SELECT 1 FROM "roleclaims" existing WHERE existing."RoleId" = source."RoleId"
                AND existing."ClaimType" = 'permission' AND existing."ClaimValue" = keys.report_key);

        WITH keys(group_key, report_key) AS (VALUES {{Pairs}})
        INSERT INTO "PermissionTypes" ("Id", "Key", "RoleClaimId", "Type")
        SELECT DISTINCT ON (target."Id", granted."Type") gen_random_uuid(), keys.report_key, target."Id", granted."Type"
        FROM keys JOIN "roleclaims" source ON source."ClaimType" = 'permission' AND source."ClaimValue" = keys.group_key
        JOIN "PermissionTypes" granted ON granted."RoleClaimId" = source."Id" AND granted."Key" = keys.group_key
        JOIN "roleclaims" target ON target."RoleId" = source."RoleId" AND target."ClaimType" = 'permission'
            AND target."ClaimValue" = keys.report_key
        WHERE NOT EXISTS (
            SELECT 1 FROM "PermissionTypes" existing WHERE existing."RoleClaimId" = target."Id"
                AND existing."Key" = keys.report_key AND existing."Type" = granted."Type");
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql($$"""
        WITH keys(group_key, report_key) AS (VALUES {{Pairs}})
        DELETE FROM "PermissionTypes" WHERE "Key" IN (SELECT report_key FROM keys);
        WITH keys(group_key, report_key) AS (VALUES {{Pairs}})
        DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" IN (SELECT report_key FROM keys);
        """);
}
