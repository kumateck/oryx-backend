using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace APP.Tests.Repository;

public sealed class StockTransferReceiptTests
{
    [RepairPostgresFact]
    public async Task Receipt_grant_migration_preserves_existing_grants_and_copies_issue_access_types_once()
    {
        await using var f = new IssuedMaterialRepairFixture();
        await f.Seed(issueStock: false);
        await using var db = f.Open();
        await SharedDensityTransferTests.Grant(db, f.Actor.UserId!.Value,
            PermissionKeys.CanIssueRawMaterialStockTransfers, PermissionKeys.CanIssuePackagingMaterialStockTransfers,
            PermissionKeys.CanAssignRawMaterialStockTransfers);
        await db.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('roleclaims', 'Id'), (SELECT MAX(\"Id\") FROM \"roleclaims\"))");
        var before = await db.RoleClaims.AsNoTracking().Where(row => row.ClaimValue == PermissionKeys.CanAssignRawMaterialStockTransfers).SingleAsync();
        var migration = new GrantStockTransferReceiptAssignments();
        var generator = db.GetService<IMigrationsSqlGenerator>();
        for (var run = 0; run < 2; run++)
            foreach (var command in generator.Generate(migration.UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
        Assert.Equal(before.Id, (await db.RoleClaims.AsNoTracking().SingleAsync(row => row.ClaimValue == PermissionKeys.CanAssignRawMaterialStockTransfers)).Id);
        foreach (var (key, from) in GrantStockTransferReceiptAssignments.Grants)
        {
            Assert.Single(PermissionUtils.GeneratePermissions(), row => row.Key == key);
            var source = await db.RoleClaims.AsNoTracking().SingleAsync(row => row.ClaimValue == from);
            var target = await db.RoleClaims.AsNoTracking().SingleAsync(row => row.ClaimValue == key);
            Assert.Equal(source.RoleId, target.RoleId);
            Assert.Equal(await db.PermissionTypes.Where(row => row.RoleClaimId == source.Id).Select(row => row.Type).OrderBy(type => type).ToListAsync(),
                await db.PermissionTypes.Where(row => row.RoleClaimId == target.Id).Select(row => row.Type).OrderBy(type => type).ToListAsync());
        }
    }

    [Fact]
    public void Receipt_endpoint_requires_the_transfer_assignment_key_for_the_stored_material_kind()
    {
        var action = typeof(MaterialController).GetMethod(nameof(MaterialController.MoveMaterialBatchToWarehouseFromHolding))!;
        Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());
        var permission = Assert.Single(action.GetCustomAttributes<MaterialKindPermissionAttribute>());
        Assert.Equal(PermissionKeys.CanAssignRawMaterialStockTransfers, permission.RawKey);
        Assert.Equal(PermissionKeys.CanAssignPackagingMaterialStockTransfers, permission.PackagingKey);
        foreach (var key in new[] { permission.RawKey, permission.PackagingKey })
            Assert.Single(PermissionUtils.GeneratePermissions(), row => row.Key == key);
    }

    [Theory]
    [InlineData(MaterialKind.Raw)]
    [InlineData(MaterialKind.Package)]
    public async Task Issued_stock_is_visible_to_its_recipient_and_shelved_once_with_its_assignment_key(MaterialKind kind)
    {
        using var f = new ScheduleStockFixture();
        await f.Seed();
        var db = f.Harness.Db;
        f.Lot.Material.Kind = kind;
        f.Lot.QuantityAssigned = f.Shelf.Quantity;
        db.BinCardInformation.Add(new() { MaterialBatchId = f.Lot.Id, WarehouseId = f.Storage.Id, UoMId = f.Lot.UoMId,
            QuantityReceived = f.Shelf.Quantity, BalanceQuantity = f.Shelf.Quantity, ArNumber = "AR-TRANSFER" });
        var raw = kind == MaterialKind.Raw;
        var warehouseType = raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage;
        f.Storage.Type = warehouseType;
        // Keep exactly one source store of the chosen kind in this fixture.
        foreach (var warehouse in await db.Warehouses.Where(row => row.DepartmentId == f.Department.Id && row.Id != f.Storage.Id).ToListAsync())
            warehouse.Type = WarehouseType.Production;
        var recipient = new User { UserName = "transfer-recipient", DepartmentId = f.OtherDepartment.Id };
        var receiving = new Warehouse { Name = "Receiving store", DepartmentId = f.OtherDepartment.Id, Type = warehouseType };
        var shelf = new WarehouseLocationShelf { WarehouseLocationRack = new()
            { WarehouseLocation = new() { Warehouse = receiving } } };
        var transfer = new StockTransferSource
        {
            StockTransfer = new() { MaterialId = f.Lot.MaterialId, UoMId = f.Lot.UoMId },
            FromDepartmentId = f.Department.Id, ToDepartmentId = f.OtherDepartment.Id,
            Quantity = f.Shelf.Quantity, Status = StockTransferStatus.Approved,
        };
        db.AddRange(recipient, shelf, transfer);
        await db.SaveChangesAsync();
        var issueKey = raw ? PermissionKeys.CanIssueRawMaterialStockTransfers : PermissionKeys.CanIssuePackagingMaterialStockTransfers;
        var assignmentKey = raw ? PermissionKeys.CanAssignRawMaterialStockTransfers : PermissionKeys.CanAssignPackagingMaterialStockTransfers;
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, issueKey, assignmentKey);
        var issued = await f.Repository.IssueStockTransfer(transfer.Id, [new() { BatchId = f.Lot.Id, Quantity = transfer.Quantity }], f.Actor.Id);
        Assert.True(issued.IsSuccess, issued.Error.Description);
        var issueCard = await db.BinCardInformation.SingleAsync(card => card.QuantityIssued > 0);
        Assert.Equal(transfer.Quantity, issueCard.QuantityIssued);
        Assert.Equal(0, issueCard.BalanceQuantity);
        Assert.Equal(f.Storage.Id, issueCard.WarehouseId);

        // The source shelf was exhausted and removed; the held lot must still appear for the receiving department.
        db.ChangeTracker.Clear();
        var repository = new MaterialRepository(db, f.Mapper);
        var queue = await repository.GetHoldingMaterialTransfers(1, 10, null!, false, f.OtherDepartment.Id, kind);
        Assert.True(queue.IsSuccess, queue.Error.Description);
        var held = Assert.Single(queue.Value.Data);
        Assert.Equal(f.Lot.Id, Assert.Single(held.Batches).MaterialBatch.Id);
        Assert.Empty((await repository.GetHoldingMaterialTransfers(1, 10, null!, false, f.Department.Id, kind)).Value.Data);
        var request = new SupplyMaterialBatchFromHoldingRequest
        {
            HoldingMaterialId = held.Id, MaterialBatchId = f.Lot.Id,
            ShelfMaterialBatches = [new() { WarehouseLocationShelfId = shelf.Id, Quantity = transfer.Quantity, UomId = f.Lot.UoMId }],
        };
        Assert.Equal("HoldingMaterial.Permission", (await repository.MoveMaterialBatchToWarehouseFromHolding(request, recipient.Id)).Error.Code);
        await SharedDensityTransferTests.Grant(db, recipient.Id, issueKey,
            raw ? PermissionKeys.CanAssignRawMaterialsStockToShelves : PermissionKeys.CanAssignPackagingMaterialsStockToShelves,
            raw ? PermissionKeys.CanAssignPackagingMaterialStockTransfers : PermissionKeys.CanAssignRawMaterialStockTransfers);
        Assert.Equal("HoldingMaterial.Permission", (await repository.MoveMaterialBatchToWarehouseFromHolding(request, recipient.Id)).Error.Code);
        Assert.Equal("HoldingMaterial.Department", (await repository.MoveMaterialBatchToWarehouseFromHolding(request, f.Actor.Id)).Error.Code);
        await SharedDensityTransferTests.Grant(db, recipient.Id, assignmentKey);
        // Revoking supplier issue must not prevent receipt.
        var issueClaim = await db.RoleClaims.SingleAsync(row => row.ClaimValue == issueKey && db.UserRoles.Any(link => link.RoleId == row.RoleId && link.UserId == recipient.Id));
        db.RoleClaims.Remove(issueClaim);
        await db.SaveChangesAsync();
        var received = await repository.MoveMaterialBatchToWarehouseFromHolding(request, recipient.Id);
        Assert.True(received.IsSuccess, received.Error.Description);
        Assert.Equal(transfer.Quantity, (await db.ShelfMaterialBatches.SingleAsync(row => row.WarehouseLocationShelfId == shelf.Id)).Quantity);
        var receiptCard = await db.BinCardInformation.SingleAsync(card => card.WarehouseId == receiving.Id);
        Assert.Equal(transfer.Quantity, receiptCard.QuantityReceived);
        Assert.Equal(transfer.Quantity, receiptCard.BalanceQuantity);
        Assert.Equal("AR-TRANSFER", receiptCard.ArNumber);
        var saved = await db.HoldingMaterialTransfers.Include(row => row.Batches).SingleAsync(row => row.Id == held.Id);
        Assert.Equal(HoldingMaterialTransferStatus.Transferred, saved.Status);
        Assert.Equal(recipient.Id, Assert.Single(saved.Batches).ReceivedById);
        Assert.Empty((await repository.GetHoldingMaterialTransfers(1, 10, null!, false, f.OtherDepartment.Id, kind)).Value.Data);
        Assert.Equal("HoldingMaterial.Received", (await repository.MoveMaterialBatchToWarehouseFromHolding(request, recipient.Id)).Error.Code);
        Assert.Single(await db.MassMaterialBatchMovements.ToListAsync());
        Assert.Equal(3, await db.BinCardInformation.CountAsync());
    }
}
