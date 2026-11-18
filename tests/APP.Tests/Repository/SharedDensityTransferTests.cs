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
        await Grant(db, actor.Id, PermissionKeys.CanIssueRawMaterialStockTransfers);
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
        var stock = await repository.GetShelfMaterialStockInWarehouse(f.Lot.MaterialId, warehouse.Id);
        Assert.True(stock.IsSuccess, stock.Error.Description); Assert.Equal(requested, stock.Value);
        Assert.True((await repository.MoveMaterialBatchToWarehouseFromHolding(request, actor.Id)).IsFailure);
        Assert.Equal(physical, placed.Quantity);
    }
}
