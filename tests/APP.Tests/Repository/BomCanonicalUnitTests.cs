using APP.Repository;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.Materials;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public partial class BomMaterialUnitTests
{
    [Fact]
    public async Task Canonical_BOM_amounts_are_converted_back_to_the_department_stock_unit_for_production()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var db = fixture.Harness.Db;
        var liter = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Liter", Symbol = "L" };
        var ml = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Milliliter", Symbol = "ml" };
        db.UnitOfMeasures.AddRange(liter, ml);
        var link = await db.MaterialDepartments.SingleAsync();
        link.UoMId = liter.Id;
        var definition = await db.BillOfMaterialItems.SingleAsync();
        definition.BaseUoMId = ml.Id;
        definition.BaseQuantity = 1.25m;
        definition.PrescribedQuantity = 75000m;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var source = await ProductionMaterialSources.Load(db, fixture.Batch.Id, MaterialKind.Raw);

        Assert.True(source.IsSuccess, source.Error.Description);
        Assert.Empty(source.Value.Blockers);
        var requirement = Assert.Single(source.Value.Requirements);
        Assert.Equal(liter.Id, requirement.UnitId);
        Assert.Equal(75m, requirement.Quantity);
        Assert.Equal(ml.Id, requirement.BaseUnitId);
        Assert.Equal(1.25m, requirement.BaseQuantity);
        Assert.Equal(75000m, requirement.PrescribedQuantity);
    }

    [Theory]
    [InlineData("mg", "kg", "mg", 2500000)]
    [InlineData("g", "kg", "mg", 2500000)]
    [InlineData("kg", "g", "mg", 2500)]
    [InlineData("ml", "L", "ml", 2500)]
    [InlineData("L", "ml", "ml", 2.5)]
    public async Task Measured_materials_save_in_the_smallest_unit_of_their_own_linkage_family(string stock, string entered, string smallest, double expected)
    {
        await using var db = Context();
        var (product, material, _, _) = await Seed(db);
        foreach (var symbol in new[] { stock, entered, smallest }.Distinct(StringComparer.OrdinalIgnoreCase))
            if (!await db.UnitOfMeasures.AnyAsync(unit => unit.Symbol == symbol)) db.UnitOfMeasures.Add(new() { Name = symbol, Symbol = symbol });
        await db.SaveChangesAsync();
        var units = await db.UnitOfMeasures.ToListAsync();
        (await db.MaterialDepartments.SingleAsync()).UoMId = units.Single(unit => unit.Symbol == stock).Id;
        await db.SaveChangesAsync();
        var request = new CreateBillOfMaterialRequest { ProductId = product.Id, Items = [new() { MaterialId = material.Id,
            BaseUoMId = units.Single(unit => unit.Symbol == entered).Id, BaseQuantity = 2.5m, PrescribedQuantity = 5m }] };

        var result = await new BoMRepository(db, Mapper(db)).CreateBillOfMaterial(request, ActorId);

        Assert.True(result.IsSuccess, result.Error.Description);
        var saved = await db.BillOfMaterialItems.SingleAsync();
        Assert.Equal(units.Single(unit => unit.Symbol == smallest).Id, saved.BaseUoMId);
        Assert.Equal((decimal)expected, saved.BaseQuantity);
        Assert.Equal((decimal)expected * 2, saved.PrescribedQuantity);
    }

    [Fact]
    public async Task A_main_and_substitute_use_independent_mass_and_volume_units()
    {
        await using var db = Context();
        var (product, main, _, mg) = await Seed(db);
        var gram = new UnitOfMeasure { Name = "Gram", Symbol = "g" };
        var liter = new UnitOfMeasure { Name = "Liter", Symbol = "L" };
        var ml = new UnitOfMeasure { Name = "Milliliter", Symbol = "ml" };
        var sub = new Material { Name = "Liquid alternate", Code = "SUB-VOL" };
        db.UnitOfMeasures.AddRange(gram, liter, ml);
        (await db.MaterialDepartments.SingleAsync()).UoM = gram;
        db.MaterialDepartments.Add(new() { Material = sub, DepartmentId = DepartmentId, UoM = liter });
        await db.SaveChangesAsync();

        var result = await new BoMRepository(db, Mapper(db)).CreateBillOfMaterial(new() { ProductId = product.Id,
            Items = [new() { MaterialId = main.Id, BaseUoMId = gram.Id, BaseQuantity = 1.25m, PrescribedQuantity = 2.5m,
                Substitutes = [new() { MaterialId = sub.Id, BaseUoMId = liter.Id, BaseQuantity = 0.0123456789m, PrescribedQuantity = 0.0246913578m }] }] }, ActorId);

        Assert.True(result.IsSuccess, result.Error.Description);
        var saved = await db.BillOfMaterialItems.SingleAsync();
        var alternate = await db.BillOfMaterialItemSubstitutes.SingleAsync();
        Assert.Equal((mg.Id, 1250m, 2500m), (saved.BaseUoMId, saved.BaseQuantity, saved.PrescribedQuantity));
        Assert.Equal(((Guid?)ml.Id, (decimal?)12.3456789m, (decimal?)24.6913578m), (alternate.BaseUoMId, alternate.BaseQuantity, alternate.PrescribedQuantity));
        Assert.Equal(liter.Id, (await db.MaterialDepartments.SingleAsync(link => link.MaterialId == sub.Id)).UoMId);
    }

    [Theory]
    [InlineData(ProductContext.Production)]
    [InlineData(ProductContext.Qa)]
    public async Task Saving_reloads_the_exact_context_grants_before_writing(ProductContext productContext)
    {
        await using var db = Context();
        var (product, material, piece, _) = await Seed(db);
        var repository = new BoMRepository(db, Mapper(db));
        CreateBillOfMaterialRequest Request() => new() { ProductId = product.Id, Items = [new() {
            MaterialId = material.Id, BaseUoMId = piece.Id, BaseQuantity = 2, PrescribedQuantity = 10 }] };
        if (productContext == ProductContext.Production) db.RoleClaims.RemoveRange(await db.RoleClaims.ToListAsync());
        await db.SaveChangesAsync();
        var denied = await repository.CreateBillOfMaterial(Request(), ActorId, productContext);
        Assert.Equal("Bom.Permission", denied.Error.Code);
        Assert.Empty(await db.BillOfMaterials.ToListAsync());
        var role = await db.Roles.SingleAsync();
        var key = productContext == ProductContext.Qa ? PermissionKeys.CanAddQaProductBom : PermissionKeys.CanAddProductionProductBom;
        db.RoleClaims.Add(new() { Id = 500, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key });
        db.PermissionTypes.Add(new() { Id = Guid.NewGuid(), RoleClaimId = 500, Key = key, Type = "Access" });
        await db.SaveChangesAsync();
        Assert.True((await repository.CreateBillOfMaterial(Request(), ActorId, productContext)).IsSuccess);
    }

    [Fact]
    public async Task A_technology_transfer_keeps_its_own_permission_and_must_target_its_stored_product()
    {
        await using var db = Context();
        var (product, material, piece, _) = await Seed(db);
        var transfer = new DOMAIN.Entities.RndTechnologyTransfers.RndTechnologyTransfer { Id = Guid.NewGuid(),
            RndProject = new DOMAIN.Entities.RndProjects.RndProject { ProductId = product.Id },
            Status = DOMAIN.Entities.RndTechnologyTransfers.RndTechnologyTransferStatus.ExecutionInProgress };
        db.RndTechnologyTransfers.Add(transfer);
        await db.SaveChangesAsync();
        var repository = new BoMRepository(db, Mapper(db));
        CreateBillOfMaterialRequest Request() => new() { ProductId = product.Id, Items = [new() {
            MaterialId = material.Id, BaseUoMId = piece.Id, BaseQuantity = 2, PrescribedQuantity = 10 }] };
        Assert.Equal("Bom.Permission", (await repository.CreateBillOfMaterial(Request(), ActorId, technologyTransferId: transfer.Id)).Error.Code);
        db.RoleClaims.RemoveRange(await db.RoleClaims.ToListAsync());
        var role = await db.Roles.SingleAsync();
        db.RoleClaims.Add(new() { Id = 510, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanPromoteRndTechnologyTransfer });
        db.PermissionTypes.Add(new() { Id = Guid.NewGuid(), RoleClaimId = 510, Key = PermissionKeys.CanPromoteRndTechnologyTransfer, Type = "Access" });
        await db.SaveChangesAsync();
        Assert.Equal("Bom.Permission", (await repository.CreateBillOfMaterial(Request(), ActorId, technologyTransferId: Guid.NewGuid())).Error.Code);
        Assert.True((await repository.CreateBillOfMaterial(Request(), ActorId, technologyTransferId: transfer.Id)).IsSuccess);
    }
}
