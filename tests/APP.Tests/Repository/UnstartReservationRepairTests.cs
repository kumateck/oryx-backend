using System.Reflection;
using System.Text.Json;
using API.Controllers;
using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace APP.Tests.Repository;

public sealed class UnstartReservationRepairTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public RepairActor Actor { get; } = new();
        public ApplicationDbContext Db { get; }
        public UnstartReservationRepairService Service { get; }
        public Guid Material { get; } = Guid.NewGuid();
        public Guid Batch { get; } = Guid.NewGuid();
        public Guid Reservation { get; } = Guid.NewGuid();
        public Guid Role { get; } = Guid.NewGuid();
        public Fixture()
        {
            Db = new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),
                options => options.EnableNullChecks(false)).Options, Actor);
            Service = new(Db, Actor);
        }
        public async Task Seed(MaterialKind kind = MaterialKind.Raw, bool proof = true)
        {
            Db.Users.Add(new User { Id = Actor.UserId!.Value, DepartmentId = Actor.DepartmentId });
            Db.Roles.Add(new Role { Id = Role });
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.UserId.Value, RoleId = Role });
            var product = new Product { Id = Guid.NewGuid(), Name = "Product", DepartmentId = Actor.DepartmentId };
            Db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = Batch, Product = product, BatchNumber = "OLD-001" });
            var time = DateTime.UtcNow.AddDays(-1);
            var run = new ProductionActivity { Id = Guid.NewGuid(), ProductionScheduleProductId = Batch, DeletedAt = time };
            Db.ProductionActivities.Add(run);
            Db.ProductionUnstartRepairs.Add(new ProductionUnstartRepair { Id = Guid.NewGuid(), ActivityId = run.Id, BatchId = Batch, AppliedAt = time,
                BeforeJson = proof ? JsonSerializer.Serialize(new { Reservations = new[] { new { Id = Reservation } } }) : "{}" });
            var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "mg" };
            Db.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity { Id = Reservation, ProductionScheduleProductId = Batch,
                Warehouse = new Warehouse { Id = Guid.NewGuid(), DepartmentId = Actor.DepartmentId, Type = WarehouseType.Production },
                UoM = unit, Quantity = 50, MaterialBatch = new MaterialBatch { Id = Guid.NewGuid(), UoM = unit, BatchNumber = "LOT-1",
                    Material = new Material { Id = Material, Name = "Material", Kind = kind } } });
            await Db.SaveChangesAsync();
            (await Db.MaterialBatchReservedQuantities.IgnoreQueryFilters().SingleAsync()).CreatedAt = time.AddHours(-1);
            await Grant(kind == MaterialKind.Raw ? PermissionKeys.CanViewApprovedRawMaterials : PermissionKeys.CanViewApprovedPackagingMaterials);
            Db.ChangeTracker.Clear();
        }
        public async Task Grant(string key)
        {
            var id = await Db.RoleClaims.CountAsync() + 1;
            Db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = id, RoleId = Role, ClaimType = AppConstants.Permission, ClaimValue = key });
            Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = id, Key = key, Type = "Action" });
            await Db.SaveChangesAsync();
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    [Theory]
    [InlineData("PreviewRaw", PermissionKeys.CanViewApprovedRawMaterials)]
    [InlineData("PreviewPackaging", PermissionKeys.CanViewApprovedPackagingMaterials)]
    [InlineData("ReleaseRaw", PermissionKeys.CanRepairUnstartRawMaterialReservations)]
    [InlineData("ReleasePackaging", PermissionKeys.CanRepairUnstartPackagingMaterialReservations)]
    public void Endpoint_names_one_specific_permission(string action, string key)
    {
        var method = typeof(UnstartReservationsController).GetMethod(action)!;
        Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(MaterialKind.Raw)]
    [InlineData(MaterialKind.Package)]
    public async Task Preview_proves_the_old_reservation_and_apply_needs_the_dedicated_key(MaterialKind kind)
    {
        await using var f = new Fixture(); await f.Seed(kind);
        var preview = await f.Service.Preview(f.Material, kind);
        Assert.True(preview.IsSuccess, preview.Error?.Description);
        Assert.Empty(Assert.Single(preview.Value.Reservations).Blockers);
        var denied = await f.Service.Apply(f.Material, kind, new() { RequestId = Guid.NewGuid(), PreviewToken = preview.Value.PreviewToken,
            Reason = "Release old Unstart hold", ReservationIds = [f.Reservation] });
        Assert.True(denied.IsFailure); Assert.Equal("UnstartReservations.Permission", denied.Error.Code);
        Assert.Null((await f.Db.MaterialBatchReservedQuantities.IgnoreQueryFilters().SingleAsync()).ReleasedAt);
    }

    [Fact]
    public async Task Wrong_variant_and_revoked_view_are_denied()
    {
        await using var f = new Fixture(); await f.Seed();
        Assert.True((await f.Service.Preview(f.Material, MaterialKind.Package)).IsFailure);
        f.Db.RoleClaims.RemoveRange(f.Db.RoleClaims); await f.Db.SaveChangesAsync();
        Assert.True((await f.Service.Preview(f.Material, MaterialKind.Raw)).IsFailure);
    }

    [Theory]
    [InlineData("restarted")]
    [InlineData("issued")]
    [InlineData("consumed")]
    [InlineData("no-snapshot")]
    [InlineData("invalid-unit")]
    public async Task Preview_blocks_unsafe_or_unproven_holds(string condition)
    {
        await using var f = new Fixture(); await f.Seed(proof: condition != "no-snapshot");
        var r = await f.Db.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes().SingleAsync();
        if (condition == "restarted") f.Db.ProductionActivities.Add(new() { Id = Guid.NewGuid(), ProductionScheduleProductId = f.Batch });
        if (condition == "issued")
        {
            var request = Guid.NewGuid();
            f.Db.Requisitions.Add(new() { Id = request, ProductionScheduleProductId = f.Batch });
            f.Db.StockRequisitionIssues.Add(new() { Id = Guid.NewGuid(), RequisitionId = request, DeletedAt = DateTime.UtcNow });
        }
        if (condition == "consumed") r.DeletedAt = DateTime.UtcNow;
        if (condition == "invalid-unit") {
            var wrongUnit = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "ml" };
            f.Db.UnitOfMeasures.Add(wrongUnit); r.UoM = wrongUnit;
        }
        await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
        var result = await f.Service.Preview(f.Material, MaterialKind.Raw);
        Assert.True(result.IsSuccess, result.Error?.Description);
        Assert.NotEmpty(Assert.Single(result.Value.Reservations).Blockers);
    }

    [Fact]
    public async Task Released_holds_are_excluded_from_active_queries_even_without_a_deletion_timestamp()
    {
        await using var f = new Fixture(); await f.Seed();
        var r = await f.Db.MaterialBatchReservedQuantities.SingleAsync(); r.ReleasedAt = DateTime.UtcNow; await f.Db.SaveChangesAsync();
        Assert.Empty(await f.Db.MaterialBatchReservedQuantities.ToListAsync());
        Assert.Empty((await f.Service.Preview(f.Material, MaterialKind.Raw)).Value.Reservations);
    }

    [RepairPostgresFact]
    public async Task Postgres_release_is_atomic_idempotent_audited_and_preserves_shelves_and_qc_state()
    {
        foreach (var kind in new[] { MaterialKind.Raw, MaterialKind.Package })
        {
            await using var f = new IssuedMaterialRepairFixture();
            await f.Seed(kind == MaterialKind.Raw ? IssuedMaterialRepairKind.Bom : IssuedMaterialRepairKind.Packaging, issueStock: false);
            await using var db = f.Open();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE \"UnstartReservationRepairs\"");
            var migration = new INFRASTRUCTURE.Migrations.AddUnstartReservationRepairs();
            foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
            var time = DateTime.UtcNow.AddMinutes(-1);
            var run = await db.ProductionActivities.SingleAsync(); run.DeletedAt = time;
            var reservations = await db.MaterialBatchReservedQuantities.ToListAsync();
            foreach (var r in reservations) r.CreatedAt = time.AddHours(-1);
            db.ProductionUnstartRepairs.Add(new() { Id = Guid.NewGuid(), ActorId = f.Actor.UserId!.Value, ActivityId = run.Id, BatchId = f.BatchId,
                AppliedAt = time, BeforeJson = JsonSerializer.Serialize(new { Reservations = reservations.Select(r => new { r.Id }) }) });
            var keys = kind == MaterialKind.Raw ? new[] { PermissionKeys.CanViewApprovedRawMaterials, PermissionKeys.CanRepairUnstartRawMaterialReservations }
                : new[] { PermissionKeys.CanViewApprovedPackagingMaterials, PermissionKeys.CanRepairUnstartPackagingMaterialReservations };
            for (var i = 0; i < keys.Length; i++)
            {
                db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = 30 + i, RoleId = f.RoleId, ClaimType = AppConstants.Permission, ClaimValue = keys[i] });
                db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = 30 + i, Key = keys[i], Type = "Action" });
            }
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            var r0 = await db.MaterialBatchReservedQuantities.Include(r => r.MaterialBatch).FirstAsync();
            var materialId = r0.MaterialBatch.MaterialId;
            var service = new UnstartReservationRepairService(db, f.Actor);
            var preview = await service.Preview(materialId, kind);
            Assert.Empty(Assert.Single(preview.Value.Reservations).Blockers);
            var request = new ReleaseUnstartReservationsRequest { RequestId = Guid.NewGuid(), PreviewToken = preview.Value.PreviewToken,
                ReservationIds = [r0.Id], Reason = "Release unused hold from historical Unstart" };
            r0.Quantity += 1; await db.SaveChangesAsync();
            var stale = await service.Apply(materialId, kind, request);
            Assert.True(stale.IsFailure); Assert.Equal("UnstartReservations.Stale", stale.Error.Code);
            request.PreviewToken = (await service.Preview(materialId, kind)).Value.PreviewToken;
            var shelves = await db.ShelfMaterialBatches.AsNoTracking().Select(s => s.Quantity).ToListAsync();
            var statuses = await db.MaterialBatches.AsNoTracking().Select(b => b.Status).ToListAsync();
            var result = await service.Apply(materialId, kind, request);
            Assert.True(result.IsSuccess, result.Error?.Description); Assert.Equal(1, result.Value.ReleasedCount);
            Assert.True((await service.Apply(materialId, kind, request)).Value.AlreadyApplied);
            Assert.Empty((await service.Preview(materialId, kind)).Value.Reservations);
            Assert.Equal(shelves, await db.ShelfMaterialBatches.AsNoTracking().Select(s => s.Quantity).ToListAsync());
            Assert.Equal(statuses, await db.MaterialBatches.AsNoTracking().Select(b => b.Status).ToListAsync());
            Assert.Single(await db.UnstartReservationRepairs.ToListAsync());
            await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync("UPDATE \"UnstartReservationRepairs\" SET \"Reason\" = 'changed'"));
        }
    }
}
