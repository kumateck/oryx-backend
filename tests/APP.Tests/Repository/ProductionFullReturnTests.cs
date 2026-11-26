using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public sealed class ProductionFullReturnTests
{
    [Fact]
    public void Full_return_and_receipt_keep_their_specific_registered_permissions()
    {
        var action = typeof(ProductionScheduleController).GetMethod(nameof(ProductionScheduleController.ReturnBeforeProduction))!;
        Assert.Equal(PermissionKeys.CanReturnProductionMaterials, Assert.Single(action.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());
        var complete = typeof(ProductionScheduleController).GetMethod(nameof(ProductionScheduleController.CompleteMaterialReturnNoteById))!;
        Assert.Equal(PermissionKeys.CanCompleteMaterialReturnNote, Assert.Single(complete.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(complete.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Single(PermissionUtils.GeneratePermissions(), row => row.Key == PermissionKeys.CanCompleteMaterialReturnNote);
        var receipt = typeof(MaterialController).GetMethod(nameof(MaterialController.SupplyMaterialBatchToWarehouse))!;
        Assert.Empty(receipt.GetCustomAttributes<AllowAnonymousAttribute>());
        var filter = Assert.Single(receipt.GetCustomAttributes<MaterialKindPermissionAttribute>());
        Assert.Equal(PermissionKeys.CanAssignRawMaterialsStockToShelves, filter.RawKey);
        Assert.Equal(PermissionKeys.CanAssignPackagingMaterialsStockToShelves, filter.PackagingKey);
        foreach (var key in new[] { PermissionKeys.CanReturnProductionMaterials, filter.RawKey, filter.PackagingKey })
            // Packaging shelf assignment is an existing SharedToSplit catalog entry on both intake screens.
            Assert.Equal(key == PermissionKeys.CanAssignPackagingMaterialsStockToShelves ? 2 : 1,
                PermissionUtils.GeneratePermissions().Count(row => row.Key == key));
    }

    [RepairPostgresFact]
    public async Task Packaging_only_returns_issued_packaging_releases_raw_holds_and_waits_for_warehouse_receipt_once()
    {
        await using var f = new IssuedMaterialRepairFixture(); await f.Seed();
        await using var db = f.Open();
        var raw = await OtherKind(db, f, false);
        var batch = await db.ProductionScheduleProducts.SingleAsync();
        batch.BatchNumber = "L014K029";
        var step = await db.ProductionActivitySteps.OrderBy(row => row.Order).FirstAsync();
        var bmr = new BatchManufacturingRecord { ProductionScheduleProductId = f.BatchId, ProductionActivityStepId = step.Id, BatchNumber = "L014K029" };
        db.BatchManufacturingRecords.Add(bmr);
        db.BatchPackagingRecords.Add(new() { ProductionScheduleProductId = f.BatchId, ProductionActivityStepId = step.Id, BatchNumber = "L014K029" });
        await db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, (await ProductionFullReturn.Run(db, f.BatchId, "Cancel unconsumed batch")).Error.Type);
        Assert.Empty(await db.MaterialReturnNotes.ToListAsync());
        await SharedDensityTransferTests.Grant(db, f.Actor.UserId!.Value, PermissionKeys.CanReturnProductionMaterials);
        var before = await db.ShelfMaterialBatches.AsNoTracking().Select(row => row.Quantity).OrderBy(row => row).ToListAsync();
        var returned = await ProductionFullReturn.Run(db, f.BatchId, "Cancel unconsumed batch");
        Assert.True(returned.IsSuccess, returned.Error.Description);
        Assert.True((await db.ProductionScheduleProducts.SingleAsync()).Cancelled);
        Assert.Equal(ProductionStatus.Cancelled, (await db.ProductionActivities.SingleAsync()).Status);
        var note = await db.MaterialReturnNotes.Include(row => row.FullReturns).SingleAsync();
        Assert.Equal(MaterialReturnStatus.Pending, note.Status); Assert.Equal("L014K029", note.BatchNumber);
        var schedules = new ProductionScheduleRepository(db, null!, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());
        Assert.Equal(ErrorType.Forbidden, (await schedules.CompleteMaterialReturn(note.Id)).Error.Type);
        await SharedDensityTransferTests.Grant(db, f.Actor.UserId.Value, PermissionKeys.CanCompleteMaterialReturnNote);
        Assert.Equal("MaterialReturn.AwaitingReceipt", (await schedules.CompleteMaterialReturn(note.Id)).Error.Code);
        Assert.Equal(2, note.FullReturns.Count);
        Assert.All(await db.MaterialReturnNoteFullReturns.IgnoreQueryFilters().Include(row => row.MaterialBatchReservedQuantity).ThenInclude(row => row.MaterialBatch).ThenInclude(row => row.Material).ToListAsync(),
            row => Assert.Equal(MaterialKind.Package, row.MaterialBatchReservedQuantity.MaterialBatch.Material.Kind));
        Assert.NotNull((await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().SingleAsync(row => row.MaterialBatchId == raw.Id)).ReleasedAt);
        Assert.Empty(await ProductionMaterialAllocator.UnissuedReservations(db).Where(row => row.ProductionScheduleProductId == f.BatchId).ToListAsync());
        Assert.Equal(before, await db.ShelfMaterialBatches.AsNoTracking().Select(row => row.Quantity).OrderBy(row => row).ToListAsync());
        Assert.True((await BmrBatchNumberRules.Validate(db, "L014K029")).IsSuccess);
        Assert.NotNull((await db.BatchManufacturingRecords.IgnoreQueryFilters().SingleAsync()).DeletedAt);
        Assert.True((await ProductionFullReturn.Run(db, f.BatchId, "Cancel unconsumed batch")).IsSuccess);
        Assert.Single(await db.MaterialReturnNotes.ToListAsync());
        var allocations = await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes().Include(row => row.MaterialBatch)
            .Where(row => row.MaterialBatch.Material.Kind == MaterialKind.Package).ToListAsync();
        var repository = new MaterialRepository(db, null!);
        var trace = await MaterialStockTraceQuery.Load(db, await db.Materials.SingleAsync(row => row.Id == allocations[0].MaterialBatch.MaterialId),
            f.Actor.DepartmentId!.Value, DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow);
        Assert.True(trace.IsSuccess, trace.Error.Description); Assert.Empty(trace.Value.Gaps);
        Assert.Contains(trace.Value.Rows, row => row.Evidence == "Full return awaiting warehouse receipt and shelving");
        foreach (var allocation in allocations)
        {
            var request = new SupplyMaterialBatchRequest { MaterialReturnNoteId = note.Id, MaterialBatchId = allocation.MaterialBatchId,
                ShelfMaterialBatches = [new() { WarehouseLocationShelfId = allocation.WarehouseLocationShelfId!.Value, Quantity = allocation.Quantity, UomId = f.UnitId }] };
            if (allocation == allocations[0])
            {
                var denied = await repository.SupplyMaterialBatchToWarehouse(request, f.Actor.UserId.Value);
                Assert.Equal(ErrorType.Forbidden, denied.Error.Type);
                await SharedDensityTransferTests.Grant(db, f.Actor.UserId.Value, PermissionKeys.CanAssignRawMaterialsStockToShelves);
                Assert.Equal(ErrorType.Forbidden, (await repository.SupplyMaterialBatchToWarehouse(request, f.Actor.UserId.Value)).Error.Type);
                await SharedDensityTransferTests.Grant(db, f.Actor.UserId.Value, PermissionKeys.CanAssignPackagingMaterialsStockToShelves);
            }
            var received = await repository.SupplyMaterialBatchToWarehouse(request, f.Actor.UserId.Value);
            Assert.True(received.IsSuccess, received.Error.Description);
            Assert.True((await repository.SupplyMaterialBatchToWarehouse(request, f.Actor.UserId.Value)).IsFailure);
            var lot = await db.MaterialBatches.SingleAsync(row => row.Id == allocation.MaterialBatchId);
            Assert.Equal(lot.TotalQuantity, lot.QuantityAssigned); Assert.Equal(0, lot.ConsumedQuantity);
            var after = await MaterialStockTraceQuery.Load(db, await db.Materials.SingleAsync(row => row.Id == lot.MaterialId),
                f.Actor.DepartmentId.Value, DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow);
            Assert.Empty(after.Value.Gaps); Assert.Equal(0, after.Value.ProductionFloorStock); Assert.Equal(0, after.Value.ConsumedStock);
            Assert.Equal(lot.TotalQuantity, after.Value.StoreStock); Assert.Equal(lot.TotalQuantity, after.Value.LedgerClosing);
        }
        Assert.Equal(MaterialReturnStatus.Completed, note.Status);
        Assert.True((await schedules.CompleteMaterialReturn(note.Id)).IsSuccess);
        Assert.Equal(0, raw.ConsumedQuantity);
    }

    [RepairPostgresFact]
    public async Task Raw_only_and_both_issued_return_exactly_the_issued_kinds()
    {
        foreach (var both in new[] { false, true })
        {
            await using var f = new IssuedMaterialRepairFixture();
            await f.Seed(IssuedMaterialRepairKind.Bom);
            await using var db = f.Open();
            await OtherKind(db, f, both);
            await SharedDensityTransferTests.Grant(db, f.Actor.UserId!.Value, PermissionKeys.CanReturnProductionMaterials);
            var result = await ProductionFullReturn.Run(db, f.BatchId, "Return all issued stock");
            Assert.True(result.IsSuccess, result.Error.Description);
            var kinds = await db.MaterialReturnNoteFullReturns.IgnoreQueryFilters().Select(row => row.MaterialBatchReservedQuantity.MaterialBatch.Material.Kind).ToListAsync();
            Assert.Equal(both ? 3 : 2, kinds.Count);
            Assert.Equal(both, kinds.Contains(MaterialKind.Package));
            Assert.Contains(MaterialKind.Raw, kinds);
        }
    }

    [RepairPostgresFact]
    public async Task Consumption_step_evidence_and_consumed_allocations_block_full_return_without_writes()
    {
        await using var f = new IssuedMaterialRepairFixture(); await f.Seed(); await using var db = f.Open();
        await SharedDensityTransferTests.Grant(db, f.Actor.UserId!.Value, PermissionKeys.CanReturnProductionMaterials);
        var step = await db.ProductionActivitySteps.OrderByDescending(row => row.Order).FirstAsync();
        step.StartedAt = DateTime.UtcNow; step.Status = ProductionStatus.InProgress; await db.SaveChangesAsync();
        Assert.True((await ProductionFullReturn.Run(db, f.BatchId, "Too late")).IsFailure);
        step.StartedAt = null; step.Status = ProductionStatus.New;
        var allocation = await db.MaterialBatchReservedQuantities.FirstAsync(); allocation.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Assert.True((await ProductionFullReturn.Run(db, f.BatchId, "Consumed stock")).IsFailure);
        Assert.Empty(await db.MaterialReturnNotes.ToListAsync()); Assert.False((await db.ProductionScheduleProducts.SingleAsync()).Cancelled);
    }

    private static async Task<MaterialBatch> OtherKind(INFRASTRUCTURE.Context.ApplicationDbContext db, IssuedMaterialRepairFixture f, bool issue)
    {
        var kind = f.Kind == IssuedMaterialRepairKind.Packaging ? MaterialKind.Raw : MaterialKind.Package;
        var warehouse = await db.Warehouses.SingleAsync(row => row.Type == (kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage));
        var shelf = new WarehouseLocationShelf { WarehouseLocationRack = new() { WarehouseLocation = new() { WarehouseId = warehouse.Id } } };
        var lot = new MaterialBatch { Material = new() { Code = "OTHER", Name = "Other kind", Kind = kind }, UoMId = f.UnitId,
            TotalQuantity = 500, QuantityAssigned = 500, BatchNumber = "OTHER/LOT", Status = BatchStatus.Available, ExpiryDate = DateTime.UtcNow.AddYears(1) };
        var floor = await db.Warehouses.SingleAsync(row => row.Type == WarehouseType.Production);
        db.AddRange(shelf, lot, new MaterialBatchReservedQuantity { MaterialBatch = lot, WarehouseLocationShelf = shelf,
            WarehouseId = floor.Id, ProductionScheduleProductId = f.BatchId, UoMId = f.UnitId, Quantity = 5 });
        db.ShelfMaterialBatches.Add(new() { MaterialBatch = lot, WarehouseLocationShelf = shelf, UoMId = f.UnitId, Quantity = 500 });
        db.BinCardInformation.Add(new() { MaterialBatch = lot, WarehouseId = warehouse.Id, UoMId = f.UnitId, QuantityReceived = 500, BalanceQuantity = 500 });
        db.MaterialDepartments.Add(new() { Material = lot.Material, DepartmentId = f.Actor.DepartmentId!.Value, UoMId = f.UnitId });
        var request = new Requisition { ProductionScheduleProductId = f.BatchId, DepartmentId = f.Actor.DepartmentId.Value, Code = "OTHER/REQUEST",
            RequisitionType = RequisitionType.Stock, RequestedById = f.Actor.UserId!.Value, Approved = true, Status = RequestStatus.Pending,
            Items = [new() { Material = lot.Material, UoMId = f.UnitId, Quantity = 5, Status = RequestStatus.Pending }] };
        db.Requisitions.Add(request); await db.SaveChangesAsync();
        if (issue)
        {
            var repository = new RequisitionRepository(db, null!, null!, null!, null!, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());
            var result = await repository.IssueStockRequisition(request.Id, f.Actor.UserId.Value);
            Assert.True(result.IsSuccess, result.Error.Description);
        }
        return lot;
    }
}
