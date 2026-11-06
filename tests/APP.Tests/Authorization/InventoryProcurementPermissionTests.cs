using System.Reflection;
using API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Store procurement: fixed-key checks sit on the action, where the catalog sees them (audit phase 4).</summary>
public sealed class InventoryProcurementPermissionTests
{
    private static string[] Policies(string action) => typeof(InventoryProcurementController).GetMethods().Where(method => method.Name == action)
        .SelectMany(method => method.GetCustomAttributes<AuthorizeAttribute>()).Select(attribute => attribute.Policy!).Order().ToArray();

    [Theory]
    [InlineData("CreateSourceRequisition", "CanSourcePurchaseRequisitionSourcing,CanViewPurchaseRequisitionsSourcing")]
    [InlineData("CreateMarketRequisition", "CanSourcePurchaseRequisitionSourcing,CanViewPurchaseRequisitionsSourcing")]
    [InlineData("SendQuotationToVendor", "CanCreateQuotationRequestForTrustedVendor,CanViewQuotationRequestsForTrustedVendors")]
    [InlineData("ReceiveQuotationFromVendor", "CanSendQuotationResponseForTrustedVendor,CanViewQuotationResponsesForTrustedVendors")]
    [InlineData("ProcessTrustedVendorQuotationAndCreateMemo", "CanCreateMemoForTrustedVendor,CanViewTrustedVendorPricingSelection")]
    [InlineData("ProcessOpenMarketMemo", "CanCreateOpenMarketMemo,CanViewOpenMarketPricingSelection")]
    [InlineData("CreateMarketRequisitionVendor", "CanCreateOpenMarketQuotationSheet,CanViewOpenMarketQuotationSheets")]
    [InlineData("UploadStockEntries", "CanSourcePurchaseRequisitionSourcing,CanViewPurchaseRequisitionsSourcing")]
    public void A_procurement_write_needs_its_view_and_task_keys(string action, string keys) =>
        Assert.Equal(keys.Split(','), Policies(action));
}
