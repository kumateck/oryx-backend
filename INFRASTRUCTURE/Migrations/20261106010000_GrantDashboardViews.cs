using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFRASTRUCTURE.Migrations
{
    /// <summary>
    /// Each dashboard now has its own view key, checked by the server. Until now the desktop showed a dashboard to anyone
    /// holding any of a list of unrelated page keys, and the server checked nothing. Every role holding one of those keys
    /// gets the dashboard key with the same access types, so everyone who saw a dashboard still does. Data only.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261106010000_GrantDashboardViews")]
    public partial class GrantDashboardViews : Migration
    {
        public static readonly (string Key, string From)[] Grants =
        [
            ("CanViewHrDashboard", "CanViewActiveEmployee"),
            ("CanViewHrDashboard", "CanViewCompensation"),
            ("CanViewHrDashboard", "CanViewPayrollConfiguration"),
            ("CanViewHrDashboard", "CanViewPayrollRuns"),
            ("CanViewHrDashboard", "CanViewPayslip"),
            ("CanViewHrDashboard", "CanViewOwnPayslip"),
            ("CanViewHrDashboard", "CanViewPerformanceCycles"),
            ("CanViewHrDashboard", "CanViewOwnGoals"),
            ("CanViewHrDashboard", "CanViewTeamGoals"),
            ("CanViewHrDashboard", "CanViewPerformanceReviews"),
            ("CanViewHrExecutiveSummary", "CanViewActiveEmployee"),
            ("CanViewProductionHealthDashboard", "CanViewPlannedProducts"),
            ("CanViewFgtnDashboard", "CanViewOrder"),
            ("CanViewWarehouseDashboard", "CanViewRawMaterialsItems"),
            ("CanViewWarehouseDashboard", "CanViewPackagingMaterialsItems"),
            ("CanViewInventoryCommandCenter", "CanViewRawMaterialsItems"),
            ("CanViewInventoryCommandCenter", "CanViewPackagingMaterialsItems"),
            ("CanViewLogisticsDashboard", "CanViewShipmentInvoice"),
            ("CanViewLogisticsDashboard", "CanViewWaybill"),
            ("CanViewProcurementDashboard", "CanViewPurchaseRequisitions"),
            ("CanViewQcDashboard", "CanViewRawMaterialGoodsReceiptNotes"),
            ("CanViewQcDashboard", "CanViewPackagingMaterialGoodsReceiptNotes"),
            ("CanViewQaDashboard", "CanViewAnalyticalTestRequests"),
            ("CanViewServicesDashboard", "CanViewServices"),
            ("CanViewServicesDashboard", "CanViewContractors"),
            ("CanViewServicesDashboard", "CanViewJobRequests"),
            ("CanViewServicesDashboard", "CanViewJobOrders"),
            ("CanViewServicesDashboard", "CanViewServicesQuotationResponses"),
            ("CanViewServicesDashboard", "CanViewServicesPricingSelection"),
            ("CanViewServicesDashboard", "CanViewServicesProformaRequests"),
            ("CanViewServicesDashboard", "CanViewServicesProformaResponses"),
            ("CanViewServicesDashboard", "CanViewServicesMemo"),
            ("CanViewServicesDashboard", "CanViewServicesPurchaseOrders"),
            ("CanViewStoresDashboard", "CanViewItemsInReagentStore"),
            ("CanViewStoresDashboard", "CanViewItemsInEquipmentStore"),
            ("CanViewStoresDashboard", "CanViewItemsInGeneralStore"),
            ("CanViewStoresDashboard", "CanViewItemsInItStore"),
            ("CanViewStoresDashboard", "CanViewReagentStoreReceivingArea"),
            ("CanViewStoresDashboard", "CanViewEquipmentStoreReceivingArea"),
            ("CanViewStoresDashboard", "CanViewGeneralStoreReceivingArea"),
            ("CanViewStoresDashboard", "CanViewItStoreReceivingArea"),
            ("CanViewStoresDashboard", "CanViewReagentStoreAvailableStocks"),
            ("CanViewStoresDashboard", "CanViewEquipmentStoreAvailableStocks"),
            ("CanViewStoresDashboard", "CanViewGeneralStoreAvailableStocks"),
            ("CanViewStoresDashboard", "CanViewItStoreAvailableStocks"),
            ("CanViewStoresDashboard", "CanViewReagentStorePurchaseRequisitions"),
            ("CanViewStoresDashboard", "CanViewEquipmentStorePurchaseRequisitions"),
            ("CanViewStoresDashboard", "CanViewGeneralStorePurchaseRequisitions"),
            ("CanViewStoresDashboard", "CanViewItStorePurchaseRequisitions"),
            ("CanViewStoresDashboard", "CanViewReagentStoreStockRequisitions"),
            ("CanViewStoresDashboard", "CanViewEquipmentStoreStockRequisitions"),
            ("CanViewStoresDashboard", "CanViewGeneralStoreStockRequisitions"),
            ("CanViewStoresDashboard", "CanViewItStoreStockRequisitions"),
            ("CanViewStoresDashboard", "CanViewReagentStoreIssueStocks"),
            ("CanViewStoresDashboard", "CanViewEquipmentStoreIssueStocks"),
            ("CanViewStoresDashboard", "CanViewGeneralStoreIssueStocks"),
            ("CanViewStoresDashboard", "CanViewItStoreIssueStocks"),
            ("CanViewStoresDashboard", "CanViewReagentStoreReturnItems"),
            ("CanViewStoresDashboard", "CanViewEquipmentStoreReturnItems"),
            ("CanViewStoresDashboard", "CanViewGeneralStoreReturnItems"),
            ("CanViewStoresDashboard", "CanViewItStoreReturnItems"),
            ("CanViewStoresDashboard", "CanViewReagentStoreDamageMissingItems"),
            ("CanViewStoresDashboard", "CanViewEquipmentStoreDamageMissingItems"),
            ("CanViewStoresDashboard", "CanViewGeneralStoreDamageMissingItems"),
            ("CanViewStoresDashboard", "CanViewItStoreDamageMissingItems"),
            ("CanViewStoresDashboard", "CanViewPurchaseRequisitionsSourcing"),
            ("CanViewStoresDashboard", "CanViewQuotationRequestsForTrustedVendors"),
            ("CanViewStoresDashboard", "CanViewQuotationResponsesForTrustedVendors"),
            ("CanViewStoresDashboard", "CanViewTrustedVendorPricingSelection"),
            ("CanViewStoresDashboard", "CanViewMemoForTrustedVendors"),
            ("CanViewStoresDashboard", "CanViewPurchaseOrdersForTrustedVendors"),
            ("CanViewStoresDashboard", "CanViewOpenMarketQuotationSheets"),
            ("CanViewStoresDashboard", "CanViewOpenMarketPricingSelection"),
            ("CanViewStoresDashboard", "CanViewOpenMarketMemo"),
            ("CanViewStoresDashboard", "CanViewOpenMarketPurchaseOrders"),
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
            foreach (var key in Grants.Select(grant => grant.Key).Distinct())
                migrationBuilder.Sql($$"""
                    DELETE FROM "PermissionTypes" WHERE "Key" = '{{key}}';
                    DELETE FROM "roleclaims" WHERE "ClaimType" = 'permission' AND "ClaimValue" = '{{key}}';
                    """);
        }
    }
}
