using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>Glycerine counted in ml, density 1.261 g/mL, now counted in mg.</summary>
public sealed class MaterialCountingUnitChangeTests
{
    private const decimal Density = 1.261m;

    [Theory]
    [InlineData(nameof(MaterialCountingUnitController.PreviewRaw), PermissionKeys.CanChangeRawMaterialCountingUnit)]
    [InlineData(nameof(MaterialCountingUnitController.ChangeRaw), PermissionKeys.CanChangeRawMaterialCountingUnit)]
    [InlineData(nameof(MaterialCountingUnitController.PreviewPackaging), PermissionKeys.CanChangePackagingMaterialCountingUnit)]
    [InlineData(nameof(MaterialCountingUnitController.ChangePackaging), PermissionKeys.CanChangePackagingMaterialCountingUnit)]
    public void Counting_unit_actions_require_one_registered_kind_specific_permission(string method, string key)
    {
        var action = typeof(MaterialCountingUnitController).GetMethod(method)!;
        Assert.Equal(key, Assert.Single(action.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Single(PermissionUtils.GeneratePermissions(), permission => permission.Key == key);
    }

    private sealed record Seeded(ScheduleStockFixture F, UnitOfMeasure Milligram, MaterialDepartment Link,
        MaterialBatchReservedQuantity Hold, RequisitionItem Item, DOMAIN.Entities.Products.Production.ProductionMaterialConfiguration Configuration);

    private static async Task<Seeded> Seed(ScheduleStockFixture f, decimal? density = Density)
    {
        await f.Seed();
        var db = f.Harness.Db;
        f.Lot.UoM.Symbol = "ml";
        f.Lot.Material.DensityGramsPerMilliliter = density;
        var milligram = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Milligram", Symbol = "mg" };
        db.UnitOfMeasures.Add(milligram);
        var link = await db.MaterialDepartments.SingleAsync();
        link.MinimumStockLevel = 1_200_000; link.ReOrderLevel = 1_300_000; link.MaximumStockLevel = 18_800_000;
        var hold = new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatchId = f.Lot.Id, WarehouseId = f.Storage.Id,
            ProductionScheduleProductId = f.Batch.Id, UoMId = f.Lot.UoMId, Quantity = 118_953.2117367168913560666138m, DensityGramsPerMilliliter = Density };
        db.MaterialBatchReservedQuantities.Add(hold);
        var configuration = f.Configuration();
        configuration.RequirementsJson = ProductionMaterialSources.Json(new[] { f.Requirement() with { Unit = "ml", Quantity = 118_953.2117367168913560666138m } });
        db.ProductionMaterialConfigurations.Add(configuration);
        var item = new RequisitionItem { Id = Guid.NewGuid(), MaterialId = f.Lot.MaterialId, UoMId = f.Lot.UoMId, Quantity = 75_000 };
        db.Requisitions.Add(new Requisition { Id = Guid.NewGuid(), Code = "RM/N/26/097", DepartmentId = f.Department.Id,
            RequestedById = f.Actor.Id, RequisitionType = RequisitionType.Stock, ProductionScheduleProductId = f.Batch.Id, Items = [item] });
        await db.SaveChangesAsync();
        return new Seeded(f, milligram, link, hold, item, configuration);
    }

    [Fact]
    public async Task Changing_ml_to_mg_converts_every_department_value_with_the_density_and_keeps_stock_physically_equal()
    {
        using var f = new ScheduleStockFixture();
        var s = await Seed(f);
        var db = f.Harness.Db;
        var repository = new MaterialRepository(db, f.Mapper);
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanChangeRawMaterialCountingUnit);

        var preview = (await repository.PreviewCountingUnitChange(f.Lot.MaterialId, MaterialKind.Raw, s.Milligram.Id, f.Actor.Id)).Value;
        Assert.Empty(preview.Blockers);
        Assert.True(preview.HasChanges);
        Assert.Equal(Density, preview.DensityGramsPerMilliliter);
        Assert.Equal([1_513_200_000m, 1_639_300_000m, 23_706_800_000m], preview.Levels.Select(level => level.After));
        // The department's stock: 1,000,000 ml before and 1,261,000,000 mg after, the same amount.
        Assert.Equal(1_000_000m, preview.Stock.Before); Assert.Equal(1_261_000_000m, preview.Stock.After);
        Assert.Equal(150_000_000m, decimal.Round(preview.Reserved.After, 6));
        Assert.Equal(150_000_000m, decimal.Round(Assert.Single(Assert.Single(preview.Batches).Values).After, 6));
        Assert.Equal(94_575_000m, Assert.Single(Assert.Single(preview.Requisitions).Values).After);

        // Nothing is written by a preview, and a stale preview is refused.
        Assert.Equal(f.Lot.UoMId, (await db.MaterialDepartments.AsNoTracking().SingleAsync()).UoMId);
        var stale = await repository.ChangeCountingUnit(f.Lot.MaterialId, MaterialKind.Raw,
            new ChangeMaterialCountingUnit { UnitId = s.Milligram.Id, PreviewHash = new string('0', 64), Reason = "Customer counts in mg" }, f.Actor.Id);
        Assert.Equal("CountingUnit.Stale", stale.Error.Code);

        var applied = await repository.ChangeCountingUnit(f.Lot.MaterialId, MaterialKind.Raw,
            new ChangeMaterialCountingUnit { UnitId = s.Milligram.Id, PreviewHash = preview.PreviewHash, Reason = "Customer counts in mg" }, f.Actor.Id);
        Assert.True(applied.IsSuccess, applied.IsFailure ? applied.Error.Description : null);
        Assert.True(applied.Value.Changed);

        var link = await db.MaterialDepartments.SingleAsync();
        Assert.Equal(s.Milligram.Id, link.UoMId);
        Assert.Equal(1_513_200_000m, link.MinimumStockLevel); Assert.Equal(23_706_800_000m, link.MaximumStockLevel);
        var line = Assert.Single(ProductionMaterialSources.Requirements(s.Configuration.RequirementsJson));
        Assert.Equal(s.Milligram.Id, line.UnitId); Assert.Equal("mg", line.Unit);
        Assert.Equal(150_000_000m, decimal.Round(line.Quantity, 6));
        Assert.Equal(1, s.Configuration.Revision);
        Assert.Equal(s.Milligram.Id, s.Item.UoMId); Assert.Equal(94_575_000m, s.Item.Quantity);
        // Physical rows keep their own unit and quantity.
        Assert.Equal(1_000_000, f.Shelf.Quantity); Assert.Equal(f.Lot.UoMId, s.Hold.UoMId);
        Assert.Equal(118_953.2117367168913560666138m, s.Hold.Quantity);

        var audit = Assert.Single(await db.MaterialCountingUnitChanges.ToListAsync());
        Assert.Equal(f.Lot.UoMId, audit.FromUnitId); Assert.Equal(s.Milligram.Id, audit.ToUnitId);
        Assert.Equal(Density, audit.DensityGramsPerMilliliter); Assert.Equal(f.Actor.Id, audit.ActorId);
        Assert.Contains("1513200000", audit.PreviewJson);
        audit.Reason = "edited";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.Entry(audit).State = EntityState.Unchanged;

        // Repeating the change has nothing left to convert.
        var again = (await repository.PreviewCountingUnitChange(f.Lot.MaterialId, MaterialKind.Raw, s.Milligram.Id, f.Actor.Id)).Value;
        Assert.False(again.HasChanges);
        Assert.Equal(1_261_000_000m, again.Stock.Before);
    }

