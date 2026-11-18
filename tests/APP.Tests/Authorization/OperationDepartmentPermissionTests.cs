using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Linking an operation to a production department and unlinking it are separate tasks with their own keys.</summary>
public sealed class OperationDepartmentPermissionTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static ApplicationDbContext Db() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    private static IMapper Mapper(ApplicationDbContext db)
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddHttpContextAccessor(); services.AddSingleton(db);
        services.AddAutoMapper(configuration => { }, typeof(APP.Mapper.OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private sealed record Seeded(Guid UserId, Department Production, Department Other, Operation Operation);

    private static async Task<Seeded> Seed(ApplicationDbContext db, params string[] keys)
    {
        var production = new Department { Id = Guid.NewGuid(), Name = "General production", Type = DepartmentType.Production };
        var other = new Department { Id = Guid.NewGuid(), Name = "Quality assurance", Type = DepartmentType.NonProduction };
        var operation = new Operation { Id = Guid.NewGuid(), Name = "Filling and Capping", Description = "Filling" };
        var user = new User { Id = Guid.NewGuid(), UserName = "admin", FirstName = "Ada", LastName = "Admin" };
        db.AddRange(production, other, operation, user);
        var role = new Role { Id = Guid.NewGuid(), Name = "Settings" };
        db.Roles.Add(role);
        db.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = role.Id, UserId = user.Id });
        foreach (var key in keys)
        {
            var claim = new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
            db.RoleClaims.Add(claim); await db.SaveChangesAsync();
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id, Key = key, Type = "Edit" });
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return new Seeded(user.Id, production, other, operation);
    }

    private static async Task<Guid?> LinkedDepartment(ApplicationDbContext db, Guid operationId)
    {
        db.ChangeTracker.Clear();
        return await db.Operations.Where(item => item.Id == operationId).Select(item => item.DepartmentId).SingleAsync();
    }

    [Theory]
    [InlineData(nameof(CollectionController.LinkOperationDepartment), PermissionKeys.CanLinkOperationDepartment)]
    [InlineData(nameof(CollectionController.UnlinkOperationDepartment), PermissionKeys.CanUnlinkOperationDepartment)]
    public void Each_endpoint_needs_its_own_key(string action, string key)
    {
        var method = typeof(CollectionController).GetMethod(action)!;
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(PermissionKeys.CanLinkOperationDepartment)]
    [InlineData(PermissionKeys.CanUnlinkOperationDepartment)]
    public void Each_key_is_registered_once(string key) =>
        Assert.Single(PermissionUtils.GeneratePermissions(),
            permission => permission.Key == key);

    [Fact]
    public async Task Linking_without_the_link_key_is_refused()
    {
        await using var db = Db();
        var seeded = await Seed(db, PermissionKeys.CanEditOperation, PermissionKeys.CanUnlinkOperationDepartment);
        var result = await new CollectionRepository(db, Mapper(db)).LinkOperationDepartment(seeded.Operation.Id, seeded.Production.Id, seeded.UserId);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Errors.Single().Type);
        Assert.Null(await LinkedDepartment(db, seeded.Operation.Id));
    }

    [Fact]
    public async Task Linking_with_the_key_sets_the_department_and_the_list_shows_it()
    {
        await using var db = Db();
        var seeded = await Seed(db, PermissionKeys.CanLinkOperationDepartment);
        var repository = new CollectionRepository(db, Mapper(db));
        var result = await repository.LinkOperationDepartment(seeded.Operation.Id, seeded.Production.Id, seeded.UserId);
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors.Select(error => error.Description)));
        Assert.Equal(seeded.Production.Id, await LinkedDepartment(db, seeded.Operation.Id));

        var list = await repository.GetItemCollection(nameof(Operation), null);
        var row = Assert.Single(list.Value);
        Assert.Equal(seeded.Production.Id, row.LinkedDepartmentId);
        Assert.Equal("General production", row.LinkedDepartmentName);
    }

    [Fact]
    public async Task Only_production_departments_can_be_linked()
    {
        await using var db = Db();
        var seeded = await Seed(db, PermissionKeys.CanLinkOperationDepartment);
        var result = await new CollectionRepository(db, Mapper(db)).LinkOperationDepartment(seeded.Operation.Id, seeded.Other.Id, seeded.UserId);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Errors.Single().Type);
        Assert.Null(await LinkedDepartment(db, seeded.Operation.Id));
    }

    [Fact]
    public async Task A_linked_operation_must_be_unlinked_before_moving()
    {
        await using var db = Db();
        var seeded = await Seed(db, PermissionKeys.CanLinkOperationDepartment);
        var second = new Department { Id = Guid.NewGuid(), Name = "Beta-lactam production", Type = DepartmentType.Production };
        db.Departments.Add(second);
        db.Operations.Single(item => item.Id == seeded.Operation.Id).DepartmentId = seeded.Production.Id;
        await db.SaveChangesAsync();
        var result = await new CollectionRepository(db, Mapper(db)).LinkOperationDepartment(seeded.Operation.Id, second.Id, seeded.UserId);
        Assert.Equal(ErrorType.Conflict, result.Errors.Single().Type);
        Assert.Equal(seeded.Production.Id, await LinkedDepartment(db, seeded.Operation.Id));
    }

    [Fact]
    public async Task A_department_cannot_hold_two_operations_with_the_same_name()
    {
        await using var db = Db();
        var seeded = await Seed(db, PermissionKeys.CanLinkOperationDepartment);
        db.Operations.Add(new Operation { Id = Guid.NewGuid(), Name = " filling and capping ", DepartmentId = seeded.Production.Id });
        await db.SaveChangesAsync();
        var result = await new CollectionRepository(db, Mapper(db)).LinkOperationDepartment(seeded.Operation.Id, seeded.Production.Id, seeded.UserId);
        Assert.Equal(ErrorType.Conflict, result.Errors.Single().Type);
    }

    [Fact]
    public async Task Unlinking_needs_the_unlink_key()
    {
        await using var db = Db();
        var seeded = await Seed(db, PermissionKeys.CanLinkOperationDepartment);
        db.Operations.Single(item => item.Id == seeded.Operation.Id).DepartmentId = seeded.Production.Id;
        await db.SaveChangesAsync();
        var denied = await new CollectionRepository(db, Mapper(db)).UnlinkOperationDepartment(seeded.Operation.Id, seeded.UserId);
        Assert.Equal(ErrorType.Forbidden, denied.Errors.Single().Type);
        Assert.Equal(seeded.Production.Id, await LinkedDepartment(db, seeded.Operation.Id));
    }

    [Fact]
    public async Task Unlinking_with_the_key_clears_the_department()
    {
        await using var db = Db();
        var seeded = await Seed(db, PermissionKeys.CanUnlinkOperationDepartment);
        db.Operations.Single(item => item.Id == seeded.Operation.Id).DepartmentId = seeded.Production.Id;
        await db.SaveChangesAsync();
        var result = await new CollectionRepository(db, Mapper(db)).UnlinkOperationDepartment(seeded.Operation.Id, seeded.UserId);
        Assert.True(result.IsSuccess);
        Assert.Null(await LinkedDepartment(db, seeded.Operation.Id));
    }
}
