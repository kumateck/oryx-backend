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
        return itemType switch
        {
            nameof(ProductCategory) => mapper.Map<List<CollectionItemDto>>(
                await context.ProductCategories.ToListAsync()),
            nameof(Resource) => mapper.Map<List<CollectionItemDto>>(await context.Resources.ToListAsync()),
            nameof(UnitOfMeasure) => mapper.Map<List<CollectionItemDto>>(await context.UnitOfMeasures.ToListAsync()),
            nameof(Material) => mapper.Map<List<CollectionItemDto>>(await context.Materials.ToListAsync()),
            nameof(Product) => mapper.Map<List<CollectionItemDto>>(await context.Products.ToListAsync()),
            _ => Error.Validation("Item", "Invalid item type")
        };
    }

    public Result<IEnumerable<string>> GetItemTypes()
    {
        return new List<string>
        {
            nameof(ProductCategory),
            nameof(Resource),
            nameof(UnitOfMeasure),
            nameof(Material),
            nameof(Product),
            nameof(WorkCenter),
            nameof(Operation)
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
            
            case nameof(WorkCenter):
                var workCenter = mapper.Map<WorkCenter>(request);
                await context.WorkCenters.AddAsync(workCenter);
                await context.SaveChangesAsync();
                return workCenter.Id;
            
            case nameof(Operation):
                var operation = mapper.Map<Operation>(request);
                await context.Operations.AddAsync(operation);
                await context.SaveChangesAsync();
                return operation.Id;
            
            default:
                return Error.Validation("Item", "Invalid item type");
        }
    }
}