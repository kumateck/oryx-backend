using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using INFRASTRUCTURE.Migrations;
using Xunit;

namespace APP.Tests.Repository;

public sealed class ProductionBoardOrderTests
{
    [RepairPostgresFact]
    public async Task PostgreSql_saves_the_department_order_atomically_and_rejects_stale_versions()
    {
        await using var f = new IssuedMaterialRepairFixture();
        await f.Seed(issueStock: false);
        await using var db = f.Open();
        await SharedDensityTransferTests.Grant(db, f.Actor.UserId!.Value,
            PermissionKeys.CanViewDepartmentProductionBoardOrder, PermissionKeys.CanReorderDepartmentProductionBoard);
        var first = new Operation { Name = "Board first", DepartmentId = f.Actor.DepartmentId, Order = 0 };
        var second = new Operation { Name = "Board second", DepartmentId = f.Actor.DepartmentId, Order = 7 };
        db.AddRange(first, second); await db.SaveChangesAsync();
        var repository = new CollectionRepository(db, null!);
        var preview = (await repository.GetProductionBoardOrder(f.Actor.DepartmentId!.Value, f.Actor.UserId.Value)).Value;
        var ids = preview.Operations.Select(row => row.Id).Reverse().ToList();
        var result = await repository.SaveProductionBoardOrder(f.Actor.DepartmentId.Value, new(preview.Version, ids), f.Actor.UserId.Value);
        Assert.True(result.IsSuccess, result.Error.Description);
        await using var verified = f.Open();
        Assert.Equal(ids, await verified.Operations.Where(row => row.DepartmentId == f.Actor.DepartmentId).OrderBy(row => row.Order).Select(row => row.Id).ToListAsync());
        Assert.Equal(Enumerable.Range(1, ids.Count), await verified.Operations.Where(row => row.DepartmentId == f.Actor.DepartmentId).OrderBy(row => row.Order).Select(row => row.Order).ToListAsync());
        Assert.Equal("BoardOrder.Changed", (await repository.SaveProductionBoardOrder(f.Actor.DepartmentId.Value, new(preview.Version, ids), f.Actor.UserId.Value)).Error.Code);
    }

