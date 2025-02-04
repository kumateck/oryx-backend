using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
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
             .AsSplitQuery()
             .Include(p => p.BaseUoM)
             .Include(p => p.BasePackingUoM)
             .Include(p => p.BillOfMaterials)
             .ThenInclude(p => p.BillOfMaterial)
             .ThenInclude(p => p.Items.OrderBy(i => i.Order))
             .Include(p => p.Category)
             .Include(p => p.FinishedProducts)
             .Include(p => p.Packages)
             .Include(p => p.Routes.OrderBy(r => r.Order)).ThenInclude(p => p.WorkCenters)
             .Include(p => p.Routes.OrderBy(r => r.Order)).ThenInclude(p => p.ResponsibleUsers)
             .Include(p => p.Routes.OrderBy(r => r.Order)).ThenInclude(p => p.ResponsibleRoles)
             .Include(p => p.Routes.OrderBy(r => r.Order)).ThenInclude(p => p.Resources)
             .Include(p =>p.CreatedBy)
             .FirstOrDefaultAsync(p => p.Id == productId);

         return product is null ? ProductErrors.NotFound(productId) : mapper.Map<ProductDto>(product);
     }

     public async Task<Result<Paginateable<IEnumerable<ProductListDto>>>> GetProducts(int page, int pageSize, string searchQuery)
     {
         var query = context.Products
             .Include(p => p.BaseUoM)
             .Include(p => p.BasePackingUoM)
             .Include(p => p.Category)
             .AsQueryable();

         if (!string.IsNullOrEmpty(searchQuery))
         {
             query = query.WhereSearch(searchQuery, f => f.Name);
         }

         return await PaginationHelper.GetPaginatedResultAsync(
             query,
             page,
             pageSize,
             mapper.Map<ProductListDto>
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
     
     public async Task<Result> UpdateProductPackageDescription(UpdateProductPackageDescriptionRequest request, Guid productId, Guid userId)
     {
         var existingProduct = await context.Products.FirstOrDefaultAsync(p => p.Id == productId);
         if (existingProduct is null)
         {
             return ProductErrors.NotFound(productId);
         }

         existingProduct.PrimaryPackDescription = request.PrimaryPackDescription;
         existingProduct.SecondaryPackDescription = request.SecondaryPackDescription;
         existingProduct.TertiaryPackDescription = request.TertiaryPackDescription;
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

     public async Task<Result<ProductBillOfMaterialDto>> GetBillOfMaterialByProductId(Guid productId)
     {
         var bom = await context.ProductBillOfMaterials
             .Include(b => b.BillOfMaterial)
             .OrderByDescending(p => p.EffectiveDate)
             .FirstOrDefaultAsync(
             p => p.ProductId == productId && p.IsActive);

         if (bom is null)
         {
             return Error.NotFound("ProductBoM.NotFound", "Could not find bom for this product");
         }

         return mapper.Map<ProductBillOfMaterialDto>(bom);
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
     
      public async Task<Result> CreateRoute(List<CreateRouteRequest> request, Guid productId, Guid userId)
      {
          var product = await context.Products.Include(product => product.BillOfMaterials)
              .Include(product => product.Routes).Include(product => product.Packages).FirstOrDefaultAsync(p => p.Id == productId);
          if (product is null) return ProductErrors.NotFound(productId);
          
          if (product.Routes.Count != 0)
          {
              context.Routes.RemoveRange(product.Routes);
          }

          var routes = new List<Route>();
          
          foreach (var routeRequest in request)
          {
              routes.Add(mapper.Map<Route>(routeRequest));
          }
          product.Routes.AddRange(routes);
          await context.SaveChangesAsync();
          return Result.Success();
      }

    public async Task<Result<RouteDto>> GetRoute(Guid routeId)
    {
        var route = await context.Routes
            .Include(r => r.Operation)
            .Include(r => r.WorkCenters).ThenInclude(r => r.WorkCenter)
            .Include(r => r.ResponsibleUsers).ThenInclude(r => r.User)
            .Include(r => r.ResponsibleRoles).ThenInclude(r => r.Role)
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
            .OrderBy(r => r.Order)
            .Include(r => r.Operation)
            .Include(r => r.WorkCenters).ThenInclude(r => r.WorkCenter)
            .Include(r => r.ResponsibleUsers).ThenInclude(r => r.User)
            .Include(r => r.ResponsibleRoles).ThenInclude(r => r.Role)
            .Include(r => r.Resources).ThenInclude(rr => rr.Resource)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Operation.Name);
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
            .Include(route => route.ResponsibleRoles)
            .Include(route => route.ResponsibleUsers)
            .Include(route => route.WorkCenters)
            .FirstOrDefaultAsync(r => r.Id == routeId);

        if (route == null)
            return Error.NotFound("Route.NotFound", $"Route with ID {routeId} not found.");
        
        context.RouteResources.RemoveRange(route.Resources);
        context.RouteResponsibleRoles.RemoveRange(route.ResponsibleRoles);
        context.RouteResponsibleUsers.RemoveRange(route.ResponsibleUsers);
        context.RouteWorkCenters.RemoveRange(route.WorkCenters);
        
        mapper.Map(request, route);
        route.LastUpdatedById = userId;
        
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
    
    public async Task<Result<Guid>> CreateProductPackage(List<CreateProductPackageRequest> request, Guid productId, Guid userId)
    {
        var product = await context.Products.Include(product => product.Packages).FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
        {
            return ProductErrors.NotFound(productId);
        }

        if (product.Packages.Count != 0)
        {
            context.ProductPackages.RemoveRange(product.Packages);
        }

        foreach (var newPackage in request.Select(mapper.Map<ProductPackage>))
        {
            newPackage.CreatedById = userId;
            product.Packages.Add(newPackage);
        }
       
        await context.SaveChangesAsync();

        return product.Id;
    }

    public async Task<Result<ProductPackageDto>> GetProductPackage(Guid productPackageId)
    {
        var productPackage = await context.ProductPackages
            .Include(p => p.Product)
            .Include(p => p.Material)
            .FirstOrDefaultAsync(p => p.ProductId == productPackageId);

        if (productPackage == null)
            return Error.NotFound("ProductPackage.NotFound", $"Product package with ID {productPackageId} not found.");

        var productPackageDto = mapper.Map<ProductPackageDto>(productPackage);
        return Result.Success(productPackageDto);
    }

    public async Task<Result<Paginateable<IEnumerable<ProductPackageDto>>>> GetProductPackages(int page, int pageSize, string searchQuery = null)
    {
        var query = context.ProductPackages
            .Include(p => p.Product)
            .Include(p => p.Material)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, p => p.Material.Name, p => p.Product.Name);
        }

        var paginatedProductPackages = await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProductPackageDto>
        );

        return Result.Success(paginatedProductPackages);
    }

    public async Task<Result> UpdateProductPackage(CreateProductPackageRequest request, Guid productPackageId, Guid userId)
    {
        var productPackage = await context.ProductPackages
            .FirstOrDefaultAsync(p => p.ProductId == productPackageId);

        if (productPackage == null)
            return Error.NotFound("ProductPackage.NotFound", $"Product package with ID {productPackageId} not found.");

        mapper.Map(request, productPackage);
        productPackage.LastUpdatedById = userId;

        context.ProductPackages.Update(productPackage);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteProductPackage(Guid productPackageId, Guid userId)
    {
        var productPackage = await context.ProductPackages
            .FirstOrDefaultAsync(p => p.ProductId == productPackageId);

        if (productPackage == null)
            return Error.NotFound("ProductPackage.NotFound", $"Product package with ID {productPackageId} not found.");

        productPackage.DeletedAt = DateTime.UtcNow;
        productPackage.LastDeletedById = userId;

        context.ProductPackages.Update(productPackage);
        await context.SaveChangesAsync();

        return Result.Success();
    }
    
    public async Task<Result<Guid>> CreateFinishedProduct(List<CreateFinishedProductRequest> request, Guid productId, Guid userId)
    {
        var product = await context.Products.Include(product => product.FinishedProducts).FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
        {
            return ProductErrors.NotFound(productId);
        }
         
        if (product.FinishedProducts.Count != 0)
        {
            context.FinishedProducts.RemoveRange(product.FinishedProducts);
        }

        foreach (var newFinishedProduct in request.Select(mapper.Map<FinishedProduct>))
        {
            newFinishedProduct.CreatedById = userId;
            product.FinishedProducts.Add(newFinishedProduct);
        }
       
        await context.SaveChangesAsync();

        return product.Id;
    }
    
    public async Task<Result> ArchiveBillOfMaterial(Guid productId, Guid userId) 
    { 
        var product = await context.Products.Include(product => product.BillOfMaterials)
            .FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null) return ProductErrors.NotFound(productId);
        
        var bom = product.BillOfMaterials.FirstOrDefault(p => p.IsActive);

        if (bom is not null)
        {
            bom.IsActive = false;
            context.ProductBillOfMaterials.Update(bom);
            await context.SaveChangesAsync();
        }

        return Result.Success();
    }
    
    public async Task<Result<Guid>> CreateBatchManufacturingRecord(CreateBatchManufacturingRecord request)
    {
        var batchRecord = mapper.Map<BatchManufacturingRecord>(request);
        await context.BatchManufacturingRecords.AddAsync(batchRecord);
        await context.SaveChangesAsync();
        return batchRecord.Id;
    }
    
    public async Task<Result<Paginateable<IEnumerable<BatchManufacturingRecordDto>>>> GetBatchManufacturingRecords(int page, int pageSize, string searchQuery = null)
    {
        var query = context.BatchManufacturingRecords
            .Include(p => p.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, p => p.BatchNumber, p => p.Product.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<BatchManufacturingRecordDto>
        );
    }

    public async Task<Result<BatchManufacturingRecordDto>> GetBatchManufacturingRecord(Guid id)
    {
        return mapper.Map<BatchManufacturingRecordDto>(
            await context.BatchManufacturingRecords.FirstOrDefaultAsync(b => b.Id == id));
    }

    public async Task<Result> UpdateBatchManufacturingRecord(CreateBatchManufacturingRecord request, Guid id)
    {
        var batchRecord = await context.BatchManufacturingRecords.FirstOrDefaultAsync(p => p.Id == id);
        if (batchRecord is null)
        {
            return ProductErrors.NotFound(id);
        }

        mapper.Map(request, batchRecord);
        context.BatchManufacturingRecords.Update(batchRecord);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Guid>> CreateBatchPackagingRecord(CreateBatchPackagingRecord request)
    {
        var batchRecord = mapper.Map<BatchManufacturingRecord>(request);
        await context.BatchManufacturingRecords.AddAsync(batchRecord);
        await context.SaveChangesAsync();
        return batchRecord.Id;
    }
    
    public async Task<Result<Paginateable<IEnumerable<BatchPackagingRecordDto>>>> GetBatchPackagingRecords(int page, int pageSize, string searchQuery = null)
    {
        var query = context.BatchPackagingRecords
            .Include(p => p.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, p => p.BatchNumber, p => p.Product.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<BatchPackagingRecordDto>
        );
    }

    public async Task<Result<BatchPackagingRecordDto>> GetBatchPackagingRecord(Guid id)
    {
        return mapper.Map<BatchPackagingRecordDto>(
            await context.BatchPackagingRecords.FirstOrDefaultAsync(b => b.Id == id));
    }

    public async Task<Result> UpdateBatchPackagingRecord(CreateBatchManufacturingRecord request, Guid id)
    {
        var batchRecord = await context.BatchPackagingRecords.FirstOrDefaultAsync(p => p.Id == id);
        if (batchRecord is null)
        {
            return ProductErrors.NotFound(id);
        }

        mapper.Map(request, batchRecord);
        context.BatchPackagingRecords.Update(batchRecord);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}