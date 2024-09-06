using APP.IRepository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class CollectionRepository(ApplicationDbContext context, IMapper mapper) : ICollectionRepository
{
    public async Task<Result<IEnumerable<CollectionItemDto>>> GetItemCollection(string itemType)
    {
        switch (itemType)
        {
            case nameof(ProductCategory):
                return mapper.Map<List<CollectionItemDto>>(await context.ProductCategories.ToListAsync());
            
            case nameof(Resource):
                return mapper.Map<List<CollectionItemDto>>(await context.Resources.ToListAsync());
            
            case nameof(UnitOfMeasure):
                return mapper.Map<List<CollectionItemDto>>(await context.UnitOfMeasures.ToListAsync());
            
            case nameof(Material):
                return mapper.Map<List<CollectionItemDto>>(await context.Materials.ToListAsync());
            
            case nameof(Product):
                return mapper.Map<List<CollectionItemDto>>(await context.Products.ToListAsync());
            
            default:
                return Error.Validation("Item", "Invalid item type");
        }
    }

    public Result<IEnumerable<string>> GetItemTypes()
    {
        return new List<string>
        {
            nameof(ProductCategory),
            nameof(Resource),
            nameof(UnitOfMeasure),
            nameof(Material),
            nameof(Product)
        };
    }
    
    public async Task<Result<Guid>> CreateItem(CreateItemRequest request, string itemType)
    {
        switch (itemType)
        {
            case nameof(ProductCategory):
                var productCategory = mapper.Map<ProductCategory>(request);
                await context.ProductCategories.AddAsync(productCategory);
                await context.SaveChangesAsync();
                return productCategory.Id;
            
            case nameof(Resource):
                var resource = mapper.Map<Resource>(request);
                await context.Resources.AddAsync(resource);
                await context.SaveChangesAsync();
                return resource.Id;
            
            case nameof(UnitOfMeasure):
                var unitOfMeasure = mapper.Map<UnitOfMeasure>(request);
                await context.UnitOfMeasures.AddAsync(unitOfMeasure);
                await context.SaveChangesAsync();
                return unitOfMeasure.Id;
            
            case nameof(Material):
                var material = mapper.Map<Material>(request);
                await context.Materials.AddAsync(material);
                await context.SaveChangesAsync();
                return material.Id;
            
            default:
                return Error.Validation("Item", "Invalid item type");
        }
    }
}