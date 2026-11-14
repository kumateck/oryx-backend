using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Requisitions.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public class BomMaterialUnitTests
{
    private static readonly Guid DepartmentId = Guid.NewGuid();
    [Fact]
    public async Task Saving_a_weight_unit_for_a_piece_link_is_refused_before_the_existing_bom_is_deleted()
    {
        await using var context = Context();
        var (product, material, piece, mg) = await Seed(context);
        var old = new BillOfMaterial { ProductId = product.Id, Items = [new() { MaterialId = material.Id, BaseUoMId = mg.Id, BaseQuantity = 500000 }] };
        context.BillOfMaterials.Add(old);
        context.ProductBillOfMaterials.Add(new() { ProductId = product.Id, BillOfMaterialId = old.Id, IsActive = true });
        await context.SaveChangesAsync();

        var result = await new BoMRepository(context, Mapper(context)).CreateBillOfMaterial(new()
        { ProductId = product.Id, Items = [new() { MaterialId = material.Id, BaseUoMId = mg.Id, BaseQuantity = 500000 }] }, Guid.NewGuid());

        Assert.Equal("Bom.MaterialUnit", result.Error.Code);
        Assert.Equal(old.Id, (await context.BillOfMaterials.SingleAsync()).Id);
        Assert.Equal(500000, (await context.BillOfMaterialItems.SingleAsync()).BaseQuantity);
        Assert.Equal(piece.Id, (await context.MaterialDepartments.SingleAsync()).UoMId);
    }

    [Fact]
    public async Task A_raw_material_can_be_saved_in_its_linked_piece_unit_without_quantity_conversion()
    {
        await using var context = Context();
        var (product, material, piece, _) = await Seed(context);
        var result = await new BoMRepository(context, Mapper(context)).CreateBillOfMaterial(new()
        { ProductId = product.Id, Items = [new() { MaterialId = material.Id, BaseUoMId = piece.Id, BaseQuantity = 500000, PrescribedQuantity = 1000000 }] }, Guid.NewGuid());
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        var saved = await context.BillOfMaterialItems.SingleAsync();
        Assert.Equal(piece.Id, saved.BaseUoMId);
        Assert.Equal(500000, saved.BaseQuantity);
        Assert.Equal(1000000, saved.PrescribedQuantity);
    }

    [Fact]
    public async Task Density_conversion_saves_mg_source_for_a_liquid_stock_link()
    {
        await using var context = Context();
        var (product, material, _, mg) = await Seed(context);
        var ml = new UnitOfMeasure { Name = "Milliliter", Symbol = "ml", Category = UnitOfMeasureCategory.Volume };
        var densityUnit = new UnitOfMeasure { Name = "Grams per milliliter", Symbol = "g/mL" };
        context.UnitOfMeasures.AddRange(ml, densityUnit);
        var link = await context.MaterialDepartments.SingleAsync(item => item.MaterialId == material.Id);
        link.UoM = ml;
        link.UoMId = ml.Id;
        link.Density = 1.45m;
        link.DensityUoM = densityUnit;
        await context.SaveChangesAsync();

        var result = await new BoMRepository(context, Mapper(context)).CreateBillOfMaterial(new()
        { ProductId = product.Id, Items = [new() { MaterialId = material.Id, BaseQuantity = 1_000_000,
            PrescribedQuantity = 1_000_000, UseDensityConversion = true }] }, Guid.NewGuid());

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        var saved = await context.BillOfMaterialItems.SingleAsync();
        Assert.Equal(mg.Id, saved.BaseUoMId);
        Assert.True(saved.UseDensityConversion);
    }

    [Theory]
    [InlineData(0, "g/mL", false)]
    [InlineData(1, null, false)]
    [InlineData(1, "kg/m³", false)]
    [InlineData(1, "g/mL", true)]
    public async Task Density_conversion_refuses_unverified_density(int density, string? symbol, bool deleted)
    {
        await using var context = Context();
        var (product, material, _, _) = await Seed(context);
        var ml = new UnitOfMeasure { Name = "Milliliter", Symbol = "ml", Category = UnitOfMeasureCategory.Volume };
        context.UnitOfMeasures.Add(ml);
        var link = await context.MaterialDepartments.SingleAsync(item => item.MaterialId == material.Id);
        link.UoM = ml;
        link.UoMId = ml.Id;
        link.Density = density;
        if (symbol is not null)
            link.DensityUoM = new UnitOfMeasure { Name = symbol, Symbol = symbol, DeletedAt = deleted ? DateTime.UtcNow : null };
        await context.SaveChangesAsync();

        var result = await new BoMRepository(context, Mapper(context)).CreateBillOfMaterial(new()
        { ProductId = product.Id, Items = [new() { MaterialId = material.Id, BaseQuantity = 1,
            PrescribedQuantity = 1, UseDensityConversion = true }] }, Guid.NewGuid());

        Assert.Equal("Bom.Density", result.Error.Code);
        Assert.Empty(await context.BillOfMaterials.ToListAsync());
    }

    [Fact]
    public async Task Density_conversion_requires_every_substitute_to_have_density_and_a_liquid_stock_unit()
    {
        await using var context = Context();
        var (product, material, _, _) = await Seed(context);
        var ml = new UnitOfMeasure { Name = "Milliliter", Symbol = "ml", Category = UnitOfMeasureCategory.Volume };
        var substitute = new Material { Name = "Substitute", Code = "RMS002", Kind = MaterialKind.Raw };
        var densityUnit = new UnitOfMeasure { Name = "Grams per milliliter", Symbol = "g/mL" };
        context.UnitOfMeasures.AddRange(ml, densityUnit);
        context.MaterialDepartments.Add(new() { Material = substitute, DepartmentId = DepartmentId, UoM = ml, Density = 1.4m, DensityUoM = densityUnit });
        var primaryLink = await context.MaterialDepartments.SingleAsync(item => item.MaterialId == material.Id);
        primaryLink.UoM = ml;
        primaryLink.UoMId = ml.Id;
        primaryLink.Density = 1.45m;
        primaryLink.DensityUoM = densityUnit;
        await context.SaveChangesAsync();

        var result = await new BoMRepository(context, Mapper(context)).CreateBillOfMaterial(new()
        { ProductId = product.Id, Items = [new() { MaterialId = material.Id, BaseQuantity = 1,
            PrescribedQuantity = 1, UseDensityConversion = true, SubstituteMaterialIds = [substitute.Id] }] }, Guid.NewGuid());

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
    }

    [Fact]
    public async Task Density_conversion_refuses_a_substitute_without_a_valid_density()
    {
        await using var context = Context();
        var (product, material, _, _) = await Seed(context);
        var ml = new UnitOfMeasure { Name = "Milliliter", Symbol = "ml", Category = UnitOfMeasureCategory.Volume };
        var substitute = new Material { Name = "Substitute", Code = "RMS003", Kind = MaterialKind.Raw };
        var densityUnit = new UnitOfMeasure { Name = "Grams per milliliter", Symbol = "g/mL" };
        context.UnitOfMeasures.AddRange(ml, densityUnit);
        context.MaterialDepartments.Add(new() { Material = substitute, DepartmentId = DepartmentId, UoM = ml, Density = 0 });
        var primaryLink = await context.MaterialDepartments.SingleAsync(item => item.MaterialId == material.Id);
        primaryLink.UoM = ml;
        primaryLink.UoMId = ml.Id;
        primaryLink.Density = 1.45m;
        primaryLink.DensityUoM = densityUnit;
        await context.SaveChangesAsync();

        var result = await new BoMRepository(context, Mapper(context)).CreateBillOfMaterial(new()
        { ProductId = product.Id, Items = [new() { MaterialId = material.Id, BaseQuantity = 1,
            PrescribedQuantity = 1, UseDensityConversion = true, SubstituteMaterialIds = [substitute.Id] }] }, Guid.NewGuid());

        Assert.Equal("Bom.Density", result.Error.Code);
        Assert.Empty(await context.BillOfMaterials.ToListAsync());
    }

    [Fact]
    public async Task A_substitute_is_saved_with_its_own_quantity_in_its_own_stock_unit()
    {
        await using var context = Context();
        var (product, material, piece, _) = await Seed(context);
        var gram = new UnitOfMeasure { Name = "Gram", Symbol = "g" };
        var substitute = new Material { Name = "Anhydrous form", Code = "RMS010", Kind = MaterialKind.Raw };
        context.MaterialDepartments.Add(new() { Material = substitute, DepartmentId = DepartmentId, UoM = gram });
        await context.SaveChangesAsync();

        var result = await new BoMRepository(context, Mapper(context)).CreateBillOfMaterial(new()
        { ProductId = product.Id, Items = [new() { MaterialId = material.Id, BaseUoMId = piece.Id, BaseQuantity = 2, PrescribedQuantity = 1000,
            Substitutes = [new() { MaterialId = substitute.Id, BaseUoMId = gram.Id, BaseQuantity = 0.8m, PrescribedQuantity = 400 }] }] }, Guid.NewGuid());

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
        var saved = await context.BillOfMaterialItemSubstitutes.SingleAsync();
        Assert.Equal((substitute.Id, (Guid?)gram.Id, (decimal?)0.8m, (decimal?)400), (saved.SubstituteMaterialId, saved.BaseUoMId, saved.BaseQuantity, saved.PrescribedQuantity));
        Assert.True((await context.BillOfMaterialItems.SingleAsync()).IsSubstitutable);
    }

    [Theory]
    [InlineData(0, false, "Bom.SubstituteQuantity")]
    [InlineData(400, true, "Bom.MaterialUnit")]
    public async Task A_substitute_needs_a_positive_quantity_in_its_linked_unit(int prescribed, bool wrongUnit, string code)
    {
        await using var context = Context();
        var (product, material, piece, mg) = await Seed(context);
        var gram = new UnitOfMeasure { Name = "Gram", Symbol = "g" };
        var substitute = new Material { Name = "Anhydrous form", Code = "RMS011", Kind = MaterialKind.Raw };
        context.MaterialDepartments.Add(new() { Material = substitute, DepartmentId = DepartmentId, UoM = gram });
        await context.SaveChangesAsync();

        var result = await new BoMRepository(context, Mapper(context)).CreateBillOfMaterial(new()
        { ProductId = product.Id, Items = [new() { MaterialId = material.Id, BaseUoMId = piece.Id, BaseQuantity = 2, PrescribedQuantity = 1000,
            Substitutes = [new() { MaterialId = substitute.Id, BaseUoMId = wrongUnit ? mg.Id : gram.Id, BaseQuantity = prescribed / 500m, PrescribedQuantity = prescribed }] }] }, Guid.NewGuid());

        Assert.Equal(code, result.Error.Code);
        Assert.Empty(await context.BillOfMaterials.ToListAsync());
    }

    [Fact]
    public async Task A_stock_requisition_using_mg_for_a_piece_link_requires_BOM_repair()
    {
        await using var context = Context();
        var (product, material, piece, mg) = await Seed(context);
        var schedule = new ProductionSchedule { DepartmentId = product.DepartmentId };
        var batch = new ProductionScheduleProduct { ProductId = product.Id, ProductionSchedule = schedule };
        context.ProductionScheduleProducts.Add(batch);
        await context.SaveChangesAsync();

        var refused = await ProductionMaterialRequisitionRules.Validate(context, batch.Id,
            [new() { MaterialId = material.Id, UomId = mg.Id, Quantity = 20 }]);
        Assert.Equal("Requisition.BomRepairRequired", refused.Error.Code);
        var allowed = await ProductionMaterialRequisitionRules.Validate(context, batch.Id,
            [new() { MaterialId = material.Id, UomId = piece.Id, Quantity = 20 }]);
        Assert.True(allowed.IsSuccess);
        Assert.Empty(context.Requisitions);
    }

    private static async Task<(Product, Material, UnitOfMeasure, UnitOfMeasure)> Seed(ApplicationDbContext context)
    {
        var department = new Department { Id = DepartmentId, Name = "Production", Type = DepartmentType.Production };
        var piece = new UnitOfMeasure { Name = "Piece", Symbol = "piece", Category = UnitOfMeasureCategory.Countable };
        var mg = new UnitOfMeasure { Name = "Milligram", Symbol = "mg" };
        var product = new Product { Name = "Zinvite", Department = department };
        var material = new Material { Name = "Gelatin Capsule Shells - Zinvite", Code = "RMG033", Kind = MaterialKind.Raw };
        context.AddRange(department, piece, mg, product, material);
        context.MaterialDepartments.Add(new() { Material = material, DepartmentId = department.Id, UoM = piece });
        await context.SaveChangesAsync();
        return (product, material, piece, mg);
    }

    private static IMapper Mapper(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging(); services.AddHttpContextAccessor(); services.AddSingleton(context);
        services.AddAutoMapper(configuration => { }, typeof(APP.Mapper.OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new CurrentUser());

    private sealed class CurrentUser : ICurrentUserService
    { public Guid? UserId => null; public Guid? DepartmentId => BomMaterialUnitTests.DepartmentId; public string DepartmentType => "Production"; }
}
