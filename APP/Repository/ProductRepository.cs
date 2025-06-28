using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Equipments;
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
         if (context.Products.IgnoreQueryFilters().Any(p => p.Name == request.Name))
             return Error.Validation("Product.Name","Product with same name already exists");
         
         if (context.Products.IgnoreQueryFilters().Any(p => p.Code == request.Code))
             return Error.Validation("Product.Code","Product with code already exists");
         
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
             .Include(p => p.Equipment)
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
             .AsSplitQuery()
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
          var product = await context.Products
              .AsSplitQuery()
              .Include(product => product.Routes)
              .FirstOrDefaultAsync(p => p.Id == productId);
          if (product is null) return ProductErrors.NotFound(productId);
          
          if (product.Routes.Count != 0)
          {
              context.Routes.RemoveRange(product.Routes);
          }

          var routes = new List<Route>();
          
          foreach (var routeRequest in request.DistinctBy(r => r.OperationId).ToList())
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
            .AsSplitQuery()
            .Include(r => r.Operation)
            .Include(r => r.WorkCenters).ThenInclude(r => r.WorkCenter)
            .Include(r => r.ResponsibleUsers).ThenInclude(r => r.User)
            .Include(r => r.ResponsibleUsers).ThenInclude(r => r.ProductAnalyticalRawData).ThenInclude(r => r.Form)
            .Include(r => r.ResponsibleRoles).ThenInclude(r => r.Role)
            .Include(r => r.ResponsibleRoles).ThenInclude(r => r.ProductAnalyticalRawData).ThenInclude(r => r.Form)
            .Include(r => r.ResponsibleUsers).ThenInclude(r => r.ProductAnalyticalRawData).ThenInclude(r => r.ProductStandardTestProcedure)
            .Include(r => r.ResponsibleRoles).ThenInclude(r => r.ProductAnalyticalRawData).ThenInclude(r => r.ProductStandardTestProcedure)
            .Include(r => r.Resources).ThenInclude(rr => rr.Resource)
            .FirstOrDefaultAsync(r => r.Id == routeId);

        if (route == null)
            return Error.NotFound("Route.NotFound", $"Route with ID {routeId} not found.");

        var routeDto = mapper.Map<RouteDto>(route);
        return Result.Success(routeDto);
    }

    public async Task<Result<IEnumerable<RouteDto>>> GetRoutes(Guid productId)
    {
        var query = await context.Routes
            .OrderBy(r => r.Order)
            .AsSplitQuery()
            .Include(r => r.Operation)
            .Include(r => r.WorkCenters).ThenInclude(r => r.WorkCenter)
            .Include(r => r.ResponsibleUsers).ThenInclude(r => r.User)
            .Include(r => r.ResponsibleRoles).ThenInclude(r => r.Role)
            .Include(r => r.ResponsibleUsers).ThenInclude(r => r.ProductAnalyticalRawData).ThenInclude(r => r.ProductStandardTestProcedure)
            .Include(r => r.ResponsibleRoles).ThenInclude(r => r.ProductAnalyticalRawData).ThenInclude(r => r.ProductStandardTestProcedure)
            .Include(r => r.Resources).ThenInclude(rr => rr.Resource)
            .Where(r => r.ProductId == productId)
            .ToListAsync();
        
        return mapper.Map<List<RouteDto>>(query);
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
        var product = await context.Products
            .Include(p => p.Packages)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
        {
            return ProductErrors.NotFound(productId);
        }

        // Build a HashSet of existing MaterialIds for fast lookup
        var existingMaterialIds = product.Packages.Select(p => p.MaterialId).ToHashSet();

        foreach (var newPackage in request)
        {
            if (newPackage.DirectLinkMaterialId.HasValue)
            {
                // Check if this new package introduces a cycle
                if (HasCircularDependency(newPackage.MaterialId, newPackage.DirectLinkMaterialId.Value, product.Packages.ToList()))
                {
                    return Error.Failure("Product.Package", $"Circular dependency detected with MaterialId {newPackage.MaterialId} and DirectLinkMaterialId {newPackage.DirectLinkMaterialId}");
                }
            }
        }

        // Remove old packages if they exist
        if (product.Packages.Count != 0)
        {
            context.ProductPackages.RemoveRange(product.Packages);
        }

        // Map and add new packages
        foreach (var newPackage in request.Select(mapper.Map<ProductPackage>))
        {
            newPackage.CreatedById = userId;
            product.Packages.Add(newPackage);
        }

        await context.SaveChangesAsync();
        return product.Id;
    }

    
    private bool HasCircularDependency(Guid materialId, Guid directLinkMaterialId, List<ProductPackage> existingPackages)
    {
        var visited = new HashSet<Guid>();  // Track visited materials
        var currentMaterialId = directLinkMaterialId;

        while (true)
        {
            // If we encounter the starting materialId again, it's a cycle
            if (currentMaterialId == materialId)
            {
                return true;
            }

            // If this material was already checked, cycle detected
            if (!visited.Add(currentMaterialId))
            {
                return true;
            }

            // Find the next linked material
            var nextPackage = existingPackages.FirstOrDefault(p => p.MaterialId == currentMaterialId);
            if (nextPackage == null || !nextPackage.DirectLinkMaterialId.HasValue)
            {
                break; // No more links, exit loop
            }

            currentMaterialId = nextPackage.DirectLinkMaterialId.Value;
        }

        return false;
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

    public async Task<Result<IEnumerable<ProductPackageDto>>> GetProductPackages()
    {
        var query = await context.ProductPackages
            .Include(p => p.Product)
            .Include(p => p.Material)
            .ToListAsync();
        
        return mapper.Map<List<ProductPackageDto>>(query);
    }

    public async Task<Result> UpdateProductPackage(CreateProductPackageRequest request, Guid productPackageId, Guid userId)
    {
        var productPackage = await context.ProductPackages
            .Include(p => p.Product)
            .ThenInclude(p => p.Packages) // Include related packages for validation
            .FirstOrDefaultAsync(p => p.Id == productPackageId);

        if (productPackage == null)
            return Error.NotFound("ProductPackage.NotFound", $"Product package with ID {productPackageId} not found.");

        // Prevent self-referencing update
        if (request.DirectLinkMaterialId.HasValue && request.DirectLinkMaterialId.Value == request.MaterialId)
        {
            return Error.Failure("Product.Package", "DirectLinkMaterialId cannot be the same as MaterialId.");
        }

        // Check for circular dependency
        if (request.DirectLinkMaterialId.HasValue)
        {
            if (HasCircularDependency(request.MaterialId, request.DirectLinkMaterialId.Value, productPackage.Product.Packages.ToList()))
            {
                return Error.Failure("Product.Package", $"Circular dependency detected with MaterialId {productPackage.MaterialId} and DirectLinkMaterialId {productPackage.DirectLinkMaterialId}");
            }
        }

        // Map updated values
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
    
     // Create Equipment
    public async Task<Result<Guid>> CreateEquipment(CreateEquipmentRequest request, Guid userId)
    {
        var equipment = mapper.Map<Equipment>(request);
        equipment.CreatedById = userId;

        await context.Equipments.AddAsync(equipment);
        await context.SaveChangesAsync();

        return equipment.Id;
    }

    // Get Equipment by ID
    public async Task<Result<EquipmentDto>> GetEquipment(Guid equipmentId)
    {
        var equipment = await context.Equipments
            .Include(e => e.UoM)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == equipmentId);

        return equipment is null
            ? Error.NotFound("Equipment.NotFound", "Equipment with this Id not found")
            : mapper.Map<EquipmentDto>(equipment);
    }

    // Get paginated list of Equipments
    public async Task<Result<Paginateable<IEnumerable<EquipmentDto>>>> GetEquipments(int page, int pageSize, string searchQuery)
    {
        var query = context.Equipments
            .AsSplitQuery()
            .Include(e => e.UoM)
            .Include(e => e.Department)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, e => e.Name, e => e.MachineId);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<EquipmentDto>
        );
    }

    // Get all Equipments
    public async Task<Result<List<EquipmentDto>>> GetEquipments()
    {
        return mapper.Map<List<EquipmentDto>>(await context.Equipments
            .AsSplitQuery()
            .Include(e => e.UoM)
            .Include(e => e.Department)
            .ToListAsync());
    }

    // Update Equipment
    public async Task<Result> UpdateEquipment(CreateEquipmentRequest request, Guid equipmentId, Guid userId)
    {
        var existingEquipment = await context.Equipments.FirstOrDefaultAsync(e => e.Id == equipmentId);
        if (existingEquipment is null)
        {
            return Error.NotFound("Equipment.NotFound", "Equipment with this Id not found");
        }

        mapper.Map(request, existingEquipment);
        existingEquipment.LastUpdatedById = userId;

        context.Equipments.Update(existingEquipment);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete Equipment (soft delete)
    public async Task<Result> DeleteEquipment(Guid equipmentId, Guid userId)
    {
        var equipment = await context.Equipments.FirstOrDefaultAsync(e => e.Id == equipmentId);
        if (equipment is null)
        {
            return Error.NotFound("Equipment.NotFound", "Equipment with this Id not found");
        }

        equipment.DeletedAt = DateTime.UtcNow;
        equipment.LastDeletedById = userId;

        context.Equipments.Update(equipment);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}