using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public sealed class StockTransferPickingTests
{
    [RepairPostgresFact]
    public async Task Picker_translates_effective_expiry_and_reservations_in_Postgres()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(IssuedMaterialRepairKind.Bom, issueStock: false, validRetest: true);
        await using var db = fixture.Open();
        var valid = await db.ShelfMaterialBatches.Include(row => row.MaterialBatch)
            .OrderBy(row => row.Id).FirstAsync();
        var old = new MaterialBatch
        {
            MaterialId = valid.MaterialBatch.MaterialId, UoMId = valid.MaterialBatch.UoMId,
            Status = BatchStatus.Available, ExpiryDate = DateTime.UtcNow.AddDays(-60),
            TotalQuantity = 1_000
        };
        db.ShelfMaterialBatches.Add(new ShelfMaterialBatch
        {
            MaterialBatch = old, WarehouseLocationShelfId = valid.WarehouseLocationShelfId,
            UoMId = valid.UoMId, Quantity = 1_000
        });
        await db.SaveChangesAsync();

        var held = await db.MaterialBatchReservedQuantities.Where(row => row.MaterialBatchId == valid.MaterialBatchId)
            .SumAsync(row => row.Quantity);
        var picks = await StockTransferPicking.Load(db, valid.MaterialBatch.MaterialId, fixture.SourceId, valid.UoMId);
        var picked = Assert.Single(picks);
        Assert.Equal(valid.Id, picked.Shelf.Id);
        Assert.Equal(valid.Quantity - held, picked.Quantity);
    }

    [Fact]
    public async Task Transfer_uses_effectively_valid_stock_and_rechecks_it_before_deducting()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var available = fixture.Lot;
        available.Status = BatchStatus.Available;
        available.QuantityAssigned = 100;
        available.ExpiryDate = DateTime.UtcNow.AddDays(-10);
        available.RetestDate = DateTime.UtcNow.AddDays(20);
        fixture.Shelf.Quantity = 100;
        var excluded = new[] {
            (Status: BatchStatus.Available, Retest: (DateTime?)null, Deleted: false),
            (Status: BatchStatus.Frozen, Retest: (DateTime?)DateTime.UtcNow.AddDays(20), Deleted: false),
            (Status: BatchStatus.Available, Retest: (DateTime?)DateTime.UtcNow.AddDays(20), Deleted: true)
        };
        foreach (var lot in excluded)
        {
            db.ShelfMaterialBatches.Add(new ShelfMaterialBatch
            {
                WarehouseLocationShelfId = fixture.Shelf.WarehouseLocationShelfId,
                MaterialBatch = new MaterialBatch
                {
                    MaterialId = available.MaterialId, UoMId = available.UoMId, TotalQuantity = 100,
                    ExpiryDate = available.ExpiryDate, RetestDate = lot.Retest, Status = lot.Status
                },
                Quantity = 100, UoMId = available.UoMId, DeletedAt = lot.Deleted ? DateTime.UtcNow : null
            });
        }
        db.Warehouses.Add(new Warehouse { DepartmentId = fixture.OtherDepartment.Id, Type = WarehouseType.RawMaterialStorage });
        db.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity
        {
            MaterialBatchId = available.Id, UoMId = available.UoMId, WarehouseId = fixture.Production.Id,
            WarehouseLocationShelfId = fixture.Shelf.WarehouseLocationShelfId,
            ProductionScheduleProductId = fixture.Batch.Id, Quantity = 10
        });
        var transfer = new StockTransferSource
        {
            StockTransfer = new StockTransfer { MaterialId = available.MaterialId, UoMId = available.UoMId },
            FromDepartmentId = fixture.Department.Id, ToDepartmentId = fixture.OtherDepartment.Id,
            Status = StockTransferStatus.Approved, Quantity = 90
        };
        db.StockTransferSources.Add(transfer);
        await db.SaveChangesAsync();

        var preview = await fixture.Repository.BatchesToSupplyForStockTransfer(transfer.Id);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var picked = Assert.Single(preview.Value);
        Assert.Equal(available.Id, picked.Batch.Id);
        Assert.False(picked.Batch.Expired);
        Assert.Equal(90, picked.QuantityToTake);

        var denied = await fixture.Repository.IssueStockTransfer(transfer.Id,
            [new() { BatchId = available.Id, Quantity = 90 }], fixture.Actor.Id);
        Assert.Equal("StockTransfer.Permission", denied.Error.Code);

        var role = new Role { Name = "Raw issue fixture" };
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = fixture.Actor.Id, RoleId = role.Id });
        const string key = PermissionKeys.CanIssueRawMaterialStockTransfers;
        db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = 999, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key });
        db.PermissionTypes.Add(new PermissionType { RoleClaimId = 999, Key = key, Type = "Action" });
        available.RetestDate = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var stale = await fixture.Repository.IssueStockTransfer(transfer.Id,
            [new() { BatchId = available.Id, Quantity = 90 }], fixture.Actor.Id);
        Assert.Equal("StockTransfer.StockChanged", stale.Error.Code);
        Assert.Equal(100, fixture.Shelf.Quantity);
        Assert.Empty(db.HoldingMaterialTransfers);

        available.RetestDate = DateTime.UtcNow.AddDays(20);
        await db.SaveChangesAsync();
        var issued = await fixture.Repository.IssueStockTransfer(transfer.Id,
            [new() { BatchId = available.Id, Quantity = 90 }], fixture.Actor.Id);
        Assert.True(issued.IsSuccess, issued.Error.Description);
        Assert.Equal(StockTransferStatus.Issued, transfer.Status);
        Assert.Equal(10, fixture.Shelf.Quantity);
        Assert.Equal(10, available.QuantityAssigned);
        var holding = await db.HoldingMaterialTransfers.Include(row => row.Batches).SingleAsync();
        Assert.Equal(90, Assert.Single(holding.Batches).Quantity);
    }
}
