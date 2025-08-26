using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Routes;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SHARED;
using SHARED.Requests;

namespace APP.Repository;

 public class ProductRepository(ApplicationDbContext context, IMapper mapper) : IProductRepository
 {
     public async Task<Result<Guid>> CreateProduct(CreateProductRequest request, Guid userId)
     {
         if (context.Products.IgnoreQueryFilters().Any(p => p.Name == request.Name))
             return Error.Validation("Product.Name","Product with same name already exists");
         
         if (context.Products.IgnoreQueryFilters().Any(p => p.Code == request.Code))
             return Error.Validation("Product.Code","Product with code already exists");
         
         if(request.Price < 0)
             return Error.Validation("Product.Price","Product Price must be greater than 0");
         
         var product = mapper.Map<Product>(request);
         product.CreatedById = userId;
         product.Prices.Add(new ProductPrices
         {
             Price = request.Price,
             Date = DateTime.UtcNow
         });
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

         if (existingProduct.Price != request.Price)
         {
             existingProduct.Prices.Add(new ProductPrices
             {
                 Price = request.Price,
                 Date = DateTime.UtcNow
             });
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
             .AsSplitQuery()
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
        //var existingMaterialIds = product.Packages.Select(p => p.MaterialId).ToHashSet();

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

    public async Task<Result<IEnumerable<ProductPackageDto>>> GetProductPackages(Guid productId)
    {
        var query = await context.ProductPackages
            .AsSplitQuery()
            .Include(p => p.Material)
            .Where(p => p.ProductId == productId)
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
    
    public async Task<Result<Guid>> CreateProductPacking(List<CreateProductPacking> request, Guid productId, Guid userId)
    {
        var product = await context.Products
            .AsSingleQuery()
            .Include(product => product.Packings)
            .ThenInclude(p => p.PackingLists)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
        {
            return ProductErrors.NotFound(productId);
        }
        
        
        if (product.Packings.Count != 0)
        {
            context.ProductPackings.RemoveRange(product.Packings);
        }

        foreach (var packing in request)
        {
            var productPacking = mapper.Map<ProductPacking>(packing);
            productPacking.ProductId = productId;
            await context.ProductPackings.AddAsync(productPacking);
        }
        
        await context.SaveChangesAsync();
        return product.Id;
    }

    public async Task<Result<IEnumerable<ProductPackingDto>>> GetProductPackings(Guid productId)
    {
        var query = await context.ProductPackings
            .AsSplitQuery()
            .Include(p => p.PackingLists)
            .ThenInclude(p => p.Uom)
            .Where(p => p.ProductId == productId)
            .ToListAsync();
        
        return mapper.Map<List<ProductPackingDto>>(query);
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
    
    public async Task<Result> ImportProductsFromExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return UploadErrors.EmptyFile;

        var products = new List<Product>();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return UploadErrors.WorksheetNotFound;

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var header = worksheet.Cells[1, col].Text.Trim();
            if (!string.IsNullOrEmpty(header))
                headers[header] = col;
        }

        var requiredHeaders = new[]
        {
            "PRODUCT NAME", "PRODUCT CODE", "CATEGORY", "BASE UOM", "BASE QUANTITY",
            "BASE PACKING UOM", "BASE PACKING QUANTITY", "EQUIPMENT", "FULL BATCH SIZE", "DEPARTMENT CODE", "LABEL CLAIMS"
        };

        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var row1 = row;
            var getCell = (string header) => worksheet.Cells[row1, headers[header]].Text.Trim();

            var categoryName = getCell("CATEGORY").ToLower();
            var baseUomName = getCell("BASE UOM").ToLower();
            var basePackingUomName = getCell("BASE PACKING UOM").ToLower();
            var equipmentName = getCell("EQUIPMENT").ToLower();
            var departmentCode = getCell("DEPARTMENT CODE");
            var productCode = getCell("PRODUCT CODE");
            
            var existingProduct = await context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Code != null && p.Code == productCode);
            if (existingProduct is not null) continue;

            var category = await context.ProductCategories.FirstOrDefaultAsync(c => c.Name != null &&  c.Name.ToLower() == categoryName);
            var baseUom = await context.UnitOfMeasures.FirstOrDefaultAsync(u => u.Name != null && u.Name.ToLower() == baseUomName);
            var basePackingUom = await context.UnitOfMeasures.FirstOrDefaultAsync(u => u.Name != null && u.Name.ToLower() == basePackingUomName);
            var equipment = await context.Equipments.FirstOrDefaultAsync(e => e.Name != null && e.Name.ToLower() == equipmentName);
            var department = await context.Departments.FirstOrDefaultAsync(d => d.Code == departmentCode);
            
            var product = new Product
            {
                Name = getCell("PRODUCT NAME"),
                Code = productCode,
                GenericName = getCell("GENERIC NAME"),
                StorageCondition = getCell("STORAGE CONDITION"),
                PackageStyle = getCell("PACK STYLE"),
                FilledWeight = getCell("FILLED VOLUME"),
                ShelfLife = getCell("SHELF LIFE"),
                ActionUse = getCell("ACTION AND USE"),
                FdaRegistrationNumber = "", // Not provided in headers
                MasterFormulaNumber = "",   // Not provided in headers
                PrimaryPackDescription = "",
                SecondaryPackDescription = "",
                TertiaryPackDescription = "",
                CategoryId = category?.Id,
                BaseUomId = baseUom?.Id,
                BasePackingUomId = basePackingUom?.Id,
                EquipmentId = equipment?.Id,
                DepartmentId = department?.Id,
                BaseQuantity = decimal.TryParse(getCell("BASE QUANTITY"), out var bq) ? bq : 0,
                BasePackingQuantity = decimal.TryParse(getCell("BASE PACKING QUANTITY"), out var bpq) ? bpq : 0,
                FullBatchSize = decimal.TryParse(getCell("FULL BATCH SIZE"), out var fbs) ? fbs : 0,
                LabelClaim = getCell("LABEL CLAIMS"),
            };

            products.Add(product);
        }
        
        var existingCodes = await context.Products
            .IgnoreQueryFilters()
            .Where(p => products.Select(np => np.Code).Contains(p.Code))
            .Select(p => p.Code)
            .ToListAsync();

        var newProducts = products
            .Where(p => !existingCodes.Contains(p.Code))
            .DistinctBy(p => p.Code) 
            .ToList();
        
        await context.Products.AddRangeAsync(newProducts);
        await context.SaveChangesAsync();

        return Result.Success();
    }
    
    public async Task<Result> ImportProductBomFromExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return UploadErrors.EmptyFile;

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return UploadErrors.WorksheetNotFound;

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var header = worksheet.Cells[1, col].Text.Trim();
            if (!string.IsNullOrEmpty(header))
                headers[header] = col;
        }

        var requiredHeaders = new[]
        {
            "PRODUCT NAME", "PRODUCT CODE", "ORDER", "MATERIAL TYPE",
            "COMPONENT MATERIAL", "COMPONENT MATERIAL CODE", "QUANTITY", "UOM", "GRADE", "CAS NUMBER", "FUNCTION"
        };

        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        var bomMap = new Dictionary<string, BillOfMaterial>(); // Key: productCode

        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            string GetCell(string header) => worksheet.Cells[row, headers[header]].Text.Trim();

            var productCode = GetCell("PRODUCT CODE");
            var materialCode = GetCell("COMPONENT MATERIAL CODE");
            var uomName = GetCell("UOM");
            var materialTypeName = GetCell("MATERIAL TYPE");

            var product = await context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Code == productCode);
            if (product == null) continue;

            var material = context.Materials.FirstOrDefault(m => m.Code == materialCode);
            if (material == null) continue;

            var uom = await context.UnitOfMeasures.FirstOrDefaultAsync(u => u.Name.ToLower() == uomName.ToLower());
            var materialType = await context.MaterialTypes.FirstOrDefaultAsync(mt => mt.Name.ToLower() == materialTypeName.ToLower());

            if (!bomMap.TryGetValue(productCode, out var billOfMaterial))
            {
                billOfMaterial = new BillOfMaterial
                {
                    ProductId = product.Id,
                    Version = 1, // or dynamic version logic
                    IsActive = true,
                    Items = []
                };
                context.BillOfMaterials.Add(billOfMaterial);
                bomMap[productCode] = billOfMaterial;

                // Create a ProductBillOfMaterial entry
                var productBom = new ProductBillOfMaterial
                {
                    ProductId = product.Id,
                    BillOfMaterial = billOfMaterial,
                    Quantity = decimal.TryParse(GetCell("QUANTITY"), out var quantity) ? quantity : 0,
                    Version = 1,
                    EffectiveDate = DateTime.UtcNow,
                    IsActive = true
                };
                context.ProductBillOfMaterials.Add(productBom);
            }

            // Add item to the BOM
            var item = new BillOfMaterialItem
            {
                MaterialId = material.Id,
                MaterialTypeId = materialType?.Id,
                Grade = GetCell("GRADE"),
                CasNumber = GetCell("CAS NUMBER"),
                Function = GetCell("FUNCTION"),
                Order = int.TryParse(GetCell("ORDER"), out var order) ? order : 0,
                IsSubstitutable = false,
                BaseQuantity = decimal.TryParse(GetCell("QUANTITY"), out var baseQuantity) ? baseQuantity : 0,
                BaseUoMId = uom?.Id,
            };

            billOfMaterial.Items.Add(item);
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> ImportProductPackagesFromExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return UploadErrors.EmptyFile;

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return UploadErrors.WorksheetNotFound;

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var header = worksheet.Cells[1, col].Text.Trim();
            if (!string.IsNullOrEmpty(header))
                headers[header] = col;
        }

        var requiredHeaders = new[]
        {
            "PRODUCT NAME", "PRODUCT CODE", "COMPONENT MATERIAL", "COMPONENT MATERIAL CODE",
            "BASE QUANTITY", "DIRECT LINK MATERIAL", "DIRECT LINK MATERIAL CODE",
            "UNIT CAPACITY", "PACKING EXCESS", "MATERIALS THICKNESS", "OTHER STANDARDS"
        };

        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        var packages = new List<ProductPackage>();

        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            string GetCell(string header) => worksheet.Cells[row, headers[header]].Text.Trim();

            var productCode = GetCell("PRODUCT CODE");
            var componentMaterialCode = GetCell("COMPONENT MATERIAL CODE");
            var directLinkMaterialCode = GetCell("DIRECT LINK MATERIAL CODE");

            var product = await context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Code == productCode);
            if (product == null) continue;

            var material = await context.Materials.FirstOrDefaultAsync(m => m.Code == componentMaterialCode);
            if (material == null) continue;

            var directLinkMaterial = await context.Materials.FirstOrDefaultAsync(m => m.Code == directLinkMaterialCode);

            var productPackage = new ProductPackage
            {
                ProductId = product.Id,
                MaterialId = material.Id,
                DirectLinkMaterialId = directLinkMaterial?.Id,
                BaseQuantity = decimal.TryParse(GetCell("BASE QUANTITY"), out var baseQty) ? baseQty : 0,
                UnitCapacity = decimal.TryParse(GetCell("UNIT CAPACITY"), out var unitCap) ? unitCap : 0,
                PackingExcessMargin = decimal.TryParse(GetCell("PACKING EXCESS"), out var excess) ? excess : 0,
                MaterialThickness = GetCell("MATERIALS THICKNESS"),
                OtherStandards = GetCell("OTHER STANDARDS"),
            };

            packages.Add(productPackage);
        }

        await context.ProductPackages.AddRangeAsync(packages);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}