using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public class GrnListSupplierTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Supplier_summary_honors_existing_checklist_and_supplier_filters_and_keeps_batch_count(bool deletedSupplier)
    {
        await using var fixture = new GrnListFixture();
        var row = fixture.AddNote("GRN", count: 2);
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Supplier name",
            DeletedAt = deletedSupplier ? DateTime.UtcNow : null };
        var warehouse = new Warehouse { Id = Guid.NewGuid(), DepartmentId = fixture.Department.Id };
        var location = new WarehouseArrivalLocation { Id = Guid.NewGuid(), WarehouseId = warehouse.Id };
        var distribution = new DistributedRequisitionMaterial { Id = Guid.NewGuid(), MaterialId = row.Material.Id,
            WarehouseArrivalLocationId = location.Id, UoMId = fixture.Unit.Id };
        var checklist = new Checklist { Id = Guid.NewGuid(), SupplierId = supplier.Id,
            MaterialId = row.Material.Id, DistributedRequisitionMaterialId = distribution.Id };
        row.Batches[0].ChecklistId = checklist.Id;
        // The second batch has no checklist; it must still contribute to the note count.
        fixture.Context.AddRange(supplier, warehouse, location, distribution, checklist);
        await fixture.Save();
        var result = await fixture.Repository.GetGrns(1, 50, "", null, null);
        Assert.True(result.IsSuccess);
        var batches = Assert.Single(result.Value.Data).MaterialBatches;
        Assert.Equal(2, batches.Count);
        Assert.Equal(deletedSupplier ? null : "Supplier name",
            Assert.Single(batches, batch => batch.Id == row.Batches[0].Id).SupplierName);
        Assert.Null(Assert.Single(batches, batch => batch.Id == row.Batches[1].Id).SupplierName);
        Assert.Empty(fixture.Context.ChangeTracker.Entries());
    }
}
