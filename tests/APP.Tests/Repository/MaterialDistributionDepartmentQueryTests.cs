using APP.Services.Procurement;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class MaterialDistributionDepartmentQueryTests
{
    private sealed class CurrentUser(Guid? departmentId = null) : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => departmentId;
        public string DepartmentType => "Production";
    }

    private static ApplicationDbContext RelationalContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=oryx_distribution_query;Username=unused")
            .ConfigureWarnings(warnings => warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
            .Options, new CurrentUser());

    [Fact]
    public void Old_lookup_reproduces_the_multiple_collection_join_warning()
    {
        using var context = RelationalContext();
        var id = Guid.NewGuid();
        var oldQuery = context.Requisitions.Include(r => r.Department).Where(r => r.Id == id);
        var error = Assert.Throws<InvalidOperationException>(() => oldQuery.ToQueryString());
        Assert.Contains("MultipleCollectionIncludeWarning", error.Message);
    }

    [Fact]
    public void New_lookup_compiles_as_split_queries_without_creator_credentials_or_storage_tree_joins()
    {
        using var context = RelationalContext();
        var sql = MaterialDistributionDepartmentQuery.ForRequisition(context, Guid.NewGuid()).ToQueryString();
        Assert.Contains("Requisitions", sql);
        Assert.Contains("Departments", sql);
        Assert.Contains("Material distribution: requesting department", sql);
        Assert.DoesNotContain("users", sql);
        Assert.DoesNotContain("PasswordHash", sql);
        Assert.DoesNotContain("WarehouseLocationRacks", sql);
        Assert.DoesNotContain("WarehouseLocationShelves", sql);
        Assert.DoesNotContain("JOIN \"Warehouses\"", sql);
    }

    [Fact]
    public async Task Returns_requesting_department_and_existing_warehouse_location_contract_without_tracking()
    {
        var department = new Department { Id = Guid.NewGuid(), Code = "PROD", Name = "Production" };
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser(department.Id));
        var creatorDepartment = new Department { Id = Guid.NewGuid(), Name = "Creator department" };
        var creator = new User { Id = Guid.NewGuid(), FirstName = "Creator", DepartmentId = creatorDepartment.Id };
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Raw materials", DepartmentId = department.Id };
        var location = new WarehouseLocation { Id = Guid.NewGuid(), Name = "Ground floor", WarehouseId = warehouse.Id };
        var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), Name = "Rack", WarehouseLocationId = location.Id };
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "Shelf", WarehouseLocationRackId = rack.Id };
        var requisition = new Requisition { Id = Guid.NewGuid(), Code = "REQ-1", DepartmentId = department.Id, CreatedById = creator.Id };
        context.AddRange(department, creatorDepartment, creator, warehouse, location, rack, shelf, requisition);
        await context.SaveChangesAsync();
        // SaveEntity stamps the actor; restore the persisted creator relationship for this seed.
        requisition.CreatedById = creator.Id;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await MaterialDistributionDepartmentQuery.ForRequisition(context, requisition.Id).SingleAsync();
        Assert.Equal(department.Id, result.Id);
        Assert.Equal("PROD", result.Code);
        var loadedWarehouse = Assert.Single(result.Warehouses);
        Assert.Equal(warehouse.Id, loadedWarehouse.Id);
        var loadedLocation = Assert.Single(loadedWarehouse.Locations);
        Assert.Equal(location.Id, loadedLocation.Id);
        Assert.Empty(loadedLocation.Racks);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Department_warehouse_scope_filters_are_preserved()
    {
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser(Guid.NewGuid()));
        var department = new Department { Id = Guid.NewGuid(), Name = "Other department" };
        var warehouse = new Warehouse { Id = Guid.NewGuid(), DepartmentId = department.Id, Name = "Other warehouse" };
        var request = new Requisition { Id = Guid.NewGuid(), DepartmentId = department.Id };
        context.AddRange(department, warehouse, request);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await MaterialDistributionDepartmentQuery.ForRequisition(context, request.Id).SingleAsync();
        Assert.Equal(department.Id, result.Id);
        Assert.Empty(result.Warehouses);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_or_deleted_requisition_is_not_returned(bool deleted)
    {
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());
        var id = Guid.NewGuid();
        if (deleted)
        {
            var department = new Department { Id = Guid.NewGuid(), Name = "Department" };
            context.AddRange(department, new Requisition { Id = id, DepartmentId = department.Id, DeletedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
        }
        var result = await MaterialDistributionDepartmentQuery.ForRequisition(context, id).FirstOrDefaultAsync();
        Assert.Null(result);
    }
}
