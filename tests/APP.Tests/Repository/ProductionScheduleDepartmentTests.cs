using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public class ProductionScheduleDepartmentTests
{
    [Fact]
    public async Task Current_account_branch_controls_list_detail_search_and_pagination_regardless_of_first_role()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var foreign = new Product { Id = Guid.NewGuid(), Name = "Foreign syrup", Code = "SYRUP", DepartmentId = fixture.OtherDepartment.Id, BaseUomId = fixture.Lot.UoMId };
        var foreignSchedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PS-SYRUP", DepartmentId = fixture.OtherDepartment.Id };
        var mixed = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PS-LEGACY", Products = [
            new() { Id = Guid.NewGuid(), ProductId = fixture.Batch.ProductId }, new() { Id = Guid.NewGuid(), Product = foreign }] };
        db.ProductionScheduleProducts.Add(new() { Id = Guid.NewGuid(), Product = foreign, ProductionSchedule = foreignSchedule });
        db.ProductionSchedules.Add(mixed);
        db.ProductionSchedules.Add(new() { Id = Guid.NewGuid(), Code = "REMOVED", DeletedAt = DateTime.UtcNow,
            Products = [new() { Id = Guid.NewGuid(), ProductId = fixture.Batch.ProductId }] });
        var role = new Role { Id = Guid.NewGuid(), Name = "ONLY", Type = DepartmentType.NonProduction };
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = fixture.Actor.Id, RoleId = role.Id });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await fixture.Repository.GetProductionSchedules(fixture.Actor.Id, 1, 1, null);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(2, result.Value.TotalRecordCount);
        Assert.Single(result.Value.Data);
        var hidden = await fixture.Repository.GetProductionSchedules(fixture.Actor.Id, 1, 50, "Foreign syrup");
        Assert.Empty(hidden.Value.Data);
        Assert.Equal(0, hidden.Value.TotalRecordCount);
        Assert.True((await fixture.Repository.GetProductionSchedule(foreignSchedule.Id, fixture.Actor.Id)).IsFailure);
        var legacy = await fixture.Repository.GetProductionSchedule(mixed.Id, fixture.Actor.Id);
        Assert.Equal(fixture.Batch.ProductId, Assert.Single(legacy.Value.Products).Product.Id);
        Assert.Equal("mg", Assert.Single(legacy.Value.Products).Product.BaseUoM.Symbol);

        // Switch the account in storage, retaining the same role and repository/request context.
        var actor = await db.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == fixture.Actor.Id);
        actor.DepartmentId = fixture.OtherDepartment.Id;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        result = await fixture.Repository.GetProductionSchedules(fixture.Actor.Id, 1, 50, null);
        Assert.Equal(2, result.Value.TotalRecordCount);
        Assert.DoesNotContain(result.Value.Data, row => row.Id == fixture.Schedule.Id);
        legacy = await fixture.Repository.GetProductionSchedule(mixed.Id, fixture.Actor.Id);
        Assert.Equal(foreign.Id, Assert.Single(legacy.Value.Products).Product.Id);
    }

    [Fact]
    public async Task Nonproduction_oversight_retains_crossbranch_visibility_but_missing_department_is_denied()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var quality = new Department { Id = Guid.NewGuid(), Name = "QA", Type = DepartmentType.NonProduction };
        db.Departments.Add(quality);
        fixture.Actor.Department = quality;
        await db.SaveChangesAsync();
        var result = await fixture.Repository.GetProductionSchedules(fixture.Actor.Id, 1, 50, null);
        Assert.Equal(fixture.Schedule.Id, Assert.Single(result.Value.Data).Id);
        fixture.Actor.Department = null;
        fixture.Actor.DepartmentId = null;
        await db.SaveChangesAsync();
        result = await fixture.Repository.GetProductionSchedules(fixture.Actor.Id, 1, 50, null);
        Assert.True(result.IsFailure);
        Assert.True((await fixture.Repository.GetProductionSchedules(Guid.NewGuid(), 1, 50, null)).IsFailure);
    }
}
