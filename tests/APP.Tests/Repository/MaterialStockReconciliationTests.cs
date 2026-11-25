using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APP.Tests.Repository;

public sealed class MaterialStockReconciliationTests
{
    [RepairPostgresFact]
    public async Task Density_fraction_reservations_do_not_overflow_and_match_approved_stock_in_the_department_unit()
    {
        await using var f = new IssuedMaterialRepairFixture();
        await f.Seed(DOMAIN.Entities.Products.Production.IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = f.Open();
        var lot = await db.MaterialBatches.Include(row => row.Material).FirstAsync();
        var millilitre = new DOMAIN.Entities.Base.UnitOfMeasure { Symbol = "ml", Name = "Millilitre" };
        db.UnitOfMeasures.Add(millilitre);
        lot.UoM = millilitre; lot.Material.Code = "RMG006"; lot.Material.Name = "Glycerine"; lot.Material.DensityGramsPerMilliliter = 1.261m;
        lot.TotalQuantity = 15860000m;
        var shelf = await db.ShelfMaterialBatches.SingleAsync(row => row.MaterialBatchId == lot.Id);
        shelf.Quantity = 15666106.264869151467089611419m; shelf.UoM = millilitre;
        lot.QuantityAssigned = shelf.Quantity;
        var first = await db.MaterialBatchReservedQuantities.SingleAsync(row => row.MaterialBatchId == lot.Id);
        first.Quantity = 74940.523394131641554321966693m; first.UoM = millilitre;
        var second = new MaterialBatchReservedQuantity { MaterialBatchId = lot.Id, UoM = millilitre, Quantity = 118953.2117367168913560666138m,
            WarehouseId = first.WarehouseId, WarehouseLocationShelfId = first.WarehouseLocationShelfId, ProductionScheduleProductId = first.ProductionScheduleProductId };
        db.MaterialBatchReservedQuantities.Add(second);
        await db.SaveChangesAsync();
        var rows = await new MaterialRepository(db, null!).GetReservedQuantitiesForMaterial(lot.MaterialId, f.Actor.DepartmentId);
        Assert.True(rows.IsSuccess, rows.Error.Description); Assert.Equal(2, rows.Value.Count);
        var measured = (await StoreStock.MeasureInStore(db, f.SourceId, [lot.MaterialId]))[lot.MaterialId];
        Assert.True(measured.OnHand > 19_000_000_000m);
        Assert.All(rows.Value, row => { Assert.Equal("mg", row.UomSymbol); Assert.InRange(Math.Abs(row.BalanceQuantity - measured.Free), 0, 0.000000001m); });
        Assert.InRange(Math.Abs(rows.Value.Sum(row => row.ReservedQuantity) - measured.Held), 0, 0.000000001m);
    }

    [Fact]
    public void Reserved_stock_read_keeps_its_registered_permission_filter()
    {
        var action = typeof(MaterialController).GetMethod(nameof(MaterialController.GetMaterialReservedQuantity))!;
        Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());
        var filter = Assert.Single(action.GetCustomAttributes<AnyPermissionAttribute>());
        foreach (var key in filter.Keys) Assert.Single(PermissionUtils.GeneratePermissions(), row => row.Key == key);
    }

