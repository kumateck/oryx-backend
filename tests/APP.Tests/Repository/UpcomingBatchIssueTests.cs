using APP.Repository;
using APP.Tests.QcWorksheets;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Identity;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public class UpcomingBatchIssueTests
{
    private sealed record Store(Warehouse Warehouse, WarehouseLocationShelf Shelf);

    private static Store NewStore(QcWorksheetTestContext harness, Department department, string name)
    {
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = name, Department = department };
        var location = new WarehouseLocation { Id = Guid.NewGuid(), Name = "Bay A", Warehouse = warehouse };
        var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), Name = "R1", WarehouseLocation = location };
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "S2", WarehouseLocationRack = rack };
        harness.Db.WarehouseLocationShelves.Add(shelf);
        return new Store(warehouse, shelf);
    }

    private static async Task<BatchManufacturingRecord> Started(QcWorksheetTestContext harness, ProductionSchedule schedule, Product product, Store store,
        string lot, decimal quantity, bool issued = false, bool cancelled = false)
    {
        var batch = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductionSchedule = schedule, Product = product, Quantity = 1, Cancelled = cancelled,
            ProductionActivity = new ProductionActivity { Id = Guid.NewGuid(), StartedAt = DateTime.UtcNow.AddHours(-(double)quantity) } };
        harness.Db.ProductionScheduleProducts.Add(batch);
        var bmr = new BatchManufacturingRecord { Id = Guid.NewGuid(), ProductionScheduleProduct = batch, ProductionActivityStepId = Guid.NewGuid(),
            BatchQuantity = 500, IssuedDate = issued ? DateTime.UtcNow : null };
        harness.Db.BatchManufacturingRecords.Add(bmr);
        var materialBatch = await harness.SeedMaterialBatch(lot, BatchStatus.Available);
        harness.Db.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = materialBatch, Warehouse = store.Warehouse,
            WarehouseLocationShelf = store.Shelf, ProductionScheduleProduct = batch, UoMId = materialBatch.UoMId, Quantity = quantity });
        return bmr;
    }

    [Fact]
    public async Task Lists_started_unissued_batches_with_the_lots_reserved_in_the_readers_stores()
    {
        using var harness = new QcWorksheetTestContext();
        var tablets = new Department { Id = Guid.NewGuid(), Name = "Tablets" };
        var liquids = new Department { Id = Guid.NewGuid(), Name = "Liquids" };
        var own = NewStore(harness, tablets, "Tablet raw store");
        var other = NewStore(harness, liquids, "Liquid raw store");
        var keeper = new User { Id = Guid.NewGuid(), UserName = "storekeeper", Department = tablets };
        harness.Db.Users.Add(keeper);
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Tablet", Symbol = "tabs" };
        var product = new Product { Id = Guid.NewGuid(), Code = "PDT-9", Name = "Paracetamol 500mg", BaseUoM = unit };
        var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PS-11" };
        harness.Db.ProductionSchedules.Add(schedule);
        var waiting = await Started(harness, schedule, product, own, "LOT-A", 3);
        await Started(harness, schedule, product, own, "LOT-ISSUED", 2, issued: true);
        await Started(harness, schedule, product, own, "LOT-CANCELLED", 2, cancelled: true);
        await Started(harness, schedule, product, other, "LOT-ELSEWHERE", 2);
        await harness.Db.SaveChangesAsync(); harness.Db.ChangeTracker.Clear();
        var repository = new UpcomingBatchIssueRepository(harness.Db);

        Assert.Equal(ErrorType.Forbidden, (await repository.List(keeper.Id, 1, 20, null)).Error.Type);

        var role = new Role { Id = Guid.NewGuid(), Name = "Stores" }; harness.Db.Roles.Add(role);
        harness.Db.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = role.Id, UserId = keeper.Id });
        var claim = new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanViewUpcomingBatchIssues };
        harness.Db.RoleClaims.Add(claim); await harness.Db.SaveChangesAsync();
        harness.Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = claim.ClaimValue, Type = "View" });
        await harness.Db.SaveChangesAsync(); harness.Db.ChangeTracker.Clear();

        var page = (await repository.List(keeper.Id, 1, 20, null)).Value;
        var row = Assert.Single(page.Data);
        Assert.Equal(waiting.Id, row.BmrId);
        Assert.Equal(("PDT-9", "Paracetamol 500mg", "PS-11", 500m, "tabs"), (row.ProductCode, row.ProductName, row.ScheduleCode, row.BatchQuantity, row.BatchUnit));
        var pick = Assert.Single(row.Materials);
        Assert.Equal(("LOT-A", "Tablet raw store", "Bay A · R1 · S2", 3m, "kg"), (pick.LotNumber, pick.Warehouse, pick.Location, pick.Quantity, pick.Unit));
        Assert.Empty((await repository.List(keeper.Id, 1, 20, "no such product")).Value.Data);
        Assert.Single((await repository.List(keeper.Id, 1, 20, "ps-11")).Value.Data);
    }

    [Fact]
    public async Task Earlier_batches_to_swap_load_for_a_started_batch()
    {
        // Production error 2026-11-19: MaterialBatch.ReservedQuantities is auto-included, so a no-tracking read of a batch's
        // reservations that includes their lot loops back to the reservations and EF refuses the query (500).
        using var harness = new QcWorksheetTestContext();
        var tablets = new Department { Id = Guid.NewGuid(), Name = "Tablets" };
        var own = NewStore(harness, tablets, "Tablet raw store");
        own.Warehouse.Type = WarehouseType.RawMaterialStorage;
        var keeper = new User { Id = Guid.NewGuid(), UserName = "storekeeper", Department = tablets };
        harness.Db.Users.Add(keeper);
        var product = new Product { Id = Guid.NewGuid(), Code = "PDT-9", Name = "Paracetamol 500mg", BaseUoM = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Tablet", Symbol = "tabs" } };
        var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PS-11" };
        harness.Db.ProductionSchedules.Add(schedule);
        var started = await Started(harness, schedule, product, own, "LOT-A", 3);
        await harness.Db.SaveChangesAsync(); harness.Db.ChangeTracker.Clear();
        using var mapping = MaterialValidityTests.Mapping(harness);
        var repository = new RequisitionRepository(harness.Db, Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<AutoMapper.IMapper>(mapping),
            null!, null!, null!, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());

        var result = await repository.GetAlternativeBatchesForProductionBatch(started.ProductionScheduleProductId, keeper.Id);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        Assert.Equal(3m, Assert.Single(result.Value).RequestedQuantity);
    }
}
