using AutoMapper;
using DOMAIN.Entities.Products;

namespace APP.Mapper.Resolvers;

public class ProductBoMResolver(IMapper mapper) : IValueResolver<Product, ProductDto, ProductBillOfMaterialDto>
{
    public ProductBillOfMaterialDto Resolve(Product source, ProductDto destination, ProductBillOfMaterialDto destMember,
        ResolutionContext context)
    {
        return  mapper.Map<ProductBillOfMaterialDto>(source.BillOfMaterials
            .OrderByDescending(p => p.EffectiveDate)
            .SingleOrDefault(p => p.IsActive));
    }
}

public class OutdatedProductBoMResolver(IMapper mapper)
    : IValueResolver<Product, ProductDto, List<ProductBillOfMaterialDto>>
{
    public List<ProductBillOfMaterialDto> Resolve(Product source, ProductDto destination, List<ProductBillOfMaterialDto> destMember, ResolutionContext context)
    {
        var mostRecentActiveBoM = source.BillOfMaterials
            .OrderByDescending(p => p.EffectiveDate)
            .SingleOrDefault(p => p.IsActive);

        return mapper.Map<List<ProductBillOfMaterialDto>>(source.BillOfMaterials
            .Where(p => p != mostRecentActiveBoM)
            .ToList());
    }
}