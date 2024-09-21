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
    
    public async Task<Result<Guid>> UpdateItem(CreateItemRequest request, Guid itemId, string itemType, Guid userId)
    {
        switch (itemType)
        {
            case nameof(ProductCategory):
                var productCategory = await context.ProductCategories.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, productCategory);
                productCategory.LastUpdatedById = userId;
                context.ProductCategories.Update(productCategory);
                await context.SaveChangesAsync();
                return productCategory.Id;
        
            case nameof(Resource):
                var resource = await context.Resources.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, resource);
                resource.LastUpdatedById = userId;
                context.Resources.Update(resource);
                await context.SaveChangesAsync();
                return resource.Id;
        
            case nameof(UnitOfMeasure):
                var unitOfMeasure = await context.UnitOfMeasures.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, unitOfMeasure);
                unitOfMeasure.LastUpdatedById = userId;
                context.UnitOfMeasures.Update(unitOfMeasure);
                await context.SaveChangesAsync();
                return unitOfMeasure.Id;
        
            case nameof(WorkCenter):
                var workCenter = await context.WorkCenters.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, workCenter);
                workCenter.LastUpdatedById = userId;
                context.WorkCenters.Update(workCenter);
                await context.SaveChangesAsync();
                return workCenter.Id;
        
            case nameof(Operation):
                var operation = await context.Operations.FirstOrDefaultAsync(p => p.Id == itemId);
                mapper.Map(request, operation);
                operation.LastUpdatedById = userId;
                context.Operations.Update(operation);
                await context.SaveChangesAsync();
                return operation.Id;
        
            default:
                return Error.Validation("Item", "Invalid item type");
        }
    }
    
    public async Task<Result> SoftDeleteItem(Guid itemId, string itemType, Guid userId)
    {
        var currentTime = DateTime.UtcNow;  // or use DateTime.Now based on your timezone requirements

        switch (itemType)
        {
            case nameof(ProductCategory):
                var productCategory = await context.ProductCategories.FirstOrDefaultAsync(p => p.Id == itemId);
                if (productCategory == null)
                    return Error.Validation("ProductCategory", "Not found");
                productCategory.DeletedAt = currentTime;
                productCategory.LastDeletedById = userId;
                context.ProductCategories.Update(productCategory);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(Resource):
                var resource = await context.Resources.FirstOrDefaultAsync(p => p.Id == itemId);
                if (resource == null)
                    return Error.Validation("Resource", "Not found");
                resource.DeletedAt = currentTime;
                resource.LastDeletedById = userId;
                context.Resources.Update(resource);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(UnitOfMeasure):
                var unitOfMeasure = await context.UnitOfMeasures.FirstOrDefaultAsync(p => p.Id == itemId);
                if (unitOfMeasure == null)
                    return Error.Validation("UnitOfMeasure", "Not found");
                unitOfMeasure.DeletedAt = currentTime;
                unitOfMeasure.LastDeletedById = userId;
                context.UnitOfMeasures.Update(unitOfMeasure);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(WorkCenter):
                var workCenter = await context.WorkCenters.FirstOrDefaultAsync(p => p.Id == itemId);
                if (workCenter == null)
                    return Error.Validation("WorkCenter", "Not found");
                workCenter.DeletedAt = currentTime;
                workCenter.LastDeletedById = userId;
                context.WorkCenters.Update(workCenter);
                await context.SaveChangesAsync();
                return Result.Success();
            
            case nameof(Operation):
                var operation = await context.Operations.FirstOrDefaultAsync(p => p.Id == itemId);
                if (operation == null)
                    return Error.Validation("Operation", "Not found");
                operation.DeletedAt = currentTime;
                operation.LastDeletedById = userId;
                context.Operations.Update(operation);
                await context.SaveChangesAsync();
                return Result.Success();
            
            default:
                return Error.Validation("Item", "Invalid item type");
        }    
    }
}