using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.StockMigrations;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class StockMigrationMaterialLinkTests
{
    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_MATERIAL_LINK_TEST_CONNECTION")))
                Skip = "Set ORYX_MATERIAL_LINK_TEST_CONNECTION to a disposable PostgreSQL server with CREATE DATABASE.";
        }
    }

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? DepartmentId { get; } = Guid.NewGuid();
        public string DepartmentType => string.Empty;
    }

    [Fact]
    public void Endpoints_require_correction_and_the_specific_material_link_keys()
    {
        foreach (var name in new[] { nameof(StockMigrationController.PreviewMaterialLinks), nameof(StockMigrationController.CreateMaterialLinks) })
        {
            var method = typeof(StockMigrationController).GetMethod(name)!;
            Assert.DoesNotContain(method.GetCustomAttributes(), a => a is AllowAnonymousAttribute);
            Assert.Equal(PermissionKeys.CanCorrectOpeningBalances, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
            var kinds = Assert.Single(method.GetCustomAttributes<MaterialKindPermissionAttribute>());
            Assert.Equal(PermissionKeys.CanLinkRawMaterials, kinds.RawKey);
            Assert.Equal(PermissionKeys.CanLinkPackagingMaterials, kinds.PackagingKey);
        }
        foreach (var key in new[] { PermissionKeys.CanCorrectOpeningBalances, PermissionKeys.CanLinkRawMaterials, PermissionKeys.CanLinkPackagingMaterials })
            Assert.Single(PermissionUtils.GeneratePermissions(), p => p.Key == key);
    }

    [PostgresFact]
    public async Task Links_are_draft_only_permission_checked_create_only_and_rechecked_in_smallest_units()
    {
        var database = "oryx_link_test_" + Guid.NewGuid().ToString("N");
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_MATERIAL_LINK_TEST_CONNECTION"));
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin)) await create.ExecuteNonQueryAsync();
        connection.Database = database;
        try
        {
            var actor = new Actor();
            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options, actor);
            await db.Database.EnsureCreatedAsync();
            var department = new Department { Id = actor.DepartmentId!.Value, Name = "Link test", Type = DepartmentType.Production };
            var user = new User { Id = actor.UserId!.Value, UserName = "link-test", DepartmentId = department.Id };
            var role = new Role { Id = Guid.NewGuid(), Name = "test", DisplayName = "test" };
            db.Departments.Add(department);
            db.Users.Add(user);
            db.Roles.Add(role);
            db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id });
            var claimId = 8000;
            void Grant(string key)
            {
                var claim = new IdentityRoleClaim<Guid> { Id = ++claimId, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
                db.RoleClaims.Add(claim);
                db.PermissionTypes.Add(new PermissionType { Key = key, RoleClaimId = claim.Id, Type = "Access" });
            }
            Grant(PermissionKeys.CanViewOpeningBalances);
            Grant(PermissionKeys.CanCorrectOpeningBalances);
            Grant(PermissionKeys.CanLinkRawMaterials);
            var mass = new Material { Code = "RM1", Name = "Powder", Kind = MaterialKind.Raw };
            var liquid = new Material { Code = "RM2", Name = "Liquid", Kind = MaterialKind.Raw };
            var pieces = new Material { Code = "PM1", Name = "Foil", Kind = MaterialKind.Package };
            db.Materials.AddRange(mass, liquid, pieces);
            var mg = new UnitOfMeasure { Name = "Milligram", Symbol = "mg" };
            var ml = new UnitOfMeasure { Name = "Milliliter", Symbol = "ml" };
            var piece = new UnitOfMeasure { Name = "Piece", Symbol = "piece" };
            db.UnitOfMeasures.AddRange(mg, ml, piece);
            StockMigrationLine Row(Material material, string unit) => new()
            {
                MaterialCode = material.Code, UnitSymbol = unit, QuantityInSheet = 1, BatchNumber = "B1", Sheet = "Stock", RowNumber = 2,
            };
            var migration = new StockMigration { DepartmentId = department.Id, MassUnit = "mg", VolumeUnit = "ml", Lines = [Row(mass, "mg"), Row(mass, "mg"), Row(liquid, "ml"), Row(pieces, "PCS")] };
            db.StockMigrations.Add(migration);
            await db.SaveChangesAsync();
            var repository = new StockMigrationRepository(db);
            async Task<List<StockMigrationMaterialLinkDto>> Preview(MaterialKind kind)
            {
                var preview = await repository.PreviewMaterialLinks(migration.Id, kind, user.Id);
                Assert.True(preview.IsSuccess, preview.Error?.Description);
                return preview.Value;
            }
            var raw = await Preview(MaterialKind.Raw);
            Assert.Equal(2, raw.Count); // Deduplicates repeated rows.
            Assert.Equal((100_000_000m, 1_000_000_000m, 10_000_000_000m), (raw[0].MinimumStockLevel, raw[0].ReOrderLevel, raw[0].MaximumStockLevel));
            Assert.Equal((100_000m, 1_000_000m, 10_000_000m), (raw[1].MinimumStockLevel, raw[1].ReOrderLevel, raw[1].MaximumStockLevel));
            Assert.True((await repository.PreviewMaterialLinks(migration.Id, MaterialKind.Package, user.Id)).IsFailure);
            Assert.True((await repository.CreateMaterialLinks(migration.Id, MaterialKind.Package,
                new() { Materials = [new() { MaterialId = pieces.Id }] }, user.Id)).IsFailure);
            var wrongKind = await repository.CreateMaterialLinks(migration.Id, MaterialKind.Raw, new() { Materials = [new() { MaterialId = pieces.Id }] }, user.Id);
            Assert.True(wrongKind.IsFailure);
            Assert.Empty(await db.MaterialDepartments.IgnoreQueryFilters().ToListAsync());
            Grant(PermissionKeys.CanLinkPackagingMaterials);
            await db.SaveChangesAsync();
            var packing = await Preview(MaterialKind.Package);
            Assert.Equal(piece.Id, Assert.Single(packing).UoMId);
            Assert.Equal((100m, 1000m, 10000m), (packing[0].MinimumStockLevel, packing[0].ReOrderLevel, packing[0].MaximumStockLevel));

            // A changed or missing unit cannot be linked using a stale preview.
            ml.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            Assert.NotNull((await Preview(MaterialKind.Raw)).Single(m => m.MaterialId == liquid.Id).Issue);
            var stale = await repository.CreateMaterialLinks(migration.Id, MaterialKind.Raw,
                new() { Materials = [raw.Single(m => m.MaterialId == liquid.Id)] }, user.Id);
            Assert.True(stale.IsFailure);
            Assert.Empty(await db.MaterialDepartments.IgnoreQueryFilters().ToListAsync());
            ml.DeletedAt = null;
            migration.Lines.First(l => l.MaterialCode == mass.Code).UnitSymbol = "ml";
            await db.SaveChangesAsync();
            Assert.NotNull((await Preview(MaterialKind.Raw)).Single(m => m.MaterialId == mass.Id).Issue);
            migration.Lines.First(l => l.MaterialCode == mass.Code).UnitSymbol = "mg";
            migration.Lines.Single(l => l.MaterialCode == liquid.Code).Excluded = true;
            await db.SaveChangesAsync();
            Assert.DoesNotContain(await Preview(MaterialKind.Raw), m => m.MaterialId == liquid.Id);
            migration.Lines.Single(l => l.MaterialCode == liquid.Code).Excluded = false;
            await db.SaveChangesAsync();

            // A link created after preview must keep its unit, density and levels.
            var existing = new MaterialDepartment { MaterialId = mass.Id, DepartmentId = department.Id, UoMId = piece.Id, MinimumStockLevel = 7, ReOrderLevel = 8, MaximumStockLevel = 9, Density = 12 };
            db.MaterialDepartments.Add(existing);
            await db.SaveChangesAsync();
            var result = await repository.CreateMaterialLinks(migration.Id, MaterialKind.Raw, new() { Materials = raw }, user.Id);
            Assert.True(result.IsSuccess, result.Error?.Description);
            Assert.Equal((1, 1), (result.Value.Created, result.Value.AlreadyLinked));
            var createdLink = await db.MaterialDepartments.IgnoreQueryFilters().SingleAsync(m => m.MaterialId == liquid.Id);
            Assert.Equal(user.Id, createdLink.CreatedById);
            Assert.Equal((piece.Id, 7m, 8m, 9m, 12m), (existing.UoMId, existing.MinimumStockLevel, existing.ReOrderLevel, existing.MaximumStockLevel, existing.Density));
            Assert.All(migration.Lines, line => Assert.DoesNotContain("RM2 is not linked", line.Blockers ?? ""));
            // Two confirmed previews submitted concurrently create exactly one link.
            await using var first = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options, actor);
            await using var second = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options, actor);
            var concurrent = await Task.WhenAll(
                new StockMigrationRepository(first).CreateMaterialLinks(migration.Id, MaterialKind.Package, new() { Materials = packing }, user.Id),
                new StockMigrationRepository(second).CreateMaterialLinks(migration.Id, MaterialKind.Package, new() { Materials = packing }, user.Id)).WaitAsync(TimeSpan.FromSeconds(30));
            Assert.All(concurrent, r => Assert.True(r.IsSuccess, r.Error?.Description));
            Assert.Equal(1, concurrent.Sum(r => r.Value.Created));
            Assert.Equal(1, concurrent.Sum(r => r.Value.AlreadyLinked));
            Assert.Single(await db.MaterialDepartments.IgnoreQueryFilters().Where(m => m.MaterialId == pieces.Id).ToListAsync());
            Assert.Empty(await Preview(MaterialKind.Package));
            Assert.Empty(await db.MaterialBatches.IgnoreQueryFilters().ToListAsync());
            Assert.Empty(await db.StockMigrationWriteOffs.ToListAsync());
            Assert.Equal(StockMigrationStatus.Draft, migration.Status);

            // Live permission revocation, missing active department and non-drafts are refused.
            var correction = await db.RoleClaims.SingleAsync(c => c.ClaimValue == PermissionKeys.CanCorrectOpeningBalances);
            db.RoleClaims.Remove(correction);
            await db.SaveChangesAsync();
            Assert.True((await repository.CreateMaterialLinks(migration.Id, MaterialKind.Package, new() { Materials = packing }, user.Id)).IsFailure);
            Grant(PermissionKeys.CanCorrectOpeningBalances);
            user.DepartmentId = null;
            await db.SaveChangesAsync();
            Assert.True((await repository.PreviewMaterialLinks(migration.Id, MaterialKind.Raw, user.Id)).IsFailure);
            var otherDepartment = new Department { Name = "Other department" };
            db.Departments.Add(otherDepartment);
            user.DepartmentId = otherDepartment.Id;
            await db.SaveChangesAsync();
            Assert.True((await repository.CreateMaterialLinks(migration.Id, MaterialKind.Package, new() { Materials = packing }, user.Id)).IsFailure);
            user.DepartmentId = department.Id;
            migration.Status = StockMigrationStatus.Approved;
            await db.SaveChangesAsync();
            Assert.True((await repository.CreateMaterialLinks(migration.Id, MaterialKind.Package, new() { Materials = packing }, user.Id)).IsFailure);
            migration.Status = StockMigrationStatus.Discarded;
            await db.SaveChangesAsync();
            Assert.True((await repository.PreviewMaterialLinks(migration.Id, MaterialKind.Raw, user.Id)).IsFailure);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
