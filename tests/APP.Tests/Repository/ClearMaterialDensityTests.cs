using System.Reflection;
using System.Text.Json;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public sealed class ClearMaterialDensityTests
{
    [Theory]
    [InlineData(nameof(MaterialDensityController.ClearRaw), PermissionKeys.CanEditRawMaterialDensity)]
    [InlineData(nameof(MaterialDensityController.ClearPackaging), PermissionKeys.CanEditPackagingMaterialDensity)]
    public void Clear_density_actions_require_one_registered_kind_specific_permission(string method, string key)
    {
        var action = typeof(MaterialDensityController).GetMethod(method)!;
        Assert.Equal(key, Assert.Single(action.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Single(PermissionUtils.GeneratePermissions(), permission => permission.Key == key);
    }

    [Theory]
    [InlineData(MaterialKind.Raw)]
    [InlineData(MaterialKind.Package)]
    public async Task Clearing_requires_the_stored_kind_permission_and_preserves_stock_and_adopted_density(MaterialKind kind)
    {
        using var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.Material.Kind = kind; f.Lot.Material.DensityGramsPerMilliliter = 0.8m;
        var unit = new DOMAIN.Entities.Base.UnitOfMeasure { Id = Guid.NewGuid(), Name = "Density", Symbol = "g/mL" };
        db.UnitOfMeasures.Add(unit);
        var existing = await db.MaterialDepartments.SingleAsync();
        existing.Density = 0.8m; existing.DensityUoM = unit;
        var archived = new MaterialDepartment { Id = Guid.NewGuid(), MaterialId = f.Lot.MaterialId, DepartmentId = f.OtherDepartment.Id,
            Density = 1.2m, DensityUoM = unit, UoMId = f.Lot.UoMId, DeletedAt = DateTime.UtcNow };
        db.MaterialDepartments.Add(archived);
        var reservation = new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatchId = f.Lot.Id,
            ProductionScheduleProductId = f.Batch.Id, UoMId = f.Lot.UoMId, Quantity = 100, DensityGramsPerMilliliter = 0.8m };
        db.MaterialBatchReservedQuantities.Add(reservation);
        await db.SaveChangesAsync();
        var repository = new MaterialRepository(db, f.Mapper);
        var wrongKind = kind == MaterialKind.Raw ? MaterialKind.Package : MaterialKind.Raw;
        await SharedDensityTransferTests.Grant(db, f.Actor.Id,
            wrongKind == MaterialKind.Raw ? PermissionKeys.CanEditRawMaterialDensity : PermissionKeys.CanEditPackagingMaterialDensity);
        var denied = await repository.ClearDensity(f.Lot.MaterialId, kind, f.Actor.Id);
        Assert.Equal("MaterialDensity.Permission", denied.Error.Code);
        Assert.Equal(0.8m, f.Lot.Material.DensityGramsPerMilliliter); Assert.Empty(db.MaterialDensityMeasurements);
        await SharedDensityTransferTests.Grant(db, f.Actor.Id,
            kind == MaterialKind.Raw ? PermissionKeys.CanEditRawMaterialDensity : PermissionKeys.CanEditPackagingMaterialDensity);
        var wrongRoute = await repository.ClearDensity(f.Lot.MaterialId, wrongKind, f.Actor.Id);
        Assert.Equal("MaterialDensity.Kind", wrongRoute.Error.Code);
        var result = await repository.ClearDensity(f.Lot.MaterialId, kind, f.Actor.Id);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Null(f.Lot.Material.DensityGramsPerMilliliter);
        var links = await db.MaterialDepartments.IgnoreQueryFilters().ToListAsync();
        Assert.All(links, link => { Assert.Equal(0, link.Density); Assert.Null(link.DensityUoMId); Assert.Null(MaterialDensity.Verified(link)); });
        Assert.Equal(1_000_000, f.Lot.TotalQuantity); Assert.Equal(1_000_000, f.Shelf.Quantity);
        Assert.Equal(100, reservation.Quantity); Assert.Equal(0.8m, reservation.DensityGramsPerMilliliter);
        var audit = Assert.Single(await db.MaterialDensityMeasurements.ToListAsync());
        Assert.Equal(0.8m, audit.PreviousDensity); Assert.Null(audit.Density); Assert.Equal(f.Actor.Id, audit.ActorId);
        using var evidence = JsonDocument.Parse(audit.PreviousDepartmentDensitiesJson);
        Assert.Equal(2, evidence.RootElement.GetArrayLength());
        Assert.True((await repository.ClearDensity(f.Lot.MaterialId, kind, f.Actor.Id)).IsSuccess);
        Assert.Single(await db.MaterialDensityMeasurements.ToListAsync()); // An identical retry does not invent another change.
        audit.Density = 2;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
    [RepairPostgresFact]
    public async Task Migration_allows_null_clear_evidence_and_keeps_database_history_immutable()
    {
        await using var f = new IssuedMaterialRepairFixture();
        await f.Seed(DOMAIN.Entities.Products.Production.IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = f.Open();
        var link = await db.MaterialDepartments.Include(item => item.Material).FirstAsync();
        var unit = new DOMAIN.Entities.Base.UnitOfMeasure { Id = Guid.NewGuid(), Name = "Density", Symbol = "g/mL" };
        db.UnitOfMeasures.Add(unit); link.Density = 0.8m; link.DensityUoM = unit; link.Material.DensityGramsPerMilliliter = 0.8m;
        db.MaterialDensityMeasurements.Add(new() { Id = Guid.NewGuid(), MaterialId = link.MaterialId,
            ActorId = f.Actor.UserId!.Value, RecordedAt = DateTime.UtcNow, Density = 0.8m });
        await db.SaveChangesAsync();
        await SharedDensityTransferTests.Grant(db, f.Actor.UserId.Value, PermissionKeys.CanEditRawMaterialDensity);
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE "MaterialDensityMeasurements" ALTER COLUMN "Density" SET NOT NULL;
            ALTER TABLE "MaterialDensityMeasurements" DROP COLUMN "PreviousDepartmentDensitiesJson";
            """);
        var generator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator>(db);
        var oldMigration = new INFRASTRUCTURE.Migrations.AddSharedMaterialDensity();
        foreach (var command in generator.Generate(oldMigration.UpOperations, db.Model)
            .Where(command => command.CommandText.Contains("CREATE TRIGGER material_density_immutable")))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        var migration = new INFRASTRUCTURE.Migrations.AllowClearingMaterialDensity();
        foreach (var command in generator.Generate(migration.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        var result = await new MaterialRepository(db, null!).ClearDensity(link.MaterialId, link.Material.Kind, f.Actor.UserId.Value);
        Assert.True(result.IsSuccess, result.Error.Description);
        var history = await db.MaterialDensityMeasurements.OrderBy(row => row.RecordedAt).ToListAsync();
        Assert.Equal(2, history.Count); Assert.Equal(0.8m, history[0].Density); Assert.Null(history[1].Density);
        Assert.NotNull(history[1].PreviousDepartmentDensitiesJson);
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync("UPDATE \"MaterialDensityMeasurements\" SET \"Density\" = 2"));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM \"MaterialDensityMeasurements\""));
        var rollback = Assert.Single(generator.Generate(migration.DownOperations, db.Model)
            .Where(command => command.CommandText.Contains("Cannot roll back")));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync(rollback.CommandText));
        Assert.Equal(2, await db.MaterialDensityMeasurements.CountAsync());
    }

}