    [RepairPostgresFact]
    public async Task Migration_copies_existing_operation_grants_with_their_access_types_once()
    {
        await using var f = new IssuedMaterialRepairFixture();
        await f.Seed(issueStock: false);
        await using var db = f.Open();
        await SharedDensityTransferTests.Grant(db, f.Actor.UserId!.Value,
            PermissionKeys.CanViewOperations, PermissionKeys.CanEditOperation);
        await db.Database.ExecuteSqlRawAsync("SELECT setval(pg_get_serial_sequence('roleclaims', 'Id'), (SELECT MAX(\"Id\") FROM \"roleclaims\"))");
        var generator = db.GetService<IMigrationsSqlGenerator>();
        for (var run = 0; run < 2; run++)
            foreach (var command in generator.Generate(new GrantProductionBoardOrdering().UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
        foreach (var (targetKey, sourceKey) in new[] {
            (PermissionKeys.CanViewDepartmentProductionBoardOrder, PermissionKeys.CanViewOperations),
            (PermissionKeys.CanReorderDepartmentProductionBoard, PermissionKeys.CanEditOperation) })
        {
            var source = await db.RoleClaims.AsNoTracking().SingleAsync(row => row.ClaimValue == sourceKey);
            var target = await db.RoleClaims.AsNoTracking().SingleAsync(row => row.ClaimValue == targetKey);
            Assert.Equal(source.RoleId, target.RoleId);
            Assert.Equal(await db.PermissionTypes.Where(row => row.RoleClaimId == source.Id).Select(row => row.Type).OrderBy(type => type).ToListAsync(),
                await db.PermissionTypes.Where(row => row.RoleClaimId == target.Id).Select(row => row.Type).OrderBy(type => type).ToListAsync());
        }
    }

    [Theory]
    [InlineData(nameof(ProductionBoardOrderController.Get), PermissionKeys.CanViewDepartmentProductionBoardOrder)]
    [InlineData(nameof(ProductionBoardOrderController.Save), PermissionKeys.CanReorderDepartmentProductionBoard)]
    public void Every_endpoint_names_its_one_registered_task_permission(string name, string key)
    {
        var method = typeof(ProductionBoardOrderController).GetMethod(name)!;
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Single(PermissionUtils.GeneratePermissions(), row => row.Key == key);
    }

    [Fact]
    public async Task Saves_a_complete_department_order_and_preserves_batch_step_order()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        var first = new Operation { Name = "Product Preparation", DepartmentId = f.Department.Id, Order = 5 };
        var second = new Operation { Name = "Line clearance for dispensing", DepartmentId = f.Department.Id, Order = 0 };
        var other = new Operation { Name = "Other department", DepartmentId = f.OtherDepartment.Id, Order = 1 };
        var activity = new ProductionActivity { ProductionScheduleProductId = f.Batch.Id,
            Steps = [new() { Operation = first, Order = 7 }, new() { Operation = second, Order = 3 }] };
        db.AddRange(first, second, other, activity); await db.SaveChangesAsync();
        var repository = new CollectionRepository(db, null!);
        Assert.True((await repository.GetProductionBoardOrder(f.Department.Id, f.Actor.Id)).IsFailure);
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanViewDepartmentProductionBoardOrder);
        var preview = await repository.GetProductionBoardOrder(f.Department.Id, f.Actor.Id);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        Assert.Equal(new[] { second.Id, first.Id }, preview.Value.Operations.Select(row => row.Id));
        var request = new SaveProductionBoardOrder(preview.Value.Version, [first.Id, second.Id]);
        Assert.True((await repository.SaveProductionBoardOrder(f.Department.Id, request, f.Actor.Id)).IsFailure);
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanReorderDepartmentProductionBoard);
        Assert.True((await repository.SaveProductionBoardOrder(f.Department.Id, request, f.Actor.Id)).IsSuccess);
        Assert.Equal(1, first.Order); Assert.Equal(2, second.Order); Assert.Equal(1, other.Order);
        Assert.Equal(new[] { 3, 7 }, await db.ProductionActivitySteps.Where(row => row.ProductionActivityId == activity.Id).OrderBy(row => row.Order).Select(row => row.Order).ToArrayAsync());
        Assert.Equal("BoardOrder.Changed", (await repository.SaveProductionBoardOrder(f.Department.Id, request, f.Actor.Id)).Error.Code);
    }

    [Fact]
    public async Task Rejects_missing_duplicate_and_foreign_operations_without_partial_reorder()
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        var own = new Operation { Name = "Own", DepartmentId = f.Department.Id, Order = 9 };
        var foreign = new Operation { Name = "Foreign", DepartmentId = f.OtherDepartment.Id, Order = 4 };
        db.AddRange(own, foreign); await db.SaveChangesAsync();
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanViewDepartmentProductionBoardOrder, PermissionKeys.CanReorderDepartmentProductionBoard);
        var repository = new CollectionRepository(db, null!);
        var preview = (await repository.GetProductionBoardOrder(f.Department.Id, f.Actor.Id)).Value;
        foreach (var ids in new List<Guid>[] { [], [own.Id, own.Id], [foreign.Id] })
            Assert.True((await repository.SaveProductionBoardOrder(f.Department.Id, new(preview.Version, ids), f.Actor.Id)).IsFailure);
        Assert.Equal(9, own.Order); Assert.Equal(4, foreign.Order);
        own.DepartmentId = f.OtherDepartment.Id; await db.SaveChangesAsync();
        Assert.Equal("BoardOrder.Changed", (await repository.SaveProductionBoardOrder(f.Department.Id, new(preview.Version, [own.Id]), f.Actor.Id)).Error.Code);
    }
}
