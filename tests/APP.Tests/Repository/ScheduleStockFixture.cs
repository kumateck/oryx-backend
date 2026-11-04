using APP.Repository;
using APP.Tests.QcWorksheets;
using AutoMapper;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using Microsoft.Extensions.DependencyInjection;
using SHARED;

namespace APP.Tests.Repository;

internal sealed class ScheduleStockFixture : IDisposable
{
    internal QcWorksheetTestContext Harness { get; } = new();
    internal Department Department { get; } = new() { Id = Guid.NewGuid(), Name = "Tablet", Type = DepartmentType.Production };
    internal Department OtherDepartment { get; } = new() { Id = Guid.NewGuid(), Name = "Syrup", Type = DepartmentType.Production };
    internal User Actor { get; private set; } = new();
    internal User Creator { get; private set; } = new();
    internal MaterialBatch Lot { get; private set; } = new();
    internal ProductionSchedule Schedule { get; private set; } = new();
    internal ProductionScheduleProduct Batch { get; private set; } = new();
    internal Warehouse Storage { get; private set; } = new();
    internal Warehouse Production { get; private set; } = new();
    internal ShelfMaterialBatch Shelf { get; private set; } = new();
    internal ProductionScheduleRepository Repository { get; private set; } = null!;
    internal IMapper Mapper { get; private set; } = null!;
    private ServiceProvider? mapping;

    internal async Task Seed()
    {
        var db = Harness.Db;
        Lot = await Harness.SeedMaterialBatch("RMC022", BatchStatus.Available);
        Lot.Material.Code = "RMC022";
        Lot.Material.Kind = MaterialKind.Raw;
        Lot.ExpiryDate = DateTime.UtcNow.AddDays(-30);
        Lot.RetestDate = DateTime.UtcNow.AddDays(120);
        Lot.UoM.Symbol = "mg";
        Lot.TotalQuantity = 1_000_000;
        Actor = new() { Id = Guid.NewGuid(), UserName = "schedule-viewer", Department = Department };
        Creator = new() { Id = Guid.NewGuid(), UserName = "schedule-creator", Department = Department };
        db.Users.AddRange(Actor, Creator);
        db.Departments.Add(OtherDepartment);
        Storage = new() { Id = Guid.NewGuid(), Name = "Tablet raw", Department = Department, Type = WarehouseType.RawMaterialStorage };
        Production = new() { Id = Guid.NewGuid(), Name = "Tablet production", Department = Department, Type = WarehouseType.Production };
        db.Warehouses.AddRange(Storage, Production,
            new Warehouse { Id = Guid.NewGuid(), Name = "Tablet packing", Department = Department, Type = WarehouseType.PackagedStorage });
        Shelf = new() { Id = Guid.NewGuid(), MaterialBatch = Lot, Quantity = 1_000_000, UoMId = Lot.UoMId,
            WarehouseLocationShelf = new() { Id = Guid.NewGuid(), Name = "A", WarehouseLocationRack = new()
            { Id = Guid.NewGuid(), Name = "Rack", WarehouseLocation = new() { Id = Guid.NewGuid(), Name = "Room", Warehouse = Storage } } } };
        db.ShelfMaterialBatches.Add(Shelf);
        var product = new Product { Id = Guid.NewGuid(), Code = "TABLET-1", Name = "Tablet product", Department = Department,
            BaseUomId = Lot.UoMId, FullBatchSize = 24 };
        var bom = new BillOfMaterial { Id = Guid.NewGuid(), Product = product, IsActive = true, Items =
            [new() { Id = Guid.NewGuid(), Material = Lot.Material, BaseUoMId = Lot.UoMId, BaseQuantity = 24, PrescribedQuantity = 300_000 }] };
        db.ProductBillOfMaterials.Add(new() { Product = product, BillOfMaterial = bom, IsActive = true, EffectiveDate = DateTime.UtcNow.AddDays(-1) });
        db.MaterialDepartments.Add(new() { Id = Guid.NewGuid(), Material = Lot.Material, Department = Department, UoMId = Lot.UoMId });
        Schedule = new() { Id = Guid.NewGuid(), Code = "PS-TABLET", Department = Department, CreatedBy = Creator };
        Batch = new() { Id = Guid.NewGuid(), Product = product, ProductionSchedule = Schedule, BatchSize = BatchSize.Full };
        db.ProductionScheduleProducts.Add(Batch);
        await db.SaveChangesAsync();
        mapping = MaterialValidityTests.Mapping(Harness);
        Mapper = mapping.GetRequiredService<IMapper>();
        Repository = new(db, Mapper, Harness.UserManager, new MaterialRepository(db, Mapper), null!, Harness.ApprovalRepository,
            new NoOpProductionActivityStepEventPublisher());
    }

    internal ProductionMaterialConfiguration Configuration() => new()
    {
        Id = Guid.NewGuid(), ProductionScheduleProductId = Batch.Id, Kind = Lot.Material.Kind,
        DepartmentId = Department.Id, StorageWarehouseId = Storage.Id, ProductionWarehouseId = Production.Id,
        RequirementsJson = ProductionMaterialSources.Json(new[] { Requirement() })
    };

    internal ProductionMaterialRequirement Requirement() => new()
    {
        MaterialId = Lot.MaterialId, SourceMaterialId = Lot.MaterialId, Name = "RMC022", Quantity = 300_000,
        UnitId = Lot.UoMId, Unit = "mg", BaseUnitId = Lot.UoMId, BaseUnit = "mg"
    };

    public void Dispose() { mapping?.Dispose(); Harness.Dispose(); }
}
