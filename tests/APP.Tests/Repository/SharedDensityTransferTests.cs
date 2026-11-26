using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public sealed class SharedDensityTransferTests
{
    internal static async Task Grant(INFRASTRUCTURE.Context.ApplicationDbContext db, Guid actor, params string[] keys)
    {
        var role = new Role { Name = Guid.NewGuid().ToString() }; db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = actor, RoleId = role.Id });
        var id = await db.RoleClaims.Select(row => (int?)row.Id).MaxAsync() ?? 0;
        foreach (var key in keys)
        {
            id++;
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = id, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key });
            db.PermissionTypes.Add(new PermissionType { RoleClaimId = id, Key = key, Type = "Action" });
        }
        await db.SaveChangesAsync();
    }

    [Theory]
    [InlineData(DOMAIN.Entities.Materials.MaterialKind.Raw)]
    [InlineData(DOMAIN.Entities.Materials.MaterialKind.Package)]
    public async Task Shared_density_update_requires_its_own_key_and_is_audited(DOMAIN.Entities.Materials.MaterialKind kind)
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.Material.Kind = kind;
        var raw = kind == DOMAIN.Entities.Materials.MaterialKind.Raw;
        var unit = new UnitOfMeasure { Name = "Density", Symbol = "g/mL" }; db.UnitOfMeasures.Add(unit); await db.SaveChangesAsync();
        await Grant(db, f.Actor.Id, raw ? PermissionKeys.CanLinkRawMaterials : PermissionKeys.CanLinkPackagingMaterials);
        var repository = new MaterialRepository(db, f.Mapper);
        var request = new List<DOMAIN.Entities.Materials.CreateMaterialDepartment> { new() { MaterialId = f.Lot.MaterialId,
            UoMId = f.Lot.UoMId, Density = 0.8m, DensityUoMId = unit.Id } };
        var denied = await repository.CreateMaterialDepartment(request, f.Actor.Id);
        Assert.Equal("MaterialDepartment.DensityPermission", denied.Error.Code);
        Assert.Null(f.Lot.Material.DensityGramsPerMilliliter); Assert.Empty(await db.MaterialDensityMeasurements.ToListAsync());
        await Grant(db, f.Actor.Id, raw ? PermissionKeys.CanEditRawMaterialDensity : PermissionKeys.CanEditPackagingMaterialDensity);
        var result = await repository.CreateMaterialDepartment(request, f.Actor.Id);
        Assert.True(result.IsSuccess, result.Error.Description); Assert.Equal(0.8m, f.Lot.Material.DensityGramsPerMilliliter);
        Assert.Single(await db.MaterialDensityMeasurements.ToListAsync());
        var otherLink = new DOMAIN.Entities.Materials.MaterialDepartment { Material = f.Lot.Material, Density = 1.5m,
            DensityUoMId = unit.Id, DensityUoM = unit, UoM = new UnitOfMeasure { Symbol = "l" } };
        Assert.Equal(1m, MaterialDensity.ConvertBomMass(800000, "mg", otherLink));
    }

    [Fact]
    public async Task Production_reserves_physical_litres_and_validates_issue_in_adopted_kilograms()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.UoM.Symbol = "l"; f.Lot.Material.DensityGramsPerMilliliter = 0.8m;
        f.Shelf.Quantity = 100;
        var kilogram = new UnitOfMeasure { Name = "Kilogram", Symbol = "kg" }; db.UnitOfMeasures.Add(kilogram); await db.SaveChangesAsync();
        var requirement = f.Requirement() with { UnitId = kilogram.Id, Unit = "kg", Quantity = 20, DensityGramsPerMilliliter = 0.8m };
        var source = new ProductionMaterialSource(f.Batch, f.Department.Id, f.Storage.Id, f.Production.Id, [requirement], "source", "{}", []);
        var plan = await ProductionMaterialAllocator.Plan(db, source, []);
        Assert.Empty(plan.Blockers); Assert.Equal(0, plan.Shortages[requirement.MaterialId]);
        Assert.Equal(25, Assert.Single(plan.Allocations).Quantity);
        ProductionMaterialAllocator.Apply(db, source, [], plan.Allocations); await db.SaveChangesAsync();
        var reservation = await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().Include(row => row.UoM).Include(row => row.MaterialBatch).SingleAsync();
        Assert.Equal(f.Lot.UoMId, reservation.UoMId); Assert.Equal(0.8m, reservation.DensityGramsPerMilliliter);
        f.Lot.Material.DensityGramsPerMilliliter = 1.2m; await db.SaveChangesAsync();
        var request = new DOMAIN.Entities.Requisitions.Requisition { Items = [new() { MaterialId = f.Lot.MaterialId, UoMId = kilogram.Id, UoM = kilogram, Quantity = 20 }] };
        Assert.True(StockIssueQuantityValidator.Validate(request, [reservation]).IsSuccess);
    }

    [RepairPostgresFact]
    public async Task Migration_backfills_only_verified_density_and_protects_measurement_history()
    {
        await using var f = new IssuedMaterialRepairFixture();
        await f.Seed(DOMAIN.Entities.Products.Production.IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = f.Open();
        var densityUnit = new UnitOfMeasure { Symbol = "g/mL", Name = "Verified density" };
        var link = await db.MaterialDepartments.FirstAsync(); link.Density = 0.8m; link.DensityUoM = densityUnit;
        await db.SaveChangesAsync();
        await Grant(db, f.Actor.UserId!.Value, PermissionKeys.CanLinkRawMaterials);
        await db.Database.ExecuteSqlRawAsync("""
            DROP TABLE "MaterialDensityMeasurements";
            ALTER TABLE "Materials" DROP COLUMN "DensityGramsPerMilliliter" CASCADE;
            ALTER TABLE "MaterialBatchReservedQuantities" DROP COLUMN "DensityGramsPerMilliliter";
            ALTER TABLE "HoldingMaterialTransferBatches" DROP COLUMN "DestinationQuantity", DROP COLUMN "DestinationUoMId" CASCADE;
            ALTER TABLE "HoldingMaterialTransferBatches" DROP COLUMN "DensityGramsPerMilliliter", DROP COLUMN "TransferQuantity", DROP COLUMN "ReceivedAt", DROP COLUMN "ReceivedById";
            """);
        // Fixture claims use explicit IDs; restore the normal identity sequence before exercising deployment SQL.
        await db.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('roleclaims', 'Id'), (SELECT MAX(\"Id\") FROM \"roleclaims\"))");
        var migration = new INFRASTRUCTURE.Migrations.AddSharedMaterialDensity();
        var generator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator>(db);
        foreach (var command in generator.Generate(migration.UpOperations, db.Model)) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        // The ORM uses the current model; upgrade the audit columns after
        // checking the original migration's backfill and history triggers.
        var clearMigration = new INFRASTRUCTURE.Migrations.AllowClearingMaterialDensity();
        foreach (var command in generator.Generate(clearMigration.UpOperations, db.Model)) await db.Database.ExecuteSqlRawAsync(command.CommandText);
        db.ChangeTracker.Clear();
        Assert.Equal(0.8m, await db.Materials.Where(m => m.Id == link.MaterialId).Select(m => m.DensityGramsPerMilliliter).SingleAsync());
        Assert.Contains(PermissionKeys.CanEditRawMaterialDensity, await APP.Services.Authorization.CurrentPermissionSet.Load(db, f.Actor.UserId));
        db.MaterialDensityMeasurements.Add(new() { Id = Guid.NewGuid(), ActorId = f.Actor.UserId.Value, MaterialId = link.MaterialId, RecordedAt = DateTime.UtcNow, Density = 0.9m });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync("UPDATE \"MaterialDensityMeasurements\" SET \"Density\" = 1"));
    }

    [Theory]
    [InlineData("l", "kg", 0.8, 20, 25)]
    [InlineData("kg", "l", 1.2, 20, 24)]
    public async Task Issue_and_receive_preserve_physical_units_and_adopted_conversion(string sourceUnit, string destinationUnit, decimal density, decimal requested, decimal physical)
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.UoM.Symbol = sourceUnit; f.Lot.Material.DensityGramsPerMilliliter = density;
        f.Shelf.Quantity = 100; f.Lot.TotalQuantity = 100; f.Lot.QuantityAssigned = 100;
        var unit = new UnitOfMeasure { Symbol = destinationUnit, Name = destinationUnit };
        var actor = new User { Id = Guid.NewGuid(), UserName = "density-recipient", DepartmentId = f.OtherDepartment.Id };
        var warehouse = new Warehouse { Id = Guid.NewGuid(), DepartmentId = f.OtherDepartment.Id, Type = WarehouseType.RawMaterialStorage };
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), WarehouseLocationRack = new() { Id = Guid.NewGuid(), WarehouseLocation = new() { Id = Guid.NewGuid(), Warehouse = warehouse } } };
        var transfer = new StockTransferSource { Id = Guid.NewGuid(), StockTransfer = new() { MaterialId = f.Lot.MaterialId, UoM = unit },
            FromDepartmentId = f.Department.Id, ToDepartmentId = f.OtherDepartment.Id, Quantity = requested, Status = StockTransferStatus.Approved };
        db.AddRange(actor, shelf, transfer);
        db.MaterialDepartments.Add(new() { MaterialId = f.Lot.MaterialId, DepartmentId = f.OtherDepartment.Id, UoM = unit });
        await db.SaveChangesAsync();
        await Grant(db, f.Actor.Id, PermissionKeys.CanIssueRawMaterialStockTransfers);
        await Grant(db, actor.Id, PermissionKeys.CanAssignRawMaterialStockTransfers);
        var preview = await f.Repository.BatchesToSupplyForStockTransfer(transfer.Id);
        Assert.True(preview.IsSuccess, preview.Error.Description); Assert.Equal(physical, Assert.Single(preview.Value).SourceQuantityToTake);
        var result = await f.Repository.IssueStockTransfer(transfer.Id, [new() { BatchId = f.Lot.Id, Quantity = requested }], f.Actor.Id);
        Assert.True(result.IsSuccess, result.Error.Description); Assert.Equal(100 - physical, f.Shelf.Quantity);
        var holding = await db.HoldingMaterialTransfers.Include(row => row.Batches).SingleAsync();
        var held = Assert.Single(holding.Batches); Assert.Equal(physical, held.Quantity); Assert.Equal(requested, held.DestinationQuantity);
        Assert.Equal(density, held.DensityGramsPerMilliliter);
        var repository = new MaterialRepository(db, f.Mapper);
        var request = new SupplyMaterialBatchFromHoldingRequest { MaterialBatchId = f.Lot.Id, HoldingMaterialId = holding.Id,
            ShelfMaterialBatches = [new() { WarehouseLocationShelfId = shelf.Id, Quantity = requested, UomId = unit.Id }] };
        var received = await repository.MoveMaterialBatchToWarehouseFromHolding(request, actor.Id);
        Assert.True(received.IsSuccess, received.Error.Description);
        var placed = await db.ShelfMaterialBatches.IgnoreQueryFilters().SingleAsync(row => row.WarehouseLocationShelfId == shelf.Id);
        Assert.Equal(physical, placed.Quantity); Assert.Equal(f.Lot.UoMId, placed.UoMId); Assert.Equal(100, f.Lot.QuantityAssigned);
        var cards = await db.BinCardInformation.ToListAsync();
        Assert.Equal(2, cards.Count);
        Assert.All(cards, card => Assert.Equal(f.Lot.UoMId, card.UoMId));
        Assert.Equal(physical, cards.Sum(card => card.QuantityIssued));
        Assert.Equal(physical, cards.Sum(card => card.QuantityReceived));
        var stock = await repository.GetShelfMaterialStockInWarehouse(f.Lot.MaterialId, warehouse.Id);
        Assert.True(stock.IsSuccess, stock.Error.Description); Assert.Equal(requested, stock.Value);
        Assert.True((await repository.MoveMaterialBatchToWarehouseFromHolding(request, actor.Id)).IsFailure);
        Assert.Equal(physical, placed.Quantity);
    }

    [Fact]
    public async Task Supplying_store_reads_the_transfer_and_its_shortfall_in_its_own_unit()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.UoM.Symbol = "kg"; f.Lot.Material.DensityGramsPerMilliliter = 1.036m;
        f.Shelf.Quantity = 100; f.Lot.TotalQuantity = 100; f.Lot.QuantityAssigned = 100;
        var millilitre = new UnitOfMeasure { Symbol = "mL", Name = "Millilitre" };
        var transfer = new StockTransferSource { Id = Guid.NewGuid(), StockTransfer = new() { MaterialId = f.Lot.MaterialId, UoM = millilitre },
            FromDepartmentId = f.Department.Id, ToDepartmentId = f.OtherDepartment.Id, Quantity = 200_000, Status = StockTransferStatus.Approved };
        db.Add(transfer); await db.SaveChangesAsync();

        var read = await f.Repository.GetStockTransferSource(transfer.Id);
        Assert.True(read.IsSuccess); Assert.Equal(207.2m, read.Value.SupplierQuantity); Assert.Equal("kg", read.Value.SupplierUoM.Symbol);
        Assert.Equal(200_000, read.Value.Quantity); Assert.Equal("mL", read.Value.UoM.Symbol);
        var list = await f.Repository.GetIncomingStockTransferRequestForUserDepartment(f.Actor.Id, 1, 10, status: StockTransferStatus.Approved);
        Assert.True(list.IsSuccess); Assert.Equal(207.2m, Assert.Single(list.Value.Data).SupplierQuantity);

        var preview = await f.Repository.BatchesToSupplyForStockTransfer(transfer.Id);
        Assert.Equal("StockTransfer.InsufficientStock", preview.Error.Code);
        Assert.Equal("The supplying store has 100 kg free of the 207.2 kg needed for 200 L. It is short by 107.2 kg. "
            + "200 L is 207.2 kg at the material's verified density of 1.036 g/mL. "
            + "Free stock is approved, in-date stock on the store's shelves that is not reserved for a production batch.", preview.Error.Description);
    }

    [Theory]
    [InlineData(20_000_000, "mL", "20,000 L")]
    [InlineData(999, "mL", "999 mL")]
    [InlineData(2_500_000, "mg", "2.5 kg")]
    [InlineData(2_500, "mg", "2.5 g")]
    [InlineData(1_234.5678, "kg", "1,234.568 kg")]
    public void Quantities_read_in_the_largest_whole_unit(decimal quantity, string unit, string expected) =>
        Assert.Equal(expected, StockTransferSupplyUnits.Readable(quantity, unit));

    [Fact]
    public async Task Approved_materials_count_production_holds_taken_from_the_store()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Shelf.Quantity = 100;
        // The allocator records the production warehouse the stock goes to and the store shelf it is taken from.
        db.MaterialBatchReservedQuantities.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = f.Lot.Id, WarehouseId = f.Production.Id,
            WarehouseLocationShelfId = f.Shelf.WarehouseLocationShelfId, ProductionScheduleProductId = f.Batch.Id, UoMId = f.Lot.UoMId, Quantity = 30 });
        // A released hold no longer counts.
        db.MaterialBatchReservedQuantities.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = f.Lot.Id, WarehouseId = f.Production.Id, ReleasedAt = DateTime.UtcNow,
            WarehouseLocationShelfId = f.Shelf.WarehouseLocationShelfId, ProductionScheduleProductId = f.Batch.Id, UoMId = f.Lot.UoMId, Quantity = 50 });
        await db.SaveChangesAsync();

        var result = await new MaterialRepository(db, f.Mapper).GetApprovedMaterials(1, 10, null, DOMAIN.Entities.Materials.MaterialKind.Raw, f.Actor.Id);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        var row = Assert.Single(result.Value.Data);
        Assert.Equal(100, row.TotalAvailableQuantity); Assert.Equal(30, row.ReservedQuantity);
    }

    [Fact]
    public async Task Store_stock_counts_live_holds_only_and_never_more_than_the_shelf()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.UoM.Symbol = "kg"; f.Lot.ExpiryDate = DateTime.UtcNow.AddYears(1); f.Shelf.Quantity = 100; f.Shelf.UoMId = f.Lot.UoMId;
        MaterialBatchReservedQuantity Hold(decimal quantity, Guid batch, bool shelf = true, DateTime? released = null) => new()
        {
            Id = Guid.NewGuid(), MaterialBatchId = f.Lot.Id, WarehouseId = f.Production.Id, UoMId = f.Lot.UoMId, Quantity = quantity,
            ProductionScheduleProductId = batch, ReleasedAt = released, WarehouseLocationShelfId = shelf ? f.Shelf.WarehouseLocationShelfId : null,
        };
        var cancelled = new DOMAIN.Entities.ProductionSchedules.ProductionScheduleProduct { Id = Guid.NewGuid(), Cancelled = true, ProductId = f.Batch.ProductId, ProductionScheduleId = f.Batch.ProductionScheduleId };
        db.Add(cancelled);
        db.MaterialBatchReservedQuantities.AddRange(
            Hold(30, f.Batch.Id),                                 // live, on the shelf
            Hold(50, f.Batch.Id, released: DateTime.UtcNow),      // released by Unstart: not reserved
            Hold(40, cancelled.Id),                               // cancelled batch: not reserved
            Hold(20, f.Batch.Id, shelf: false));                  // older hold with no shelf: still on the lot
        await db.SaveChangesAsync();
        var units = new Dictionary<Guid, (string?, decimal?)> { [f.Lot.MaterialId] = ("kg", null) };

        var level = (await StoreStock.Measure(db, f.Storage.Id, units))[f.Lot.MaterialId];
        Assert.Equal(new StoreStockLevel(100, 0, 0, 100, 50, 50), level);

        // Holds larger than the shelf (stock already left it) never push free stock below zero.
        db.MaterialBatchReservedQuantities.Add(Hold(500, f.Batch.Id)); await db.SaveChangesAsync();
        Assert.Equal(new StoreStockLevel(100, 0, 0, 100, 100, 0), (await StoreStock.Measure(db, f.Storage.Id, units))[f.Lot.MaterialId]);

        // Expired and unreleased lots are on hand but never usable or free.
        f.Lot.ExpiryDate = DateTime.UtcNow.AddDays(-1); f.Lot.RetestDate = null; await db.SaveChangesAsync();
        Assert.Equal(new StoreStockLevel(100, 100, 0, 0, 0, 0), (await StoreStock.Measure(db, f.Storage.Id, units))[f.Lot.MaterialId]);
        f.Lot.ExpiryDate = DateTime.UtcNow.AddYears(1); f.Lot.Status = BatchStatus.Quarantine; await db.SaveChangesAsync();
        Assert.Equal(new StoreStockLevel(100, 0, 100, 0, 0, 0), (await StoreStock.Measure(db, f.Storage.Id, units))[f.Lot.MaterialId]);
    }

    [Fact]
    public async Task Batch_rows_add_up_to_the_approved_materials_totals()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.ExpiryDate = DateTime.UtcNow.AddYears(1); f.Shelf.Quantity = 100; f.Shelf.UoMId = f.Lot.UoMId;
        db.MaterialBatchReservedQuantities.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = f.Lot.Id, WarehouseId = f.Production.Id,
            WarehouseLocationShelfId = f.Shelf.WarehouseLocationShelfId, ProductionScheduleProductId = f.Batch.Id, UoMId = f.Lot.UoMId, Quantity = 30 });
        await db.SaveChangesAsync();
        var repository = new MaterialRepository(db, f.Mapper);

        var list = Assert.Single((await repository.GetApprovedMaterials(1, 10, null, DOMAIN.Entities.Materials.MaterialKind.Raw, f.Actor.Id)).Value.Data);
        var batches = (await repository.GetMaterialBatchesByMaterialIdV2(1, 10, f.Lot.MaterialId, f.Department.Id, f.Actor.Id)).Value.Data.ToList();
        Assert.Equal(list.TotalAvailableQuantity, batches.Sum(row => row.Quantity));
        Assert.Equal(list.ReservedQuantity, batches.Sum(row => row.ReservedQuantity));
        Assert.Equal(list.FreeQuantity, batches.Sum(row => row.FreeQuantity));
        Assert.Equal((30m, 70m, "Usable"), (Assert.Single(batches).ReservedQuantity!.Value, batches[0].FreeQuantity!.Value, batches[0].StockState));
    }

    [Fact]
    public async Task Transfer_asked_in_litres_of_a_milligram_store_reads_and_reports_in_the_largest_unit()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.UoM.Symbol = "mg"; f.Lot.Material.DensityGramsPerMilliliter = 1.036m; f.Lot.ExpiryDate = DateTime.UtcNow.AddYears(1);
        f.Shelf.Quantity = 3_743_300_000; f.Shelf.UoMId = f.Lot.UoMId; f.Lot.TotalQuantity = f.Shelf.Quantity; f.Lot.QuantityAssigned = f.Shelf.Quantity;
        var litre = new UnitOfMeasure { Symbol = "L", Name = "Litre" };
        var transfer = new StockTransferSource { Id = Guid.NewGuid(), StockTransfer = new() { MaterialId = f.Lot.MaterialId, UoM = litre },
            FromDepartmentId = f.Department.Id, ToDepartmentId = f.OtherDepartment.Id, Quantity = 20_000, Status = StockTransferStatus.Approved };
        db.Add(transfer); await db.SaveChangesAsync(); db.ChangeTracker.Clear();

        var read = await f.Repository.GetStockTransferSource(transfer.Id);
        Assert.True(read.IsSuccess, read.IsFailure ? read.Error.Description : null);
        Assert.Equal(20_720_000_000m, read.Value.SupplierQuantity); Assert.Equal("mg", read.Value.SupplierUoM.Symbol);

        var preview = await f.Repository.BatchesToSupplyForStockTransfer(transfer.Id);
        Assert.StartsWith("The supplying store has 3,743.3 kg free of the 20,720 kg needed for 20,000 L. It is short by 16,976.7 kg. "
            + "20,000 L is 20,720 kg at the material's verified density of 1.036 g/mL.", preview.Error.Description);

        // Asking for it again is refused when it is raised, in the supplier's unit, and the department list leaves it out.
        var requester = new User { Id = Guid.NewGuid(), UserName = "syrup-requester", DepartmentId = f.OtherDepartment.Id };
        db.Users.Add(requester); await db.SaveChangesAsync();
        var create = await f.Repository.CreateStockTransfer(new DOMAIN.Entities.ProductionSchedules.StockTransfers.Request.CreateStockTransferRequest
        {
            Code = "ST-2", MaterialId = f.Lot.MaterialId, UoMId = litre.Id, RequiredQuantity = 20_000,
            Sources = [new() { FromDepartmentId = f.Department.Id, Quantity = 20_000 }],
        }, requester.Id);
        Assert.Equal("StockTransfer.InsufficientStock", create.Error.Code);
        Assert.StartsWith("The supplying store has 3,743.3 kg free of the 20,720 kg needed for 20,000 L.", create.Error.Description);
        var materials = new MaterialRepository(db, f.Mapper);
        // The request form sends the quantity in the requester's own unit; without a unit the server assumes that one.
        db.MaterialDepartments.Add(new() { MaterialId = f.Lot.MaterialId, DepartmentId = f.OtherDepartment.Id, UoMId = litre.Id });
        await db.SaveChangesAsync();
        Assert.Equal(litre.Id, await materials.DepartmentUnitOfCaller(f.Lot.MaterialId, requester.Id));
        Assert.DoesNotContain((await materials.GetDepartmentsWithEnoughStock(f.Lot.MaterialId, 20_000, litre.Id)).Value, d => d.Id == f.Department.Id);
        Assert.Contains((await materials.GetDepartmentsWithEnoughStock(f.Lot.MaterialId, 3_000, litre.Id)).Value, d => d.Id == f.Department.Id);

        var allowed = await f.Repository.CreateStockTransfer(new DOMAIN.Entities.ProductionSchedules.StockTransfers.Request.CreateStockTransferRequest
        {
            Code = "ST-3", MaterialId = f.Lot.MaterialId, UoMId = litre.Id, RequiredQuantity = 3_000,
            Sources = [new() { FromDepartmentId = f.Department.Id, Quantity = 3_000 }],
        }, requester.Id);
        Assert.True(allowed.IsSuccess, allowed.IsFailure ? allowed.Error.Description : null);
    }
}
