using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public sealed class MaterialStockTraceTests
{
    [Fact]
    public async Task Confirmed_consumption_requires_an_issued_allocation_and_records_its_production_trace_once()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.TotalQuantity = 100; f.Lot.QuantityAssigned = 70; f.Shelf.Quantity = 70;
        var allocation = new MaterialBatchReservedQuantity { MaterialBatchId = f.Lot.Id, WarehouseId = f.Production.Id,
            WarehouseLocationShelfId = f.Shelf.WarehouseLocationShelfId, ProductionScheduleProductId = f.Batch.Id,
            Quantity = 30, UoMId = f.Lot.UoMId };
        db.MaterialBatchReservedQuantities.Add(allocation); await db.SaveChangesAsync();
        var repository = new MaterialRepository(db, f.Mapper);
        Assert.Equal("Material.ConsumptionUnissued", (await repository.ConsumeMaterialAtLocation(f.Lot.Id, f.Production.Id, 30, f.Actor.Id, allocation.Id)).Error.Code);
        db.Requisitions.Add(new() { DepartmentId = f.Department.Id, ProductionScheduleProductId = f.Batch.Id,
            RequisitionType = RequisitionType.Stock, Status = RequestStatus.Completed,
            Items = [new() { MaterialId = f.Lot.MaterialId, Quantity = 30, Status = RequestStatus.Completed, UoMId = f.Lot.UoMId }] });
        await db.SaveChangesAsync();
        Assert.Equal("Material.ConsumptionPermission", (await repository.ConsumeMaterialAtLocation(f.Lot.Id, f.Production.Id, 30, f.Actor.Id, allocation.Id)).Error.Code);
        Assert.Empty(await db.MaterialBatchEvents.ToListAsync()); Assert.Equal(0, f.Lot.ConsumedQuantity);
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanUpdateProductionStep);
        var consumed = await repository.ConsumeMaterialAtLocation(f.Lot.Id, f.Production.Id, 30, f.Actor.Id, allocation.Id);
        Assert.True(consumed.IsSuccess, consumed.Error.Description);
        var evidence = await db.MaterialBatchEvents.SingleAsync();
        Assert.Equal(allocation.Id, evidence.MaterialBatchReservedQuantityId);
        Assert.Equal(f.Batch.Id, evidence.ProductionScheduleProductId); Assert.Equal(f.Actor.Id, evidence.UserId);
        Assert.Equal(30, f.Lot.ConsumedQuantity); Assert.Equal(70, f.Shelf.Quantity);
        Assert.NotNull((await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().SingleAsync()).DeletedAt);
        Assert.Equal("Material.ConsumptionAllocation", (await repository.ConsumeMaterialAtLocation(f.Lot.Id, f.Production.Id, 30, f.Actor.Id, allocation.Id)).Error.Code);
        Assert.Single(await db.MaterialBatchEvents.ToListAsync());
    }

    [RepairPostgresFact]
    public async Task Trace_queries_translate_and_read_one_postgres_snapshot()
    {
        await using var f = new IssuedMaterialRepairFixture(); await f.Seed(DOMAIN.Entities.Products.Production.IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = f.Open();
        await SharedDensityTransferTests.Grant(db, f.Actor.UserId!.Value, PermissionKeys.CanViewRawMaterialStockTrace);
        var lot = await db.MaterialBatches.FirstAsync();
        var result = await new MaterialRepository(db, null!).GetMaterialStockTrace(lot.MaterialId, MaterialKind.Raw,
            f.Actor.DepartmentId!.Value, DateTime.UtcNow.Date, f.Actor.UserId.Value);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(result.Value.LedgerClosing, result.Value.StoreStock);
        Assert.Equal(0, result.Value.LedgerGap); Assert.NotEmpty(result.Value.Rows); Assert.NotEmpty(result.Value.LedgerRows);
    }

    [Theory]
    [InlineData(nameof(MaterialStockTraceController.Raw), PermissionKeys.CanViewRawMaterialStockTrace)]
    [InlineData(nameof(MaterialStockTraceController.Packaging), PermissionKeys.CanViewPackagingMaterialStockTrace)]
    [InlineData(nameof(MaterialStockTraceController.PrintRaw), PermissionKeys.CanPrintRawMaterialStockTrace)]
    [InlineData(nameof(MaterialStockTraceController.PrintPackaging), PermissionKeys.CanPrintPackagingMaterialStockTrace)]
    public void Each_trace_endpoint_requires_its_registered_variant_key(string action, string key)
    {
        var method = typeof(MaterialStockTraceController).GetMethod(action)!;
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Single(PermissionUtils.GeneratePermissions(), row => row.Key == key);
        Assert.Contains(GrantMaterialStockTraceViews.Grants, grant => grant.Key == key);
    }

    [Theory]
    [InlineData(MaterialKind.Raw)]
    [InlineData(MaterialKind.Package)]
    public async Task Store_reserved_floor_and_consumed_positions_reconcile_without_double_counting(MaterialKind kind)
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.Material.Kind = kind; f.Storage.Type = kind == MaterialKind.Raw ? DOMAIN.Entities.Warehouses.WarehouseType.RawMaterialStorage : DOMAIN.Entities.Warehouses.WarehouseType.PackagedStorage;
        f.Shelf.Quantity = 70; f.Lot.TotalQuantity = 100; f.Lot.QuantityAssigned = 70; f.Lot.ConsumedQuantity = 12;
        var issuedBatch = new ProductionScheduleProduct { ProductId = f.Batch.ProductId, ProductionScheduleId = f.Schedule.Id, BatchNumber = "ISSUED-1" };
        db.MaterialBatchReservedQuantities.AddRange(
            new() { MaterialBatchId = f.Lot.Id, WarehouseLocationShelfId = f.Shelf.WarehouseLocationShelfId,
                WarehouseId = f.Production.Id, UoMId = f.Lot.UoMId, ProductionScheduleProductId = f.Batch.Id, Quantity = 10 },
            new() { MaterialBatchId = f.Lot.Id, WarehouseLocationShelfId = f.Shelf.WarehouseLocationShelfId,
                WarehouseId = f.Production.Id, UoMId = f.Lot.UoMId, ProductionScheduleProduct = issuedBatch, Quantity = 18 });
        db.Requisitions.Add(new() { Code = "RM-ISSUE-1", ProductionScheduleProduct = issuedBatch, DepartmentId = f.Department.Id,
            Status = RequestStatus.Completed, RequisitionType = RequisitionType.Stock, Items = [new() { MaterialId = f.Lot.MaterialId, Status = RequestStatus.Completed, Quantity = 30, UoMId = f.Lot.UoMId }] });
        var since = DateTime.UtcNow.Date;
        db.BinCardInformation.AddRange(
            new() { MaterialBatchId = f.Lot.Id, WarehouseId = f.Storage.Id, UoMId = f.Lot.UoMId, QuantityReceived = 100, CreatedAt = since.AddDays(-1) },
            new() { MaterialBatchId = f.Lot.Id, WarehouseId = f.Storage.Id, UoMId = f.Lot.UoMId, QuantityIssued = 30 });
        db.MassMaterialBatchMovements.Add(new() { BatchId = f.Lot.Id, FromWarehouseId = f.Storage.Id, ToWarehouseId = f.Production.Id,
            Quantity = 30, MovedAt = DateTime.UtcNow, MovedById = f.Actor.Id });
        db.MaterialBatchEvents.Add(new() { BatchId = f.Lot.Id, Quantity = 12, Type = EventType.Consumed,
            ConsumptionWarehouseId = f.Production.Id, ConsumedAt = DateTime.UtcNow, UserId = f.Actor.Id });
        await db.SaveChangesAsync();
        var opening = await db.BinCardInformation.SingleAsync(row => row.QuantityReceived == 100);
        opening.CreatedAt = since.AddDays(-1);
        await db.SaveChangesAsync();
        var repository = new MaterialRepository(db, f.Mapper);
        Assert.Equal("StockTrace.Permission", (await repository.GetMaterialStockTrace(f.Lot.MaterialId, kind, f.Department.Id, since, f.Actor.Id)).Error.Code);
        var wrong = kind == MaterialKind.Raw ? PermissionKeys.CanViewPackagingMaterialStockTrace : PermissionKeys.CanViewRawMaterialStockTrace;
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, wrong);
        Assert.Equal("StockTrace.Permission", (await repository.GetMaterialStockTrace(f.Lot.MaterialId, kind, f.Department.Id, since, f.Actor.Id)).Error.Code);
        var key = kind == MaterialKind.Raw ? PermissionKeys.CanViewRawMaterialStockTrace : PermissionKeys.CanViewPackagingMaterialStockTrace;
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, key);
        Assert.Equal("StockTrace.Department", (await repository.GetMaterialStockTrace(f.Lot.MaterialId, kind, f.OtherDepartment.Id, since, f.Actor.Id)).Error.Code);
        Assert.Equal("StockTrace.Kind", (await repository.GetMaterialStockTrace(f.Lot.MaterialId, kind == MaterialKind.Raw ? MaterialKind.Package : MaterialKind.Raw, f.Department.Id, since, f.Actor.Id)).Error.Code);
        var read = await repository.GetMaterialStockTrace(f.Lot.MaterialId, kind, f.Department.Id, since, f.Actor.Id);
        Assert.True(read.IsSuccess, read.Error.Description);
        var stock = read.Value;
        Assert.Equal("StockTrace.Permission", (await repository.GetMaterialStockTrace(f.Lot.MaterialId, kind, f.Department.Id, since, f.Actor.Id, forPrint: true)).Error.Code);
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, kind == MaterialKind.Raw ? PermissionKeys.CanPrintRawMaterialStockTrace : PermissionKeys.CanPrintPackagingMaterialStockTrace);
        Assert.True((await repository.GetMaterialStockTrace(f.Lot.MaterialId, kind, f.Department.Id, since, f.Actor.Id, forPrint: true)).IsSuccess);
        Assert.Equal(100, stock.OpeningBalance); Assert.Equal(30, stock.Issued); Assert.Equal(70, stock.LedgerClosing);
        Assert.Equal(70, stock.StoreStock); Assert.Equal(10, stock.ReservedStock); Assert.Equal(60, stock.FreeStock);
        Assert.Equal(18, stock.ProductionFloorStock); Assert.Equal(12, stock.ConsumedStock);
        Assert.Equal(100, stock.StoreStock + stock.ProductionFloorStock + stock.ConsumedStock);
        Assert.Equal(0, stock.LedgerGap); Assert.Empty(stock.Gaps);
        Assert.Equal("ISSUED-1", Assert.Single(stock.Rows, row => row.State == MaterialStockTraceState.ProductionFloor).ProductBatchNumber);
        // Missing accounting evidence is visible; tracing never changes physical stock to make the numbers agree.
        db.BinCardInformation.Remove(await db.BinCardInformation.SingleAsync(row => row.QuantityIssued == 30));
        await db.SaveChangesAsync();
        var missing = (await repository.GetMaterialStockTrace(f.Lot.MaterialId, kind, f.Department.Id, since, f.Actor.Id)).Value;
        Assert.Equal(30, missing.LedgerGap); Assert.NotEmpty(missing.Gaps); Assert.Equal(70, f.Shelf.Quantity);
    }
}