    [Theory]
    [InlineData("vani")]
    [InlineData("RMV006")]
    [InlineData("RMV006 VANILLA")]
    [InlineData("R-03506220")]
    public async Task Bin_card_material_search_keeps_all_matching_entries_and_department_scope(string search)
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.Material.Name = "Vanilla Powder"; f.Lot.Material.Code = "RMV006"; f.Lot.BatchNumber = "R-03506220";
        for (var index = 0; index < 24; index++)
            db.BinCardInformation.Add(new() { MaterialBatchId = f.Lot.Id, WarehouseId = f.Storage.Id, UoMId = f.Lot.UoMId,
                Description = "TABLET Raw Warehouse", QuantityIssued = index, CreatedAt = DateTime.UtcNow.AddMinutes(index) });
        var other = new Warehouse { DepartmentId = f.OtherDepartment.Id, Type = WarehouseType.RawMaterialStorage };
        db.BinCardInformation.Add(new() { MaterialBatchId = f.Lot.Id, Warehouse = other, UoMId = f.Lot.UoMId });
        await db.SaveChangesAsync();
        var repository = new WarehouseRepository(db, f.Mapper, new MaterialRepository(db, f.Mapper), null!);
        var first = await repository.GetBinCardInformation(1, 20, search, null, f.Lot.MaterialId, f.Department.Id);
        var second = await repository.GetBinCardInformation(2, 20, search, null, f.Lot.MaterialId, f.Department.Id);
        Assert.Equal(24, first.Value.TotalRecordCount);
        Assert.Equal(20, first.Value.Data.Count()); Assert.Equal(4, second.Value.Data.Count());
        Assert.Equal(24, first.Value.Data.Concat(second.Value.Data).Select(row => row.Id).Distinct().Count());
        Assert.All(first.Value.Data.Concat(second.Value.Data), row => Assert.Equal(f.Storage.Name, row.WarehouseName));
    }

    [Fact]
    public async Task Bin_card_read_preserves_registered_authorization_and_denies_a_caller_without_it()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        var method = typeof(WarehouseController).GetMethod(nameof(WarehouseController.GetBinCardInformation))!;
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.NotEmpty(method.GetCustomAttributes<AuthorizeAttribute>());
        var filter = Assert.Single(method.GetCustomAttributes<AnyPermissionAttribute>());
        foreach (var key in filter.Keys) Assert.Single(PermissionUtils.GeneratePermissions(), permission => permission.Key == key);
        async Task<bool> Allowed()
        {
            var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(db).BuildServiceProvider() };
            http.Items["Sub"] = f.Actor.Id.ToString();
            var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
            var executing = new ActionExecutingContext(action, [], new Dictionary<string, object>(), null!);
            var allowed = false;
            await filter.OnActionExecutionAsync(executing, () => { allowed = true; return Task.FromResult(new ActionExecutedContext(action, [], null!)); });
            if (!allowed) Assert.Equal(403, Assert.IsType<ObjectResult>(executing.Result).StatusCode);
            return allowed;
        }
        Assert.False(await Allowed());
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanViewApprovedRawMaterials);
        Assert.True(await Allowed());
    }

    [Fact]
    public async Task A_shared_lots_reservation_balance_uses_only_the_selected_departments_stock_and_live_holds()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Shelf.Quantity = 100;
        var other = new Warehouse { DepartmentId = f.OtherDepartment.Id, Type = WarehouseType.RawMaterialStorage };
        var otherShelf = new WarehouseLocationShelf { WarehouseLocationRack = new() { WarehouseLocation = new() { Warehouse = other } } };
        db.ShelfMaterialBatches.Add(new() { MaterialBatchId = f.Lot.Id, WarehouseLocationShelf = otherShelf, UoMId = f.Lot.UoMId, Quantity = 300 });
        db.MaterialBatchReservedQuantities.AddRange(
            new() { MaterialBatchId = f.Lot.Id, WarehouseLocationShelfId = f.Shelf.WarehouseLocationShelfId, WarehouseId = f.Production.Id,
                ProductionScheduleProductId = f.Batch.Id, UoMId = f.Lot.UoMId, Quantity = 20 },
            new() { MaterialBatchId = f.Lot.Id, WarehouseLocationShelf = otherShelf, Warehouse = other,
                ProductionScheduleProductId = f.Batch.Id, UoMId = f.Lot.UoMId, Quantity = 40 });
        await db.SaveChangesAsync();
        var repository = new MaterialRepository(db, f.Mapper);
        var own = Assert.Single((await repository.GetReservedQuantitiesForMaterial(f.Lot.MaterialId, f.Department.Id)).Value);
        var otherRow = Assert.Single((await repository.GetReservedQuantitiesForMaterial(f.Lot.MaterialId, f.OtherDepartment.Id)).Value);
        Assert.Equal(20, own.ReservedQuantity); Assert.Equal(80, own.BalanceQuantity);
        Assert.Equal(40, otherRow.ReservedQuantity); Assert.Equal(260, otherRow.BalanceQuantity);
    }
}
