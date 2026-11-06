using System.Reflection;
using API.Authorization;
using API.Controllers;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Procurement, requisition and approval reads: each needs a key of a page that shows it (audit phase 4).</summary>
public sealed class ProcurementReadPermissionTests
{
    private static IEnumerable<AnyPermissionAttribute> Rules(Type controller, string name) =>
        controller.GetMethods().Where(method => method.Name == name).SelectMany(method => method.GetCustomAttributes<AnyPermissionAttribute>());

    [Theory]
    [InlineData(typeof(ProcurementController), "GetPurchaseOrder", "CanViewForeignPurchaseOrder")]
    [InlineData(typeof(ProcurementController), "GetPurchaseOrder", "CanViewLocalPurchaseOrder")]
    [InlineData(typeof(ProcurementController), "GetShipmentInvoices", "CanViewShipmentInvoice")]
    [InlineData(typeof(ProcurementController), "GetSupplier", "CanViewVendors")]
    [InlineData(typeof(RequisitionController), "GetSourceRequisitions", "CanSourcePurchaseRequisition")]
    [InlineData(typeof(InventoryProcurementController), "GenerateMemoCode", "CanCreateOpenMarketMemo")]
    [InlineData(typeof(StockAdjustmentController), "GetStockAdjustments", "CanViewMaterialStockAdjustment")]
    public void A_procurement_read_takes_the_key_of_a_page_that_shows_it(Type controller, string name, string key) =>
        Assert.Contains(Rules(controller, name), rule => rule.Keys.Contains(key));

    [Fact]
    public void Reference_lists_and_the_approval_history_panel_are_open_with_a_reason()
    {
        Assert.All(typeof(CollectionController).GetMethods().Where(method => method.Name is "GetItemCollection" or "GetUoM"),
            method => Assert.False(string.IsNullOrWhiteSpace(method.GetCustomAttribute<SignedInUserAttribute>()?.Reason)));
        Assert.NotNull(typeof(ApprovalController).GetMethod("GetApprovalByModelId")!.GetCustomAttribute<SignedInUserAttribute>());
    }
}
