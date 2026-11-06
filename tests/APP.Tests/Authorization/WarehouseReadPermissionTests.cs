using System.Reflection;
using API.Authorization;
using API.Controllers;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Warehouse, material and store reads: a page key, or the per-store check kept in the action (audit phase 4).</summary>
public sealed class WarehouseReadPermissionTests
{
    private static MethodInfo Action(Type controller, string name) => Assert.Single(controller.GetMethods(), method => method.Name == name);

    [Theory]
    [InlineData(typeof(MaterialController), "GetMaterials", "CanViewRawMaterials")]
    [InlineData(typeof(MaterialController), "GetMaterialStockAcrossWarehouses", "CanViewProductSchedules")]
    [InlineData(typeof(MaterialController), "GetInconsistencyReport", "CanFixStockInconsistencies")]
    [InlineData(typeof(WarehouseController), "GetWarehouse", "CanViewWarehouses")]
    [InlineData(typeof(ItemController), "GetItemWaybills", "CanViewWaybill")]
    public void A_warehouse_read_takes_the_key_of_a_page_that_shows_it(Type controller, string name, string key) =>
        Assert.Contains(controller.GetMethods().Where(method => method.Name == name)
            .SelectMany(method => method.GetCustomAttributes<AnyPermissionAttribute>()), attribute => attribute.Keys.Contains(key));

    [Theory]
    [InlineData(typeof(DamagedStocksController), "GetDamagedStocks")]
    [InlineData(typeof(ItemStockRequisitionController), "GetItems")]
    [InlineData(typeof(RecoverableItemsReportController), "GetRecoverableItemReports")]
    public void Store_reads_keep_their_per_store_check_in_the_action(Type controller, string name) =>
        Assert.NotNull(Action(controller, name).GetCustomAttribute<PermissionCheckedInActionAttribute>());
}
