using System.Reflection;
using API.Controllers;
using APP.Utils;
using INFRASTRUCTURE.Migrations;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Production orders and finished-goods dispatch: each task needs its own key on the server (audit phase 4).</summary>
public sealed class ProductionOrderPermissionTests
{
    [Theory]
    [InlineData("CreateProductionOrder", PermissionKeys.CanCreateOrders)]
    [InlineData("DeleteProductionOrder", PermissionKeys.CanDeleteOrder)]
    [InlineData("AllocateProductionOrder", PermissionKeys.CanGeneratePackingList)]
    [InlineData("UpdateProductionOrderAllocation", PermissionKeys.CanGeneratePackingList)]
    [InlineData("CreateProformaInvoice", PermissionKeys.CanGenerateProformaInvoice)]
    [InlineData("SendProformaInvoiceToCustomer", PermissionKeys.CanSendProformaInvoice)]
    [InlineData("CreateWaybillFromProductionOrder", PermissionKeys.CanCreateWaybillForFgw)]
    [InlineData("MarkAllocationProductionOrderAsLoaded", PermissionKeys.CanLoadFinishedGoods)]
    [InlineData("MarkProductAllocationAsDelivered", PermissionKeys.CanDeliverFinishedGoods)]
    [InlineData("DeleteInvoice", PermissionKeys.CanDeleteInvoice)]
    public void An_order_write_needs_its_task_key(string action, string key) =>
        Assert.All(typeof(ProductionOrderController).GetMethods().Where(method => method.Name == action),
            method => Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy));

    [Fact]
    public void Loading_and_delivering_go_to_everyone_who_could_do_them_and_unused_tasks_to_nobody()
    {
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToHashSet();
        Assert.All(GrantFinishedGoodsDispatchTasks.Grants, grant => { Assert.Contains(grant.Key, registered); Assert.Contains(grant.From, registered); });
        foreach (var from in new[] { PermissionKeys.CanViewPackingList, PermissionKeys.CanViewInvoice })
        {
            Assert.Contains((PermissionKeys.CanLoadFinishedGoods, from), GrantFinishedGoodsDispatchTasks.Grants);
            Assert.Contains((PermissionKeys.CanDeliverFinishedGoods, from), GrantFinishedGoodsDispatchTasks.Grants);
        }
        Assert.Equal([PermissionKeys.CanDeliverFinishedGoods, PermissionKeys.CanLoadFinishedGoods],
            GrantFinishedGoodsDispatchTasks.Grants.Select(grant => grant.Key).Distinct().Order());
    }
}
