using System.Reflection;
using API.Authorization;
using API.Controllers;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Production, product and order reads: each needs a key of a page that shows it (audit phase 4).</summary>
public sealed class ProductionReadPermissionTests
{
    private static IReadOnlyList<string> Keys(Type controller, string name) =>
        Assert.Single(Assert.Single(controller.GetMethods(), method => method.Name == name).GetCustomAttributes<AnyPermissionAttribute>()).Keys;

    [Theory]
    [InlineData(typeof(ProductionScheduleController), "GetProductionSchedules", "CanViewProductSchedules")]
    [InlineData(typeof(ProductionScheduleController), "GetProductionActivityById", "CanViewPlannedProducts")]
    [InlineData(typeof(ProductionScheduleController), "GetBatchManufacturingRecords", "CanViewIssuedBmrBprs")]
    [InlineData(typeof(ProductionScheduleController), "GetFinishedGoodsTransferNotes", "CanViewFgtn")]
    [InlineData(typeof(ProductionScheduleController), "GetMaterialReturnNotes", "CanViewMaterialsReturnNote")]
    [InlineData(typeof(ProductionScheduleController), "GetBatchPackagingRecords", "CanViewIssuedBmrBprs")]
    [InlineData(typeof(ProductionOrderController), "GetProductionOrders", "CanViewOrder")]
    [InlineData(typeof(ProductionOrderController), "GetInvoice", "CanViewInvoice")]
    [InlineData(typeof(ProductController), "GetEquipments", "CanViewEquipment")]
    [InlineData(typeof(BillOfMaterialController), "GetBillOfMaterials", "CanViewProductionProductBom")]
    public void A_production_read_takes_the_key_of_a_page_that_shows_it(Type controller, string name, string key) =>
        Assert.Contains(key, Keys(controller, name));
}
