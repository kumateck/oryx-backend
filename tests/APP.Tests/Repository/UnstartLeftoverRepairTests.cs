using System.Reflection;
using API.Controllers;
using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace APP.Tests.Repository;

public sealed class UnstartLeftoverRepairTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public RepairActor Actor { get; } = new();
        public ApplicationDbContext Db { get; }
        public UnstartLeftoverRepairService Service { get; }
        public Guid Batch { get; } = Guid.NewGuid();
        public Guid Run { get; } = Guid.NewGuid();
        public Guid Request { get; } = Guid.NewGuid();
        public Guid Role { get; } = Guid.NewGuid();
        public Fixture()
        {
            Db = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),
                options => options.EnableNullChecks(false)).Options, Actor);
            Service = new(Db, Actor);
        }
        public async Task Seed(MaterialKind kind = MaterialKind.Raw)
        {
            Db.Users.Add(new User { Id = Actor.UserId!.Value, DepartmentId = Actor.DepartmentId });
            Db.Roles.Add(new Role { Id = Role });
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.UserId.Value, RoleId = Role });
            Db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = Batch, BatchNumber = "TEST-OLD" });
            var time = DateTime.UtcNow.AddDays(-1);
            Db.ProductionActivities.Add(new ProductionActivity { Id = Run, ProductionScheduleProductId = Batch, DeletedAt = time });
            Db.ProductionUnstartRepairs.Add(new ProductionUnstartRepair { Id = Guid.NewGuid(), ActivityId = Run, BatchId = Batch, AppliedAt = time });
            Db.Requisitions.Add(new Requisition { Id = Request, Code = "UNTRUSTED-PREFIX", DepartmentId = Actor.DepartmentId!.Value,
                RequisitionType = RequisitionType.Stock, ProductionScheduleProductId = Batch, CreatedAt = time.AddHours(-1),
                Items = [new RequisitionItem { Id = Guid.NewGuid(), Material = new Material { Id = Guid.NewGuid(), Kind = kind }, Quantity = 5 }] });
            await Db.SaveChangesAsync();
            (await Db.Requisitions.IgnoreAutoIncludes().SingleAsync()).CreatedAt = time.AddHours(-1);
            await Grant(kind == MaterialKind.Raw ? PermissionKeys.CanViewRawMaterialRequisitions : PermissionKeys.CanViewPackagingMaterialRequisitions);
            Db.ChangeTracker.Clear();
        }
        public async Task Grant(string permission)
        {
            var id = Db.RoleClaims.Local.Count + 1;
            Db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = id, RoleId = Role, ClaimType = AppConstants.Permission, ClaimValue = permission });
            Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = id, Key = permission, Type = "Action" });
            await Db.SaveChangesAsync();
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    [Theory]
    [InlineData("PreviewRaw", PermissionKeys.CanViewRawMaterialRequisitions)]
    [InlineData("PreviewPackaging", PermissionKeys.CanViewPackagingMaterialRequisitions)]
    [InlineData("RepairRaw", PermissionKeys.CanRepairUnstartRawMaterialRequisitions)]
    [InlineData("RepairPackaging", PermissionKeys.CanRepairUnstartPackagingMaterialRequisitions)]
    public void Endpoints_have_specific_policies(string action, string key)
    {
        var method = typeof(UnstartLeftoversController).GetMethod(action)!;
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(MaterialKind.Raw)]
    [InlineData(MaterialKind.Package)]
    public async Task Preview_finds_old_unissued_requests_by_material_kind_and_preserves_them(MaterialKind kind)
    {
        await using var f = new Fixture(); await f.Seed(kind);
        var result = await f.Service.Preview(kind);
        Assert.True(result.IsSuccess, result.Error?.Description);
        var row = Assert.Single(result.Value.Requisitions);
        Assert.Equal(f.Request, row.RequisitionId); Assert.Empty(row.Blockers);
        Assert.Equal(64, result.Value.PreviewToken.Length);
        Assert.Null((await f.Db.Requisitions.SingleAsync()).DeletedAt);
    }

    [Fact]
    public async Task Wrong_variant_and_revoked_grants_are_denied()
    {
        await using var f = new Fixture(); await f.Seed();
        Assert.True((await f.Service.Preview(MaterialKind.Package)).IsFailure);
        f.Db.RoleClaims.RemoveRange(f.Db.RoleClaims); await f.Db.SaveChangesAsync();
        Assert.True((await f.Service.Preview(MaterialKind.Raw)).IsFailure);
    }

    [Fact]
    public async Task Apply_requires_the_dedicated_repair_permission_before_any_transaction_or_write()
    {
        await using var f = new Fixture(); await f.Seed();
        var preview = await f.Service.Preview(MaterialKind.Raw);
        var result = await f.Service.Apply(MaterialKind.Raw, new() { RequestId = Guid.NewGuid(), PreviewToken = preview.Value.PreviewToken,
            RequisitionIds = [f.Request], Reason = "Historical Unstart leftovers" });
        Assert.True(result.IsFailure); Assert.Equal("UnstartLeftovers.Permission", result.Error.Code);
        Assert.Null((await f.Db.Requisitions.SingleAsync()).DeletedAt);
    }

    [Theory]
    [InlineData("restarted")]
    [InlineData("received")]
    [InlineData("issued")]
    [InlineData("reservation")]
    public async Task Unsafe_requests_are_visible_with_blockers(string condition)
    {
        await using var f = new Fixture(); await f.Seed();
        if (condition == "restarted") f.Db.ProductionActivities.Add(new() { Id = Guid.NewGuid(), ProductionScheduleProductId = f.Batch });
        if (condition == "received") (await f.Db.RequisitionItems.SingleAsync()).QuantityReceived = 1;
        if (condition == "issued") f.Db.StockRequisitionIssues.Add(new() { Id = Guid.NewGuid(), RequisitionId = f.Request, DeletedAt = DateTime.UtcNow });
        if (condition == "reservation") f.Db.MaterialBatchReservedQuantities.Add(new() { Id = Guid.NewGuid(), ProductionScheduleProductId = f.Batch });
        await f.Db.SaveChangesAsync();
        Assert.NotEmpty(Assert.Single((await f.Service.Preview(MaterialKind.Raw)).Value.Requisitions).Blockers);
    }

    [Theory]
    [InlineData("newer")]
    [InlineData("department")]
    [InlineData("deleted")]
    [InlineData("no-receipt")]
    public async Task Newer_unrelated_and_already_cancelled_requests_are_excluded(string condition)
    {
        await using var f = new Fixture(); await f.Seed();
        var r = await f.Db.Requisitions.SingleAsync();
        if (condition == "newer") r.CreatedAt = DateTime.UtcNow;
        if (condition == "department") r.DepartmentId = Guid.NewGuid();
        if (condition == "deleted") r.DeletedAt = DateTime.UtcNow;
        if (condition == "no-receipt") r.ProductionScheduleProductId = Guid.NewGuid();
        await f.Db.SaveChangesAsync();
        Assert.Empty((await f.Service.Preview(MaterialKind.Raw)).Value.Requisitions);
    }

    [RepairPostgresFact]
    public async Task Postgres_repair_migration_cancels_once_preserves_stock_and_refuses_stale_evidence()
    {
        foreach (var kind in new[] { MaterialKind.Raw, MaterialKind.Package })
        {
            await using var f = new IssuedMaterialRepairFixture();
            await f.Seed(kind == MaterialKind.Raw ? IssuedMaterialRepairKind.Bom : IssuedMaterialRepairKind.Packaging, issueStock: false);
            await using var db = f.Open();
            // Exercise the deployment migration, including append-only database guards.
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"UnstartLeftoverRepairs\"");
            var migration = new INFRASTRUCTURE.Migrations.AddUnstartLeftoverRepairs();
            foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
            var materials = await db.Materials.Select(material => material.Id).ToListAsync();
            db.Requisitions.Add(new Requisition { Id = f.RequisitionId, Code = "OLD/LEFTOVER", DepartmentId = f.Actor.DepartmentId!.Value,
                RequestedById = f.Actor.UserId!.Value, ProductionScheduleProductId = f.BatchId, RequisitionType = RequisitionType.Stock,
                Status = RequestStatus.Pending, Items = materials.Select(id => new RequisitionItem { Id = Guid.NewGuid(), MaterialId = id, UoMId = f.UnitId, Quantity = 5 }).ToList() });
            await db.SaveChangesAsync();
            var time = DateTime.UtcNow.AddMinutes(-1);
            var run = await db.ProductionActivities.SingleAsync();
            run.DeletedAt = time;
            foreach (var reservation in await db.MaterialBatchReservedQuantities.ToListAsync()) { reservation.ReleasedAt = time; reservation.DeletedAt = time; }
            var r = await db.Requisitions.IgnoreAutoIncludes().SingleAsync();
            r.CreatedAt = time.AddHours(-1);
            var receipt = new ProductionUnstartRepair { Id = Guid.NewGuid(), ActivityId = run.Id, BatchId = f.BatchId,
                ActorId = f.Actor.UserId!.Value, AppliedAt = time };
            db.ProductionUnstartRepairs.Add(receipt);
            var keys = kind == MaterialKind.Raw
                ? new[] { PermissionKeys.CanViewRawMaterialRequisitions, PermissionKeys.CanRepairUnstartRawMaterialRequisitions }
                : new[] { PermissionKeys.CanViewPackagingMaterialRequisitions, PermissionKeys.CanRepairUnstartPackagingMaterialRequisitions };
            for (var i = 0; i < keys.Length; i++)
            {
                db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = 20 + i, RoleId = f.RoleId, ClaimType = AppConstants.Permission, ClaimValue = keys[i] });
                db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = 20 + i, Key = keys[i], Type = "Action" });
            }
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var service = new UnstartLeftoverRepairService(db, f.Actor);
            var preview = await service.Preview(kind);
            Assert.True(preview.IsSuccess, preview.Error?.Description);
            Assert.Empty(Assert.Single(preview.Value.Requisitions).Blockers);
            var request = new RepairUnstartLeftoversRequest { RequestId = Guid.NewGuid(), PreviewToken = preview.Value.PreviewToken,
                RequisitionIds = [f.RequisitionId], Reason = "Remove a historical Unstart leftover" };
            // A competing received quantity makes the reviewed plan stale and cannot be cancelled.
            var line = await db.RequisitionItems.FirstAsync(); line.QuantityReceived = 1; await db.SaveChangesAsync();
            var stale = await service.Apply(kind, request);
            Assert.True(stale.IsFailure); Assert.Equal("UnstartLeftovers.Stale", stale.Error.Code);
            line.QuantityReceived = 0; await db.SaveChangesAsync();
            request.PreviewToken = (await service.Preview(kind)).Value.PreviewToken;
            var shelves = await db.ShelfMaterialBatches.AsNoTracking().Select(shelf => shelf.Quantity).ToListAsync();
            var result = await service.Apply(kind, request);
            Assert.True(result.IsSuccess, result.Error?.Description); Assert.Equal(1, result.Value.CancelledCount);
            Assert.Empty(await db.Requisitions.ToListAsync()); Assert.Empty(await db.RequisitionItems.ToListAsync());
            Assert.Equal(shelves, await db.ShelfMaterialBatches.AsNoTracking().Select(shelf => shelf.Quantity).ToListAsync());
            Assert.True((await service.Apply(kind, request)).Value.AlreadyApplied);
            Assert.Single(await db.UnstartLeftoverRepairs.ToListAsync());
            Assert.NotNull((await db.Requisitions.IgnoreQueryFilters().IgnoreAutoIncludes().SingleAsync()).DeletedAt);
            var sql = "UPDATE \"UnstartLeftoverRepairs\" SET \"Reason\" = 'tampered'";
            await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync(sql));
        }
    }
}
