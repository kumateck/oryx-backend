using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.AnalyticalTestRequests;
using DOMAIN.Entities.MaterialSpecifications;
using DOMAIN.Entities.ProductSpecifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>Every edit of a specification is a revision and must state its reason, which every linked record keeps.</summary>
public sealed class SpecificationRevisionReasonTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("Typo")]
    public async Task A_material_specification_cannot_be_revised_without_a_reason(string? reason)
    {
        await using var db = Context();
        var (first, _) = await SeedMaterial(db);

        var result = await new MaterialSpecificationRepository(db, Mapper(db)).UpdateMaterialSpecification(first, MaterialUpdate(reason));

        Assert.True(result.IsFailure);
        Assert.Equal("MaterialSpecification.RevisionReason", result.Errors.Single().Code);
        Assert.All(await db.MaterialSpecifications.ToListAsync(), spec => Assert.Equal("01", spec.RevisionNumber));
    }

    [Fact]
    public async Task A_material_specification_revision_records_its_reason_on_every_linked_material()
    {
        await using var db = Context();
        var (first, second) = await SeedMaterial(db);

        var result = await new MaterialSpecificationRepository(db, Mapper(db))
            .UpdateMaterialSpecification(first, MaterialUpdate("  Assay limit changed to 98.0–102.0% per BP 2026.  "));

        Assert.True(result.IsSuccess);
        foreach (var id in new[] { first, second })
        {
            var spec = await db.MaterialSpecifications.SingleAsync(item => item.Id == id);
            Assert.Equal("02", spec.RevisionNumber);
            Assert.Equal("Assay limit changed to 98.0–102.0% per BP 2026.", spec.RevisionReason);
        }
    }

    [Fact]
    public async Task A_product_specification_needs_a_reason_and_records_it()
    {
        await using var db = Context();
        var id = Guid.NewGuid();
        db.ProductSpecifications.Add(new ProductSpecification { Id = id, SpecificationNumber = "SPC/P/001", RevisionNumber = "01",
            SupersedesNumber = "None", ProductId = Guid.NewGuid(), FormId = Guid.NewGuid(), UserId = Guid.NewGuid() });
        await db.SaveChangesAsync();
        var repository = new ProductSpecificationRepository(db, Mapper(db));

        var refused = await repository.UpdateProductSpecification(id, ProductUpdate(""));
        var revised = await repository.UpdateProductSpecification(id, ProductUpdate("Dissolution test added after validation."));

        Assert.Equal("ProductSpecification.RevisionReason", refused.Errors.Single().Code);
        Assert.True(revised.IsSuccess);
        Assert.Equal("Dissolution test added after validation.", (await db.ProductSpecifications.SingleAsync()).RevisionReason);
    }

    private static UpdateMaterialSpecificationRequest MaterialUpdate(string? reason) => new()
    {
        SpecificationNumber = "SPC/M/001", RevisionNumber = "02", SupersedesNumber = "01", EffectiveDate = DateTime.UtcNow,
        ReviewDate = DateTime.UtcNow.AddYears(1), FormId = Guid.NewGuid(), RevisionReason = reason!,
    };

    private static UpdateProductSpecificationRequest ProductUpdate(string reason) => new()
    {
        SpecificationNumber = "SPC/P/001", RevisionNumber = "02", SupersedesNumber = "01", EffectiveDate = DateTime.UtcNow,
        ReviewDate = DateTime.UtcNow.AddYears(1), FormId = Guid.NewGuid(), TestStage = TestStage.Finished, RevisionReason = reason,
    };

    /// <summary>Two materials share one specification number, as the editor links them.</summary>
    private static async Task<(Guid First, Guid Second)> SeedMaterial(ApplicationDbContext db)
    {
        var first = Guid.NewGuid(); var second = Guid.NewGuid();
        foreach (var id in new[] { first, second })
            db.MaterialSpecifications.Add(new MaterialSpecification { Id = id, SpecificationNumber = "SPC/M/001", RevisionNumber = "01",
                SupersedesNumber = "None", MaterialId = Guid.NewGuid(), FormId = Guid.NewGuid(), UserId = Guid.NewGuid() });
        await db.SaveChangesAsync();
        return (first, second);
    }

    private static IMapper Mapper(ApplicationDbContext db)
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddHttpContextAccessor(); services.AddSingleton(db);
        services.AddAutoMapper(configuration => { }, typeof(APP.Mapper.OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false)).Options, new CurrentUser());

    private sealed class CurrentUser : ICurrentUserService
    { public Guid? UserId => null; public Guid? DepartmentId => null; public string DepartmentType => "NonProduction"; }
}
