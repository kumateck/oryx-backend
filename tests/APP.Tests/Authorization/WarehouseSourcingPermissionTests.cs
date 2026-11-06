using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Warehouse moves and purchase sourcing: the key follows the stored material kind or supplier type (audit phase 4).</summary>
public sealed class WarehouseSourcingPermissionTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => Guid.Empty;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static ApplicationDbContext Db() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    private static IEnumerable<MethodInfo> Actions(Type controller, string name) => controller.GetMethods().Where(method => method.Name == name);

    [Theory]
    [InlineData(typeof(MaterialController), "MoveMaterialBatchToWarehouseFromDistribute", PermissionKeys.CanAssignRawMaterialsStockToShelves)]
    [InlineData(typeof(MaterialController), "SupplyMaterialBatchToWarehouse", PermissionKeys.CanAssignRawMaterialsStockToShelves)]
    [InlineData(typeof(MaterialController), "MoveMaterialBatchToWarehouseFromHolding", PermissionKeys.CanIssueRawMaterialStockTransfers)]
    [InlineData(typeof(MaterialController), "MoveMaterialBatchV2", PermissionKeys.CanReassignRawMaterialStock)]
    public void A_warehouse_move_uses_the_key_for_the_material_kind(Type controller, string action, string rawKey) =>
        Assert.Equal(rawKey, Assert.Single(Actions(controller, action).SelectMany(method => method.GetCustomAttributes<MaterialKindPermissionAttribute>())).RawKey);

    [Fact]
    public void Issuing_a_stock_requisition_uses_the_kind_key()
    {
        var issues = Actions(typeof(RequisitionController), "IssueStockRequisition").ToList();
        Assert.Equal(PermissionKeys.CanIssueRawMaterialRequisitions, Assert.Single(issues.SelectMany(method => method.GetCustomAttributes<MaterialKindPermissionAttribute>())).RawKey);
        Assert.Empty(Actions(typeof(RequisitionController), "GetAlternativeBatches").SelectMany(method => method.GetCustomAttributes<MaterialKindPermissionAttribute>()));
    }

    [Theory]
    [InlineData("MarkQuotationAsSent", PermissionKeys.CanSendForeignQuotationRequest, PermissionKeys.CanSendLocalQuotationRequest)]
    [InlineData("ReceiveQuotationFromSupplier", PermissionKeys.CanSendForeignQuotationResponse, PermissionKeys.CanSendLocalQuotationResponse)]
    [InlineData("ProcessQuotationAndCreatePurchaseOrder", PermissionKeys.CanApplyChangesForForeignVendorPricingSelection, PermissionKeys.CanApplyChangesForLocalVendorPricingSelection)]
    public void A_sourcing_step_uses_the_key_for_the_supplier_type(string action, string foreign, string local)
    {
        var attribute = Assert.Single(Actions(typeof(RequisitionController), action).SelectMany(method => method.GetCustomAttributes<SupplierTypePermissionAttribute>()));
        Assert.Equal((foreign, local), (attribute.ForeignKey, attribute.LocalKey));
    }

    [Fact]
    public async Task Supplier_types_come_from_the_stored_supplier_and_quotation()
    {
        var db = Db();
        var foreign = new Supplier { Id = Guid.NewGuid(), Name = "Abroad", Type = SupplierType.Foreign };
        var local = new Supplier { Id = Guid.NewGuid(), Name = "Nearby", Type = SupplierType.Local };
        var quotation = new SupplierQuotation { Id = Guid.NewGuid(), SupplierId = local.Id };
        db.Suppliers.AddRange(foreign, local);
        db.SupplierQuotations.Add(quotation);
        await db.SaveChangesAsync();
        Assert.Equal([SupplierType.Foreign], await SupplierTypePermissionAttribute.Types(db, new Dictionary<string, object?> { ["supplierId"] = foreign.Id }));
        Assert.Equal([SupplierType.Local], await SupplierTypePermissionAttribute.Types(db, new Dictionary<string, object?> { ["supplierQuotationId"] = quotation.Id }));
        var both = await SupplierTypePermissionAttribute.Types(db, new Dictionary<string, object?>
        {
            ["processQuotations"] = new List<ProcessQuotation> { new() { SupplierId = foreign.Id }, new() { SupplierId = local.Id } },
        });
        Assert.Equal(2, both.Count);
    }

    [Fact]
    public async Task Shelf_batches_and_stock_requisitions_name_their_material_kind()
    {
        var db = Db();
        var foil = new Material { Id = Guid.NewGuid(), Code = "PM-1", Name = "Foil", Kind = MaterialKind.Package };
        var batch = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = foil.Id };
        var shelf = new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatchId = batch.Id };
        var requisition = new Requisition { Id = Guid.NewGuid(), Code = "SR-1" };
        db.Materials.Add(foil);
        db.MaterialBatches.Add(batch);
        db.ShelfMaterialBatches.Add(shelf);
        db.Requisitions.Add(requisition);
        db.RequisitionItems.Add(new RequisitionItem { Id = Guid.NewGuid(), RequisitionId = requisition.Id, MaterialId = foil.Id });
        await db.SaveChangesAsync();
        Assert.Equal([MaterialKind.Package], await MaterialKindPermissionAttribute.Kinds(db, new Dictionary<string, object> { ["request"] = new MoveShelfMaterialBatchRequest { ShelfMaterialBatchId = shelf.Id } }));
        Assert.Equal([MaterialKind.Package], await MaterialKindPermissionAttribute.Kinds(db, new Dictionary<string, object> { ["stockRequisitionId"] = requisition.Id }));
    }
}
