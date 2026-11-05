using System.Reflection;
using API.Controllers;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using APP.Repository;
using APP.Tests.QcWorksheets;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.MaterialSampling;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public class MaterialStockReportTests
{
    private static ShelfMaterialBatch Shelf(QcWorksheetTestContext harness, MaterialBatch batch, Warehouse warehouse, decimal quantity, UnitOfMeasure unit = null)
    {
        var location = new WarehouseLocation { Id = Guid.NewGuid(), Warehouse = warehouse };
        var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), WarehouseLocation = location };
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), WarehouseLocationRack = rack };
        var position = new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatch = batch, WarehouseLocationShelf = shelf, Quantity = quantity, UoM = unit };
        harness.Db.ShelfMaterialBatches.Add(position); return position;
    }

    [Theory]
    [InlineData(nameof(MaterialController.GetMaterialStockReport), PermissionKeys.CanViewWarehouseReport)]
    [InlineData(nameof(MaterialController.GetExpiredMaterialBatches), PermissionKeys.CanViewExpiredMaterials)]
    [InlineData(nameof(MaterialController.GetAboutToExpireMaterialBatches), PermissionKeys.CanViewAboutToExpireMaterials)]
    public void Reports_require_the_existing_specific_permission_without_role_bypasses(string action, string permission)
    {
        var method = Assert.IsAssignableFrom<MethodInfo>(typeof(MaterialController).GetMethod(action));
        var policy = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(permission, policy.Policy); Assert.Null(policy.Roles);
    }

    [Theory]
    [InlineData(MaterialKind.Raw)]
    [InlineData(MaterialKind.Package)]
    public async Task Complete_physical_report_preserves_metadata_units_and_stock_ownership(MaterialKind kind)
    {
        using var harness = new QcWorksheetTestContext();
        var own = new Department { Id = Guid.NewGuid(), Name = "Oral solids" };
        var other = new Department { Id = Guid.NewGuid(), Name = "Liquids" };
        var ownStore = new Warehouse { Id = Guid.NewGuid(), Department = own };
        var otherStore = new Warehouse { Id = Guid.NewGuid(), Department = other };
        var actor = new User { Id = Guid.NewGuid(), UserName = "stock-reader", Department = own };
        var global = new User { Id = Guid.NewGuid(), UserName = "global-reader" };
        harness.Db.Users.AddRange(actor, global);
        var batch = await harness.SeedMaterialBatch("SHARED", BatchStatus.Available);
        batch.Material.Name = "Cyanocobalamin"; batch.Material.Kind = kind;
        batch.ManufacturingDate = DateTime.UtcNow.AddYears(-3); batch.ExpiryDate = DateTime.UtcNow.AddDays(-30);
        batch.Checklist = new Checklist { Id = Guid.NewGuid(), Manufacturer = new Manufacturer { Id = Guid.NewGuid(), Name = "Maker" }, Supplier = new Supplier { Id = Guid.NewGuid(), Name = "Supplier" } };
        harness.Db.Checklists.Add(batch.Checklist);
        Shelf(harness, batch, ownStore, 5); Shelf(harness, batch, ownStore, 1); Shelf(harness, batch, otherStore, 17);
        var grams = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Gram", Symbol = "g" };
        Shelf(harness, batch, ownStore, 300, grams); // Units must never be added as if they were the same unit.
        Shelf(harness, batch, ownStore, 999).DeletedAt = DateTime.UtcNow;
        Shelf(harness, batch, ownStore, 0);
        harness.Db.MaterialBatchReservedQuantities.AddRange(
            new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = batch, Warehouse = ownStore, UoMId = batch.UoMId, Quantity = 2 },
            new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = batch, Warehouse = ownStore, UoMId = batch.UoMId, Quantity = 700, ReleasedAt = DateTime.UtcNow },
            new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = batch, Warehouse = ownStore, UoMId = batch.UoMId, Quantity = 800, DeletedAt = DateTime.UtcNow });
        harness.Db.MaterialSamplings.AddRange(
            new MaterialSampling { Id = Guid.NewGuid(), MaterialBatch = batch, ArNumber = "AR-001", CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new MaterialSampling { Id = Guid.NewGuid(), MaterialBatch = batch, ArNumber = "AR-DELETED", CreatedAt = DateTime.UtcNow.AddDays(-6), DeletedAt = DateTime.UtcNow });
        var future = await harness.SeedMaterialBatch("FUTURE", BatchStatus.Available);
        future.Material.Kind = kind; future.ExpiryDate = DateTime.UtcNow.AddDays(90); Shelf(harness, future, ownStore, 1);
        var opposite = await harness.SeedMaterialBatch("OTHER-KIND", BatchStatus.Available);
        opposite.Material.Kind = kind == MaterialKind.Raw ? MaterialKind.Package : MaterialKind.Raw;
        opposite.ExpiryDate = batch.ExpiryDate; Shelf(harness, opposite, ownStore, 4);
        StockReportTestPositions.MarkIssued(harness);
        await harness.Db.SaveChangesAsync(); harness.Db.ChangeTracker.Clear();
        var repository = new MaterialRepository(harness.Db, harness.Mapper);
        var filter = new MaterialStockReportFilter { MaterialKind = kind };
        var result = await repository.GetMaterialStockReport(filter, actor.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(own.Id, result.Value.DepartmentId); Assert.Equal("Oral solids", result.Value.DepartmentName);
        Assert.InRange(result.Value.GeneratedAt, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
        var row = Assert.Single(result.Value.Rows);
        Assert.Equal(future.Id, row.BatchId);
        Assert.Equal(1, row.Quantity); Assert.Equal("FUTURE", row.BatchNumber);
        Assert.All(result.Value.Rows, item => Assert.Equal(own.Id, item.DepartmentId));
        Assert.Empty(harness.Db.ChangeTracker.Entries());
        filter.ExpiredOnly = true;
        var expiredRows = (await repository.GetMaterialStockReport(filter, actor.Id)).Value.Rows;
        Assert.Equal(2, expiredRows.Count);
        Assert.Contains(expiredRows, item => item.BatchId == batch.Id && item.UoM == "kg" && item.Quantity == 8);
        Assert.Contains(expiredRows, item => item.BatchId == batch.Id && item.UoM == "g" && item.Quantity == 300);
        filter.DepartmentId = other.Id;
        Assert.True((await repository.GetMaterialStockReport(filter, actor.Id)).IsFailure);
        Assert.Equal(17, Assert.Single((await repository.GetMaterialStockReport(filter, global.Id)).Value.Rows).Quantity);
        filter.DepartmentId = null; filter.ExpiredOnly = false;
        Assert.Equal(1, (await repository.GetMaterialStockReport(filter, global.Id)).Value.Rows.Count);
        var stored = await harness.Db.Users.IgnoreQueryFilters().SingleAsync(item => item.Id == actor.Id);
        stored.DepartmentId = other.Id; await harness.Db.SaveChangesAsync(); harness.Db.ChangeTracker.Clear();
        Assert.Empty((await repository.GetMaterialStockReport(filter, actor.Id)).Value.Rows);
        stored = await harness.Db.Users.IgnoreQueryFilters().SingleAsync(item => item.Id == actor.Id);
        stored.IsDisabled = true; await harness.Db.SaveChangesAsync();
        Assert.True((await repository.GetMaterialStockReport(filter, actor.Id)).IsFailure);
        Assert.True((await repository.GetMaterialStockReport(filter, Guid.NewGuid())).IsFailure);
        filter.MaterialKind = (MaterialKind)77;
        Assert.True((await repository.GetMaterialStockReport(filter, global.Id)).IsFailure);
    }

    [Theory]
    [InlineData(MaterialKind.Raw, 0)]
    [InlineData(MaterialKind.Package, 0)]
    public async Task Expired_count_uses_effective_validity_without_losing_original_dates(MaterialKind kind, int expected)
    {
        using var harness = new QcWorksheetTestContext();
        var actor = new User { Id = Guid.NewGuid() }; harness.Db.Users.Add(actor);
        var batch = await harness.SeedMaterialBatch("EXTENDED", BatchStatus.Available);
        batch.Material.Kind = kind; batch.ExpiryDate = DateTime.UtcNow.AddDays(-90); batch.RetestDate = DateTime.UtcNow.AddDays(60);
        Shelf(harness, batch, new Warehouse { Id = Guid.NewGuid() }, 2);
        await harness.Db.SaveChangesAsync(); harness.Db.ChangeTracker.Clear();
        var repository = new MaterialRepository(harness.Db, harness.Mapper);
        var filter = new MaterialStockReportFilter { ExpiredOnly = true };
        Assert.Equal(expected, (await repository.GetMaterialStockReport(filter, actor.Id)).Value.Rows.Count);
        filter.ExpiredOnly = false;
        // Raw and packaging extend alike, and the report keeps both original dates.
        var row = Assert.Single((await repository.GetMaterialStockReport(filter, actor.Id)).Value.Rows);
        Assert.Equal(batch.ExpiryDate, row.ExpiryDate); Assert.Equal(batch.RetestDate, row.RetestDate);
    }

    [Fact]
    public async Task An_unissued_hold_stays_in_the_source_department_and_is_not_counted_twice()
    {
        using var harness = new QcWorksheetTestContext();
        var source = new Department { Id = Guid.NewGuid(), Name = "Warehouse" };
        var target = new Department { Id = Guid.NewGuid(), Name = "Production" };
        var actor = new User { Id = Guid.NewGuid() }; var targetActor = new User { Id = Guid.NewGuid(), Department = target };
        harness.Db.Users.AddRange(actor, targetActor);
        var batch = await harness.SeedMaterialBatch("HELD", BatchStatus.Available);
        batch.ExpiryDate = DateTime.UtcNow.AddDays(-30);
        Shelf(harness, batch, new Warehouse { Id = Guid.NewGuid(), Department = source }, 10);
        var productionId = Guid.NewGuid();
        harness.Db.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = batch,
            Warehouse = new Warehouse { Id = Guid.NewGuid(), Department = target }, Quantity = 4, UoMId = batch.UoMId, ProductionScheduleProductId = productionId });
        var requisition = new DOMAIN.Entities.Requisitions.Requisition { Id = Guid.NewGuid(), ProductionScheduleProductId = productionId,
            RequisitionType = DOMAIN.Entities.Requisitions.RequisitionType.Stock, Status = DOMAIN.Entities.Requisitions.RequestStatus.Pending,
            Items = [new DOMAIN.Entities.Requisitions.RequisitionItem { Id = Guid.NewGuid(), MaterialId = batch.MaterialId }] };
        harness.Db.Requisitions.Add(requisition);
        await harness.Db.SaveChangesAsync(); harness.Db.ChangeTracker.Clear();
        var repository = new MaterialRepository(harness.Db, harness.Mapper);
        var row = Assert.Single((await repository.GetMaterialStockReport(new() { ExpiredOnly = true }, actor.Id)).Value.Rows);
        Assert.Equal(10, row.Quantity); Assert.Equal(source.Id, row.DepartmentId);
        Assert.Empty((await repository.GetMaterialStockReport(new(), targetActor.Id)).Value.Rows);
        Assert.Empty((await repository.GetExpiredMaterialBatches(new(), targetActor.Id)).Value.Data);
        using var mapping = MaterialValidityTests.Mapping(harness);
        var expiry = new MaterialRepository(harness.Db, mapping.GetRequiredService<AutoMapper.IMapper>());
        var globalRow = Assert.Single((await expiry.GetExpiredMaterialBatches(new(), actor.Id)).Value.Data);
        Assert.Equal(source.Id, Assert.Single(globalRow.StockDepartments).Id);
    }

    [Fact]
    public void PostgreSql_query_materializes_shelves_and_reservations_with_metadata_in_one_statement()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=sql_translation_only;Username=unused").Options, new NoCurrentUser());
        var sql = MaterialStockReportQuery.Rows(db, new MaterialStockReportFilter { ExpiredOnly = true }, Guid.NewGuid(), DateTime.UtcNow).ToQueryString();
        Assert.Contains("UNION ALL", sql); Assert.Contains("RetestDate", sql); Assert.Contains("ArNumber", sql);
        Assert.Contains("ReleasedAt", sql); Assert.Contains("DepartmentId", sql);
    }
}

internal static class StockReportTestPositions
{
    internal static void MarkIssued(QcWorksheetTestContext harness)
    {
        foreach (var group in harness.Db.MaterialBatchReservedQuantities.Local.Where(item => item.ProductionScheduleProductId == Guid.Empty).GroupBy(item => item.MaterialBatchId))
        {
            var productionId = Guid.NewGuid();
            foreach (var item in group) item.ProductionScheduleProductId = productionId;
            harness.Db.Requisitions.Add(new DOMAIN.Entities.Requisitions.Requisition {
                Id = Guid.NewGuid(), ProductionScheduleProductId = productionId,
                RequisitionType = DOMAIN.Entities.Requisitions.RequisitionType.Stock,
                Status = DOMAIN.Entities.Requisitions.RequestStatus.Completed,
                Items = [new DOMAIN.Entities.Requisitions.RequisitionItem { Id = Guid.NewGuid(), MaterialId = group.First().MaterialBatch.MaterialId }],
            });
        }
    }
}
