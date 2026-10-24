using APP.Services.Materials;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public class MaterialPurchaseUnitReaderTests
{
    [Fact]
    public async Task ReadsAllDepartmentsIncludingZeroStockAndDeduplicatesSymbols()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var data = new PurchaseUnitTestData(); await data.Seed(context);
        var otherMg = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Duplicate", Symbol = " MG " };
        var volume = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Litre", Symbol = "L" };
        var first = new Department { Id = Guid.NewGuid(), Name = "One" };
        var second = new Department { Id = Guid.NewGuid(), Name = "Two" };
        var third = new Department { Id = Guid.NewGuid(), Name = "Three" };
        context.AddRange(otherMg, volume, first, second, third,
            data.Link(first.Id, data.UnitId), data.Link(second.Id, otherMg.Id), data.Link(third.Id, volume.Id));
        await context.SaveChangesAsync();
        var units = await new MaterialPurchaseUnitsReader(context).Read(data.MaterialId);
        Assert.Equal(2, units.Count);
        Assert.Contains(units, unit => unit.Symbol == "L");
        Assert.Single(units.Where(unit => unit.Symbol.Equals("mg", StringComparison.OrdinalIgnoreCase)));
        Assert.Empty(context.MaterialBatches);
    }

    [Fact]
    public async Task ExcludesDeletedLinkDepartmentAndUnitAndUnrelatedMaterial()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var data = new PurchaseUnitTestData(); await data.Seed(context);
        var active = new Department { Id = Guid.NewGuid(), Name = "Active" };
        var deleted = new Department { Id = Guid.NewGuid(), Name = "Deleted", DeletedAt = DateTime.UtcNow };
        var deadUnit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Deleted", Symbol = "L", DeletedAt = DateTime.UtcNow };
        var deletedLink = data.Link(active.Id, data.UnitId); deletedLink.DeletedAt = DateTime.UtcNow;
        context.AddRange(active, deleted, deadUnit, deletedLink, data.Link(deleted.Id, data.UnitId), data.Link(active.Id, deadUnit.Id));
        await context.SaveChangesAsync();
        Assert.Empty(await new MaterialPurchaseUnitsReader(context).Read(data.MaterialId));
        Assert.Empty(await new MaterialPurchaseUnitsReader(context).Read(Guid.NewGuid()));
    }

    [Fact]
    public async Task SymbolPayloadUsesAnyDepartmentAndRetainsLegacyQuantityReference()
    {
        await using var context = SupplierRelationshipTestContext.Create();
        var data = new PurchaseUnitTestData(); await data.Seed(context);
        var department = new Department { Id = Guid.NewGuid(), Name = "Another department" };
        context.AddRange(department, data.Link(department.Id, data.UnitId)); await context.SaveChangesAsync();
        var request = new DOMAIN.Entities.Procurement.Suppliers.CreateSupplierRequest {
            AssociatedManufacturers = [new() { MaterialId = data.MaterialId, ManufacturerId = data.ManufacturerId, PurchaseUnit = " KG " }] };
        Assert.True((await SupplierPurchaseUnitPolicy.Normalize(context, request, data.SupplierId)).IsSuccess);
        Assert.Equal("kg", request.AssociatedManufacturers[0].PurchaseUnit);
        Assert.Equal(data.UnitId, request.AssociatedManufacturers[0].UoMId);
        request.AssociatedManufacturers[0].PurchaseUnit = "L";
        Assert.False((await SupplierPurchaseUnitPolicy.Normalize(context, request, data.SupplierId)).IsSuccess);
    }
}
