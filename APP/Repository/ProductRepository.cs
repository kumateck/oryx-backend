using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Routes;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

 public class ProductRepository(ApplicationDbContext context, IMapper mapper) : IProductRepository
 {
     public async Task<Result<Guid>> CreateProduct(CreateProductRequest request, Guid userId)
     {
         var product = mapper.Map<Product>(request);
         product.CreatedById = userId;
         await context.Products.AddAsync(product); 
         await context.SaveChangesAsync();

         return product.Id;
     }
     
     public async Task<Result<ProductDto>> GetProduct(Guid productId) 
     { 
         var product = await context.Products
             .Include(p => p.BillOfMaterials)
             .Include(p => p.FinishedProducts)
             .FirstOrDefaultAsync(p => p.Id == productId);

         return product is null ? ProductErrors.NotFound(productId) : mapper.Map<ProductDto>(product);
     }

     public async Task<Result<Paginateable<IEnumerable<ProductDto>>>> GetProducts(int page, int pageSize, string searchQuery)
     {
         var query = context.Products
             .AsSplitQuery()
             .Include(p => p.BillOfMaterials)
             .Include(p => p.Category)
             .Include(p => p.FinishedProducts)
             .AsQueryable();

         if (!string.IsNullOrEmpty(searchQuery))
         {
             query = query.WhereSearch(searchQuery, f => f.Name);
         }

         return await PaginationHelper.GetPaginatedResultAsync(
             query,
             page,
             pageSize,
             mapper.Map<ProductDto>
         );
     }

     public async Task<Result> UpdateProduct(UpdateProductRequest request, Guid productId, Guid userId)
     {
         var existingProduct = await context.Products.FirstOrDefaultAsync(p => p.Id == productId);
         if (existingProduct is null)
         {
             return ProductErrors.NotFound(productId);
         }

         mapper.Map(request, existingProduct);
         existingProduct.LastUpdatedById = userId;

         context.Products.Update(existingProduct);
         await context.SaveChangesAsync();
         return Result.Success();
     }

     public async Task<Result> DeleteProduct(Guid productId, Guid userId)
     {
         var product = await context.Products.FirstOrDefaultAsync(p => p.Id == productId);
         if (product is null)
         {
             return ProductErrors.NotFound(productId);
         }

         product.DeletedAt = DateTime.UtcNow;
         product.LastDeletedById = userId;
         context.Products.Update(product);
         await context.SaveChangesAsync();
         return Result.Success();
     }
     
     public async Task<Result<Guid>> CreateBillOfMaterials(CreateProductBillOfMaterialRequest request, Guid productId) 
     { 
         var bom = mapper.Map<ProductBillOfMaterial>(request);
         
         await context.ProductBillOfMaterials.AddAsync(bom); 
         await context.SaveChangesAsync(); 
         return bom.Id;
     }
      
     public async Task<Result> UpdateBillOfMaterials(CreateProductBillOfMaterialRequest request, Guid bomId) 
     { 
         var existingBom = await context.ProductBillOfMaterials.FirstOrDefaultAsync(p => p.Id == bomId);
         
         if (existingBom is null)
         {
             return Error.NotFound("ProductBoM.NotFound", "Could not find bom for this product");
         }

         mapper.Map(request, existingBom);

         context.ProductBillOfMaterials.Update(existingBom);
         await context.SaveChangesAsync();
         return Result.Success();
     }

     public async Task<Result> DeleteBillOfMaterials(Guid bomId, Guid userId) 
     { 
         var bom = await context.ProductBillOfMaterials.FirstOrDefaultAsync(p => p.Id == bomId);
         
         if (bom is null)
         {
             return Error.NotFound("ProductBoM.NotFound", "Could not find bom for this product");
         }

         bom.DeletedAt = DateTime.UtcNow;
         bom.LastDeletedById = userId;
         context.ProductBillOfMaterials.Update(bom);
         await context.SaveChangesAsync();
         return Result.Success(); 
     }
     
      public async Task<Result<Guid>> CreateRoute(CreateRouteRequest request, Guid userId)
      {
          var route = mapper.Map<Route>(request);
          route.Resources = request.ResourceIds.Select(r => new RouteResource 
          { 
              ResourceId = r
          }).ToList();
          route.CreatedById = userId;
              
          await context.Routes.AddAsync(route);
          await context.SaveChangesAsync();

          return Result.Success(route.Id);
      }

    public async Task<Result<RouteDto>> GetRoute(Guid routeId)
    {
        var route = await context.Routes
            .Include(r => r.Operation)
            .Include(r => r.WorkCenter)
            .Include(r => r.BillOfMaterialItem)
            .Include(r => r.Resources).ThenInclude(rr => rr.Resource)
            .FirstOrDefaultAsync(r => r.Id == routeId);

        if (route == null)
            return Error.NotFound("Route.NotFound", $"Route with ID {routeId} not found.");

        var routeDto = mapper.Map<RouteDto>(route);
        return Result.Success(routeDto);
    }

    public async Task<Result<Paginateable<IEnumerable<RouteDto>>>> GetRoutes(int page, int pageSize, string searchQuery = null)
    {
        var query = context.Routes
            .Include(r => r.Operation)
            .Include(r => r.WorkCenter)
            .Include(r => r.BillOfMaterialItem)
            .Include(r => r.Resources).ThenInclude(rr => rr.Resource)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(r => r.Operation.Name.Contains(searchQuery) || r.WorkCenter.Name.Contains(searchQuery));
        }

        var paginatedRoutes = await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<RouteDto>
        );

        return Result.Success(paginatedRoutes);
    }

    public async Task<Result> UpdateRoute(UpdateRouteRequest request, Guid routeId, Guid userId)
    {
        var route = await context.Routes
            .Include(r => r.Resources)
            .FirstOrDefaultAsync(r => r.Id == routeId);

        if (route == null)
            return Error.NotFound("Route.NotFound", $"Route with ID {routeId} not found.");

        mapper.Map(request, route);
        route.LastUpdatedById = userId;

        // Update Route details
        // route.OperationId = request.OperationId;
        // route.WorkCenterId = request.WorkCenterId;
        // route.BillOfMaterialItemId = request.BillOfMaterialItemId;
        // route.EstimatedTime = request.EstimatedTime;
        // route.LastUpdatedById = userId;

        // Remove existing resources
        context.RouteResources.RemoveRange(route.Resources);

        // Add new resources
        route.Resources = request.ResourceIds.Select(r => new RouteResource
        {
            ResourceId = r,
            RouteId = routeId
        }).ToList();

        context.Routes.Update(route);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteRoute(Guid routeId, Guid userId)
    {
        var route = await context.Routes
            .Include(r => r.Resources)
            .FirstOrDefaultAsync(r => r.Id == routeId);

        if (route == null)
            return Error.NotFound("Route.NotFound", $"Route with ID {routeId} not found.");

        route.DeletedAt = DateTime.UtcNow;
        route.LastDeletedById = userId;

        context.Routes.Update(route);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}