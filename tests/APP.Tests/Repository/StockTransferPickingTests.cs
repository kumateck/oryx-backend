using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public sealed class StockTransferPickingTests
{
    [RepairPostgresFact]
    public async Task Concurrent_issue_deducts_stock_once_and_keeps_the_recorded_batches()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(IssuedMaterialRepairKind.Bom, stock: 1_000, issueStock: false);
        using var mapping = new ScheduleStockFixture();
        await mapping.Seed();
        await using var db = fixture.Open();
        var lot = await db.MaterialBatches.OrderBy(row => row.Id).FirstAsync();
        var recipient = new Department { Name = "Transfer recipient", Type = DepartmentType.Production };
        var recipientWarehouse = new Warehouse { Department = recipient, Type = WarehouseType.RawMaterialStorage };
        var transfer = new StockTransferSource
        {
            StockTransfer = new StockTransfer { MaterialId = lot.MaterialId, UoMId = lot.UoMId },
            FromDepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(), ToDepartment = recipient,
            Status = StockTransferStatus.Approved, Quantity = 100
        };
        db.AddRange(recipientWarehouse, transfer);
        const string permission = PermissionKeys.CanIssueRawMaterialStockTransfers;
        db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = 901, RoleId = fixture.RoleId, ClaimType = AppConstants.Permission, ClaimValue = permission });
        db.PermissionTypes.Add(new PermissionType { RoleClaimId = 901, Key = permission, Type = "Action" });
        await db.SaveChangesAsync();

        ProductionScheduleRepository Repository(INFRASTRUCTURE.Context.ApplicationDbContext context) =>
            new(context, mapping.Mapper, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());

        async Task<Result> Issue()
        {
            await using var requestDb = fixture.Open();
            return await Repository(requestDb).IssueStockTransfer(transfer.Id,
                [new() { BatchId = lot.Id, Quantity = 100 }], fixture.Actor.UserId.GetValueOrDefault());
        }

        var results = await Task.WhenAll(Issue(), Issue());
        Assert.Single(results, result => result.IsSuccess);
        Assert.Equal("StockTransfer.Status", Assert.Single(results, result => result.IsFailure).Error.Code);
        var remaining = await db.ShelfMaterialBatches.AsNoTracking().SingleAsync(row => row.MaterialBatchId == lot.Id);
        Assert.Equal(900, remaining.Quantity);
        var holding = await db.HoldingMaterialTransfers.AsNoTracking().SingleAsync(row => row.StockTransferSourceId == transfer.Id);
        var history = await Repository(db).BatchesToSupplyForStockTransfer(transfer.Id);
        Assert.Equal(100, Assert.Single(history.Value).QuantityToTake);

        await db.HoldingMaterialTransfers.Where(row => row.Id == holding.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.StockTransferSourceId, (Guid?)null));
        var legacyHistory = await Repository(db).BatchesToSupplyForStockTransfer(transfer.Id);
        Assert.Equal(lot.Id, Assert.Single(legacyHistory.Value).Batch.Id);
    }

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
            Status = StockTransferStatus.InProgress, Quantity = 90
        };
        db.StockTransferSources.Add(transfer);
        await db.SaveChangesAsync();

        var preview = await fixture.Repository.BatchesToSupplyForStockTransfer(transfer.Id);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        var picked = Assert.Single(preview.Value);
        Assert.Equal(available.Id, picked.Batch.Id);
        Assert.False(picked.Batch.Expired);
        Assert.Equal(90, picked.QuantityToTake);

        var denied = await fixture.Repository.ApproveStockTransfer(transfer.Id, fixture.Actor.Id);
        Assert.Equal("StockTransfer.Permission", denied.Error.Code);

        var role = new Role { Name = "Raw issue fixture" };
        var otherActor = new User { Id = Guid.NewGuid(), DepartmentId = fixture.OtherDepartment.Id, UserName = "other-department" };
        db.Roles.Add(role);
        db.Users.Add(otherActor);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = fixture.Actor.Id, RoleId = role.Id });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = otherActor.Id, RoleId = role.Id });
        var keys = new[] { PermissionKeys.CanIssueRawMaterialStockTransfers,
            PermissionKeys.CanApproveIncomingStockTransferRequest, PermissionKeys.CanRejectIncomingStockTransferRequest };
        foreach (var (key, index) in keys.Select((key, index) => (key, index)))
        {
            var claimId = 999 + index;
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = claimId, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key });
            db.PermissionTypes.Add(new PermissionType { RoleClaimId = claimId, Key = key, Type = "Action" });
        }
        available.RetestDate = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
        var wrongDepartment = await fixture.Repository.RejectStockTransfer(transfer.Id, otherActor.Id);
        Assert.Equal("StockTransfer.Department", wrongDepartment.Error.Code);
        var approval = await fixture.Repository.ApproveStockTransfer(transfer.Id, fixture.Actor.Id);
        Assert.True(approval.IsSuccess, approval.Error.Description);
        var deniedIssue = await fixture.Repository.IssueStockTransfer(transfer.Id,
            [new() { BatchId = available.Id, Quantity = 90 }], fixture.Creator.Id);
        Assert.Equal("StockTransfer.Permission", deniedIssue.Error.Code);

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
        Assert.Equal(transfer.Id, holding.StockTransferSourceId);
        Assert.Equal(90, Assert.Single(holding.Batches).Quantity);

        var repeatApproval = await fixture.Repository.ApproveStockTransfer(transfer.Id, fixture.Actor.Id);
        var lateRejection = await fixture.Repository.RejectStockTransfer(transfer.Id, fixture.Actor.Id);
        Assert.Equal("StockTransfer.Status", repeatApproval.Error.Code);
        Assert.Equal("StockTransfer.Status", lateRejection.Error.Code);

        // Simulate a legacy record reopened by the old approval endpoint.
        transfer.Status = StockTransferStatus.Approved;
        holding.Status = HoldingMaterialTransferStatus.Transferred;
        available.Status = BatchStatus.Frozen;
        await db.SaveChangesAsync();
        var repeatedIssue = await fixture.Repository.IssueStockTransfer(transfer.Id,
            [new() { BatchId = available.Id, Quantity = 90 }], fixture.Actor.Id);
        Assert.Equal("StockTransfer.Status", repeatedIssue.Error.Code);
        Assert.Equal(10, fixture.Shelf.Quantity);
        var issuedBatches = await fixture.Repository.BatchesToSupplyForStockTransfer(transfer.Id);
        Assert.Equal(90, Assert.Single(issuedBatches.Value).QuantityToTake);
        Assert.Equal(available.Id, Assert.Single(issuedBatches.Value).Batch.Id);

        holding.StockTransferSourceId = null;
        await db.SaveChangesAsync();
        var legacyBatches = await fixture.Repository.BatchesToSupplyForStockTransfer(transfer.Id);
        Assert.Equal(available.Id, Assert.Single(legacyBatches.Value).Batch.Id);

        var duplicateSource = new StockTransferSource
        {
            StockTransferId = transfer.StockTransferId, FromDepartmentId = transfer.FromDepartmentId,
            ToDepartmentId = transfer.ToDepartmentId
        };
        db.StockTransferSources.Add(duplicateSource);
        await db.SaveChangesAsync();
        var rejected = await fixture.Repository.RejectStockTransfer(duplicateSource.Id, fixture.Actor.Id);
        Assert.True(rejected.IsSuccess, rejected.Error.Description);
        var rejectedApproval = await fixture.Repository.ApproveStockTransfer(duplicateSource.Id, fixture.Actor.Id);
        Assert.Equal("StockTransfer.Status", rejectedApproval.Error.Code);
        var ambiguousBatches = await fixture.Repository.BatchesToSupplyForStockTransfer(transfer.Id);
        Assert.Empty(ambiguousBatches.Value);
    }
}
