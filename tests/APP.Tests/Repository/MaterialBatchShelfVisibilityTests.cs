using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class MaterialBatchShelfVisibilityTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    [Fact]
    public async Task Stock_by_batch_excludes_deleted_and_empty_shelves_before_pagination()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());
        var department = new Department { Id = Guid.NewGuid(), Name = "SYRUP" };
        var store = new Warehouse { Id = Guid.NewGuid(), Name = "Raw store", Type = WarehouseType.RawMaterialStorage, Department = department };
        department.Warehouses = [store];
        var actor = new User { Id = Guid.NewGuid(), UserName = "warehouse", Department = department };
        var material = new Material { Id = Guid.NewGuid(), Name = "Propylene Glycol", Code = "RM1", Kind = MaterialKind.Raw,
            Departments = [new MaterialDepartment { Department = department }] };
        var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Millilitre", Symbol = "ml" };
        var batch = new MaterialBatch { UoM = unit, Id = Guid.NewGuid(), Material = material, BatchNumber = "KJ2283PU1B", Status = BatchStatus.Available };
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "B", WarehouseLocationRack = new WarehouseLocationRack
            { Name = "G/09", WarehouseLocation = new WarehouseLocation { Name = "General", Warehouse = store } } };
        var active = new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatch = batch, WarehouseLocationShelf = shelf, UoM = unit, Quantity = 37500 };
        db.Users.Add(actor);
        db.ShelfMaterialBatches.AddRange(active,
            new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatch = batch, WarehouseLocationShelf = shelf, UoM = unit, Quantity = 0 },
            new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatch = batch, WarehouseLocationShelf = shelf, UoM = unit, Quantity = 100, DeletedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var mapper = new MapperConfiguration(c => c.CreateMap<ShelfMaterialBatch, ShelfMaterialBatchDto>(MemberList.None)
            .ForMember(d => d.WarehouseLocationShelf, o => o.Ignore()).ForMember(d => d.MaterialBatch, o => o.Ignore())
            .ForMember(d => d.UoM, o => o.Ignore())).CreateMapper();
        var result = await new MaterialRepository(db, mapper).GetMaterialBatchesByMaterialIdV2(1, 1, material.Id, department.Id, actor.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecordCount);
        Assert.Equal((active.Id, 37500m), (Assert.Single(result.Value.Data).Id, Assert.Single(result.Value.Data).Quantity));
    }
}
