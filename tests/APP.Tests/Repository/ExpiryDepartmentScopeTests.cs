using APP.Repository;
using APP.Tests.QcWorksheets;
using AutoMapper;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace APP.Tests.Repository;

public class ExpiryDepartmentScopeTests
{
    private static void Shelf(QcWorksheetTestContext harness, MaterialBatch batch, Warehouse warehouse, decimal quantity)
    {
        var location = new WarehouseLocation { Id = Guid.NewGuid(), Warehouse = warehouse };
        var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), WarehouseLocation = location };
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = warehouse.Name + " shelf", WarehouseLocationRack = rack };
        harness.Db.ShelfMaterialBatches.Add(new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatch = batch, WarehouseLocationShelf = shelf, Quantity = quantity });
    }

    [Theory]
    [InlineData(false, MaterialKind.Raw)]
    [InlineData(true, MaterialKind.Raw)]
    [InlineData(false, MaterialKind.Package)]
    [InlineData(true, MaterialKind.Package)]
    public async Task Current_account_department_limits_results_counts_locations_and_quantities_before_pagination(bool soon, MaterialKind kind)
    {
        using var harness = new QcWorksheetTestContext();
        var own = new Department { Id = Guid.NewGuid(), Name = "Oral solids" };
        var other = new Department { Id = Guid.NewGuid(), Name = "Liquids" };
        var ownStore = new Warehouse { Id = Guid.NewGuid(), Name = "Own store", Department = own };
        var otherStore = new Warehouse { Id = Guid.NewGuid(), Name = "Other store", Department = other };
        var actor = new User { Id = Guid.NewGuid(), UserName = "department-reader", Department = own };
        harness.Db.Users.Add(actor);
        var batch = await harness.SeedMaterialBatch("SHARED", BatchStatus.Available);
        batch.Material.Code = "cy-shared"; batch.Material.Kind = kind;
        batch.ExpiryDate = DateTime.UtcNow.AddDays(soon ? 30 : -30); batch.TotalQuantity = 1000;
        Shelf(harness, batch, ownStore, 5); Shelf(harness, batch, otherStore, 17);
        harness.Db.MaterialBatchReservedQuantities.AddRange(
            new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = batch, Warehouse = ownStore, Quantity = 2 },
            new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = batch, Warehouse = otherStore, Quantity = 9 });
        var foreign = await harness.SeedMaterialBatch("FOREIGN", BatchStatus.Available);
        foreign.Material.Code = "cy-foreign"; foreign.Material.Kind = kind;
        foreign.ExpiryDate = batch.ExpiryDate.Value.AddDays(-1);
        Shelf(harness, foreign, otherStore, 33); Shelf(harness, foreign, ownStore, 0);
        // A material-use association cannot grant access to another department's physical stock.
        harness.Db.MaterialDepartments.Add(new MaterialDepartment { Id = Guid.NewGuid(), Material = foreign.Material, Department = own, UoMId = foreign.UoMId });
        StockReportTestPositions.MarkIssued(harness);
        await harness.Db.SaveChangesAsync(); harness.Db.ChangeTracker.Clear();
        using var mapping = MaterialValidityTests.Mapping(harness);
        var repository = new MaterialRepository(harness.Db, mapping.GetRequiredService<IMapper>());
        Task<SHARED.Result<APP.Utils.Paginateable<IEnumerable<MaterialBatchDto>>>> Read(MaterialFilter filter, Guid userId) => soon
            ? repository.GetAboutToExpireMaterialBatches(filter, userId) : repository.GetExpiredMaterialBatches(filter, userId);
        var filter = new MaterialFilter { SearchQuery = "cy", PageSize = 1, MaterialKind = kind };
        var result = await Read(filter, actor.Id);
        Assert.Equal(1, result.Value.TotalRecordCount);
        var row = Assert.Single(result.Value.Data);
        Assert.Equal(batch.Id, row.Id);
        Assert.Equal(own.Id, Assert.Single(row.StockDepartments).Id);
        Assert.Equal(5, row.RemainingQuantity); Assert.Equal(2, row.ReservedQuantity); Assert.Equal(7, row.TotalQuantity);
        Assert.Equal(5, Assert.Single(row.Shelves).Quantity);
        Assert.Equal(ownStore.Id, Assert.Single(row.ReservedQuantities).Warehouse.Id);
        Assert.Equal(ownStore.Id, Assert.Single(row.Locations).Location.Id);
        Assert.Equal(7, Assert.Single(row.Locations).QuantityAtLocation);
        var foreignFilter = new MaterialFilter { SearchQuery = "cy", WarehouseIds = [otherStore.Id] };
        Assert.Empty((await Read(foreignFilter, actor.Id)).Value.Data);
        // An account reassignment takes effect on the next request, without trusting old department claims.
        var stored = await harness.Db.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == actor.Id);
        stored.DepartmentId = other.Id; stored.Department = null;
        await harness.Db.SaveChangesAsync(); harness.Db.ChangeTracker.Clear();
        var reassigned = Assert.Single((await Read(new MaterialFilter { SearchQuery = "cy-shared" }, actor.Id)).Value.Data);
        Assert.Equal(other.Id, Assert.Single(reassigned.StockDepartments).Id);
        Assert.Equal(17, reassigned.RemainingQuantity); Assert.Equal(26, reassigned.TotalQuantity);
        var otherShelves = await harness.Db.ShelfMaterialBatches.IgnoreQueryFilters().Where(item => item.MaterialBatchId == batch.Id
            && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == otherStore.Id).ToListAsync();
        foreach (var shelf in otherShelves) shelf.Quantity = 0;
        await harness.Db.SaveChangesAsync();
        var reservedOnly = Assert.Single((await Read(new MaterialFilter { SearchQuery = "cy-shared" }, actor.Id)).Value.Data);
        Assert.Empty(reservedOnly.Shelves); Assert.Equal(0, reservedOnly.RemainingQuantity); Assert.Equal(9, reservedOnly.TotalQuantity);
        var allocations = await harness.Db.MaterialBatchReservedQuantities.IgnoreQueryFilters()
            .Where(item => item.MaterialBatchId == batch.Id && item.WarehouseId == otherStore.Id).ToListAsync();
        foreach (var allocation in allocations) allocation.Quantity = 0;
        await harness.Db.SaveChangesAsync();
        Assert.Empty((await Read(new MaterialFilter { SearchQuery = "cy-shared" }, actor.Id)).Value.Data);
        stored = await harness.Db.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == actor.Id);
        stored.IsDisabled = true; await harness.Db.SaveChangesAsync();
        Assert.True((await Read(filter, actor.Id)).IsFailure);
        Assert.True((await Read(filter, Guid.NewGuid())).IsFailure);
    }
}
