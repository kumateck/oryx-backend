using APP.Mapper;
using AutoMapper;
using DOMAIN.Entities.Products;
using Xunit;

namespace APP.Tests.Repository;

public class ProductPackageSubstituteMappingTests
{
    private readonly IMapper mapper = new MapperConfiguration(config => config.AddProfile<OryxMapper>()).CreateMapper();

    [Fact]
    public void Quantified_substitutes_keep_their_material_and_own_quantity()
    {
        var substituteId = Guid.Parse("0199614c-f091-7766-9463-5d7b1c0b69e6");
        var request = new CreateProductPackageRequest
        {
            MaterialId = Guid.Parse("0199614c-f091-7b2d-9dd8-b14aaf4de44f"),
            ProductPackingId = Guid.Parse("019a977d-6042-7f9c-8cc8-4d9a0d9895d2"),
            BaseQuantity = 1,
            PrescribedQuantity = 30600,
            SubstituteMaterialIds = [substituteId],
            Substitutes = [new() { MaterialId = substituteId, PrescribedQuantity = 30599.125m }],
        };

        var result = mapper.Map<ProductPackage>(request);

        Assert.Equal(request.MaterialId, result.MaterialId);
        Assert.Equal(request.ProductPackingId, result.ProductPackingId);
        Assert.Equal(30600m, result.PrescribedQuantity);
        var substitute = Assert.Single(result.Substitutes);
        Assert.Equal(substituteId, substitute.SubstituteMaterialId);
        Assert.Equal(30599.125m, substitute.PrescribedQuantity);
    }

    [Fact]
    public void Quantified_substitutes_take_precedence_over_legacy_ids()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var result = mapper.Map<ProductPackage>(new CreateProductPackageRequest
        {
            SubstituteMaterialIds = [Guid.NewGuid()],
            Substitutes = [new() { MaterialId = first, PrescribedQuantity = 30600 },
                new() { MaterialId = second, PrescribedQuantity = 12.75m }],
        });

        Assert.Equal(new[] { first, second }, result.Substitutes.Select(item => item.SubstituteMaterialId));
        Assert.Equal(new decimal?[] { 30600, 12.75m }, result.Substitutes.Select(item => item.PrescribedQuantity));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Legacy_ids_still_map_without_inventing_a_substitute_quantity(bool nullTypedList)
    {
        var materialId = Guid.NewGuid();
        var request = new CreateProductPackageRequest { SubstituteMaterialIds = [materialId] };
        if (nullTypedList) request.Substitutes = null!;

        var substitute = Assert.Single(mapper.Map<ProductPackage>(request).Substitutes);

        Assert.Equal(materialId, substitute.SubstituteMaterialId);
        Assert.Null(substitute.PrescribedQuantity);
    }

    [Fact]
    public void A_row_without_substitutes_still_maps()
    {
        Assert.Empty(mapper.Map<ProductPackage>(new CreateProductPackageRequest()).Substitutes);
    }
}