    [Fact]
    public async Task A_kilogram_link_keeps_levels_in_mg_but_uses_kg_for_production_and_requisitions()
    {
        using var f = new ScheduleStockFixture();
        var s = await Seed(f);
        var db = f.Harness.Db;
        var kilogram = new UnitOfMeasure { Symbol = "kg", Name = "Kilogram" };
        db.UnitOfMeasures.Add(kilogram);
        await db.SaveChangesAsync();
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanChangeRawMaterialCountingUnit);
        var repo = new MaterialRepository(db, f.Mapper);
        var preview = (await repo.PreviewCountingUnitChange(f.Lot.MaterialId, MaterialKind.Raw, kilogram.Id, f.Actor.Id)).Value;
        Assert.Empty(preview.Blockers);
        Assert.Equal("mg", preview.Levels[0].AfterUnit);
        Assert.Equal(1_513_200_000m, preview.Levels[0].After);
        Assert.Equal(1_261m, preview.Stock.After);
        Assert.Equal(150m, decimal.Round(Assert.Single(Assert.Single(preview.Batches).Values).After, 6));
        Assert.Equal(94.575m, Assert.Single(Assert.Single(preview.Requisitions).Values).After);
        var applied = await repo.ChangeCountingUnit(f.Lot.MaterialId, MaterialKind.Raw,
            new ChangeMaterialCountingUnit { UnitId = kilogram.Id, PreviewHash = preview.PreviewHash, Reason = "Count stock by weight" }, f.Actor.Id);
        Assert.True(applied.IsSuccess);
        Assert.Equal(1_513_200_000m, s.Link.MinimumStockLevel);
    }

    [Fact]
    public async Task Selecting_the_current_unit_repairs_stale_rows_and_records_the_density()
    {
        using var f = new ScheduleStockFixture();
        var s = await Seed(f);
        var db = f.Harness.Db;
        s.Link.UoMId = s.Milligram.Id;
        await db.SaveChangesAsync();
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanChangeRawMaterialCountingUnit);
        var repo = new MaterialRepository(db, f.Mapper);
        var preview = (await repo.PreviewCountingUnitChange(f.Lot.MaterialId, MaterialKind.Raw, s.Milligram.Id, f.Actor.Id)).Value;
        Assert.True(preview.HasChanges);
        Assert.Empty(preview.Blockers);
        Assert.Equal(preview.Stock.Before, preview.Stock.After);
        Assert.Equal(Density, preview.DensityGramsPerMilliliter);
        var applied = await repo.ChangeCountingUnit(f.Lot.MaterialId, MaterialKind.Raw,
            new ChangeMaterialCountingUnit { UnitId = s.Milligram.Id, PreviewHash = preview.PreviewHash, Reason = "Normalize earlier quantities" }, f.Actor.Id);
        Assert.True(applied.IsSuccess);
        Assert.Equal(1_200_000m, s.Link.MinimumStockLevel);
        Assert.Equal(94_575_000m, s.Item.Quantity);
        Assert.Equal(Density, (await db.MaterialCountingUnitChanges.SingleAsync()).DensityGramsPerMilliliter);
    }

    [Fact]
    public async Task Packaging_conversion_updates_unitless_definitions_and_an_extra_request()
    {
        using var f = new ScheduleStockFixture();
        var s = await Seed(f);
        var db = f.Harness.Db;
        f.Lot.Material.Kind = MaterialKind.Package;
        s.Configuration.Kind = MaterialKind.Package;
        var package = new ProductPackage { Product = f.Batch.Product, Material = f.Lot.Material, BaseQuantity = 2,
            PrescribedQuantity = 200, Loose = 1 };
        var extra = new ProductionExtraPacking { ProductionScheduleProductId = f.Batch.Id, MaterialId = f.Lot.MaterialId,
            UoMId = f.Lot.UoMId, Quantity = 30 };
        db.ProductPackages.Add(package);
        db.ProductionExtraPackings.Add(extra);
        await db.SaveChangesAsync();
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanChangePackagingMaterialCountingUnit);
        var repo = new MaterialRepository(db, f.Mapper);
        var preview = (await repo.PreviewCountingUnitChange(f.Lot.MaterialId, MaterialKind.Package, s.Milligram.Id, f.Actor.Id)).Value;
        Assert.Empty(preview.Blockers);
        Assert.Equal([2_522m, 252_200m, 1_261m], Assert.Single(preview.Packaging).Values.Select(value => value.After));
        Assert.Equal(37_830m, Assert.Single(Assert.Single(preview.ExtraRequests).Values).After);
        var result = await repo.ChangeCountingUnit(f.Lot.MaterialId, MaterialKind.Package,
            new ChangeMaterialCountingUnit { UnitId = s.Milligram.Id, PreviewHash = preview.PreviewHash, Reason = "Weigh material at issue" }, f.Actor.Id);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        Assert.Equal(252_200m, package.PrescribedQuantity);
        Assert.Equal(37_830m, extra.Quantity);
        Assert.Equal(s.Milligram.Id, extra.UoMId);
    }

    [Fact]
    public async Task Mass_volume_change_without_a_verified_density_is_refused()
    {
        using var f = new ScheduleStockFixture();
        var s = await Seed(f, density: null);
        var repository = new MaterialRepository(f.Harness.Db, f.Mapper);
        await SharedDensityTransferTests.Grant(f.Harness.Db, f.Actor.Id, PermissionKeys.CanChangeRawMaterialCountingUnit);
        var preview = (await repository.PreviewCountingUnitChange(f.Lot.MaterialId, MaterialKind.Raw, s.Milligram.Id, f.Actor.Id)).Value;
        Assert.Contains(preview.Blockers, blocker => blocker.Contains("verified density"));
        var refused = await repository.ChangeCountingUnit(f.Lot.MaterialId, MaterialKind.Raw,
            new ChangeMaterialCountingUnit { UnitId = s.Milligram.Id, PreviewHash = preview.PreviewHash, Reason = "Customer counts in mg" }, f.Actor.Id);
        Assert.Equal("CountingUnit.Blocked", refused.Error.Code);
        Assert.Equal(f.Lot.UoMId, (await f.Harness.Db.MaterialDepartments.SingleAsync()).UoMId);
        Assert.Empty(f.Harness.Db.MaterialCountingUnitChanges);
    }

    [Fact]
    public async Task Change_requires_the_key_for_the_stored_material_kind()
    {
        using var f = new ScheduleStockFixture();
        var s = await Seed(f);
        var repository = new MaterialRepository(f.Harness.Db, f.Mapper);
        var none = await repository.PreviewCountingUnitChange(f.Lot.MaterialId, MaterialKind.Raw, s.Milligram.Id, f.Actor.Id);
        Assert.Equal("CountingUnit.Permission", none.Error.Code);
        await SharedDensityTransferTests.Grant(f.Harness.Db, f.Actor.Id, PermissionKeys.CanChangePackagingMaterialCountingUnit);
        Assert.Equal("CountingUnit.Permission", (await repository.PreviewCountingUnitChange(f.Lot.MaterialId, MaterialKind.Raw, s.Milligram.Id, f.Actor.Id)).Error.Code);
        Assert.Equal("CountingUnit.Kind", (await repository.PreviewCountingUnitChange(f.Lot.MaterialId, MaterialKind.Package, s.Milligram.Id, f.Actor.Id)).Error.Code);
    }

    [Fact]
    public async Task Stock_levels_save_no_longer_changes_the_unit_of_an_existing_link()
    {
        using var f = new ScheduleStockFixture();
        var s = await Seed(f);
        var repository = new MaterialRepository(f.Harness.Db, f.Mapper);
        await SharedDensityTransferTests.Grant(f.Harness.Db, f.Actor.Id, PermissionKeys.CanLinkRawMaterials);
        var result = await repository.CreateMaterialDepartment([new CreateMaterialDepartment { MaterialId = f.Lot.MaterialId, UoMId = s.Milligram.Id,
            MinimumStockLevel = 1, ReOrderLevel = 2, MaximumStockLevel = 3 }], f.Actor.Id);
        Assert.Equal("MaterialDepartment.CountingUnit", result.Error.Code);
        Assert.Equal(1_200_000m, (await f.Harness.Db.MaterialDepartments.SingleAsync()).MinimumStockLevel);
        var levels = await repository.CreateMaterialDepartment([new CreateMaterialDepartment { MaterialId = f.Lot.MaterialId, UoMId = f.Lot.UoMId,
            MinimumStockLevel = 1, ReOrderLevel = 2, MaximumStockLevel = 3 }], f.Actor.Id);
        Assert.True(levels.IsSuccess);
    }

    [Theory]
    [InlineData("mg", null, 150_000_000)]
    [InlineData("mg", 1.261, 150_000_000)]
    [InlineData("ml", 1.261, 118_953.2117367168913560666138)]
    public void A_density_converted_bom_line_reads_directly_for_a_mass_unit_and_through_density_for_a_volume_unit(string unit, double? density, double expected)
    {
        var link = new MaterialDepartment { UoM = new UnitOfMeasure { Symbol = unit },
            Material = new Material { Name = "Glycerine", DensityGramsPerMilliliter = (decimal?)density } };
        var converted = MaterialDensity.ConvertBomMass(150_000_000m, "mg", link);
        Assert.NotNull(converted);
        Assert.Equal((decimal)expected, decimal.Round(converted.Value, 6), 4);
    }

    [Fact]
    public void A_volume_unit_without_density_still_cannot_take_a_mass_bom_line()
    {
        var link = new MaterialDepartment { UoM = new UnitOfMeasure { Symbol = "ml" }, Material = new Material { Name = "Glycerine" } };
        Assert.Null(MaterialDensity.ConvertBomMass(150_000_000m, "mg", link));
    }
}
