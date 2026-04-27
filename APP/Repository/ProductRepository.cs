using System.Collections.Generic;
using System.Globalization;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Equipments;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
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
            return Error.Validation("Product.Name", "Product with same name already exists");

        if (context.Products.IgnoreQueryFilters().Any(p => p.Code == request.Code))
            return Error.Validation("Product.Code", "Product with code already exists");

        if (request.Price < 0)
            return Error.Validation("Product.Price", "Product Price must be greater than 0");

        var product = mapper.Map<Product>(request);
        product.CreatedById = userId;
        product.Prices.Add(new ProductPrices { Price = request.Price, Date = DateTime.UtcNow });
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        return product.Id;
    }

    public async Task<Result<ProductDto>> GetProduct(Guid productId)
    {
        var product = await context
            .Products.AsSplitQuery()
            .Include(p => p.BaseUoM)
            .Include(p => p.Equipment)
            .Include(p => p.BillOfMaterials)
                .ThenInclude(p => p.BillOfMaterial)
                    .ThenInclude(p => p.Items.OrderBy(i => i.Order))
            .Include(p => p.Category)
            .Include(p => p.FinishedProducts)
            .Include(p => p.Packages)
            .Include(p => p.Routes.OrderBy(r => r.Order))
                .ThenInclude(p => p.WorkCenters)
            .Include(p => p.Routes.OrderBy(r => r.Order))
                .ThenInclude(p => p.ResponsibleUsers)
            .Include(p => p.Routes.OrderBy(r => r.Order))
                .ThenInclude(p => p.ResponsibleRoles)
            .Include(p => p.Routes.OrderBy(r => r.Order))
                .ThenInclude(p => p.Resources)
            .Include(p => p.Packings)
                .ThenInclude(p => p.PackingLists.OrderBy(r => r.Order))
                    .ThenInclude(p => p.Uom)
            .Include(p => p.Packings)
                .ThenInclude(p => p.BasePackingUoM)
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.Id == productId);

        return product is null
            ? ProductErrors.NotFound(productId)
            : mapper.Map<ProductDto>(product);
    }

    public async Task<Result<Paginateable<IEnumerable<ProductListDto>>>> GetProducts(
        int page,
        int pageSize,
        string searchQuery,
        Guid? departmentId,
        Division? division,
        string category,
        bool? isVerified = null
    )
    {
        var query = context.Products.AsSplitQuery().AsQueryable();

        if (isVerified.HasValue)
        {
            query = query.Where(p => p.IsVerified == isVerified.Value);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, f => f.Name);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(p => p.DepartmentId == departmentId);
        }

        if (division.HasValue)
        {
            query = query.Where(p => p.Division == division);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category.Name == category);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProductListDto>
        );
    }

    public async Task<Result<List<ProductCategory>>> GetProductCategories()
    {
        return await context.ProductCategories.ToListAsync();
    }

    public async Task<Result> UpdateProduct(
        UpdateProductRequest request,
        Guid productId,
        Guid userId
    )
    {
        var existingProduct = await context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (existingProduct is null)
        {
            return ProductErrors.NotFound(productId);
        }

        if (existingProduct.Price != request.Price)
        {
            existingProduct.Prices.Add(
                new ProductPrices { Price = request.Price, Date = DateTime.UtcNow }
            );
        }

        mapper.Map(request, existingProduct);
        existingProduct.LastUpdatedById = userId;

        context.Products.Update(existingProduct);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateProductPackageDescription(
        UpdateProductPackageDescriptionRequest request,
        Guid productId,
        Guid userId
    )
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

    public async Task<Result<Guid>> CreateBillOfMaterials(
        CreateProductBillOfMaterialRequest request,
        Guid productId
    )
    {
        var bom = mapper.Map<ProductBillOfMaterial>(request);

        await context.ProductBillOfMaterials.AddAsync(bom);
        await context.SaveChangesAsync();
        return bom.Id;
    }

    public async Task<Result> UpdateBillOfMaterials(
        CreateProductBillOfMaterialRequest request,
        Guid bomId
    )
    {
        var existingBom = await context.ProductBillOfMaterials.FirstOrDefaultAsync(p =>
            p.Id == bomId
        );

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
        var bom = await context
            .ProductBillOfMaterials.AsSplitQuery()
            .Include(b => b.BillOfMaterial)
                .ThenInclude(b => b.Items)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.MaterialCategory)
            .OrderByDescending(p => p.EffectiveDate)
            .FirstOrDefaultAsync(p => p.ProductId == productId && p.IsActive);

        return Result.Success(mapper.Map<ProductBillOfMaterialDto>(bom));
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

    public async Task<Result> CreateRoute(
        List<CreateRouteRequest> request,
        Guid productId,
        Guid userId
    )
    {
        var product = await context
            .Products.AsSplitQuery()
            .Include(product => product.Routes)
            .FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
            return ProductErrors.NotFound(productId);

        if (product.Routes.Count != 0)
        {
            await context.Routes.Where(x => x.ProductId == productId).ExecuteDeleteAsync();
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
        var route = await context
            .Routes.AsSplitQuery()
            .Include(r => r.Operation)
            .Include(r => r.WorkCenters)
                .ThenInclude(r => r.WorkCenter)
            .Include(r => r.ResponsibleUsers)
                .ThenInclude(r => r.User)
            .Include(r => r.ResponsibleUsers)
                .ThenInclude(r => r.ProductAnalyticalRawData)
                    .ThenInclude(r => r.Form)
            .Include(r => r.ResponsibleRoles)
                .ThenInclude(r => r.Role)
            .Include(r => r.ResponsibleRoles)
                .ThenInclude(r => r.ProductAnalyticalRawData)
                    .ThenInclude(r => r.Form)
            .Include(r => r.ResponsibleUsers)
                .ThenInclude(r => r.ProductAnalyticalRawData)
                    .ThenInclude(r => r.ProductStandardTestProcedure)
            .Include(r => r.ResponsibleRoles)
                .ThenInclude(r => r.ProductAnalyticalRawData)
                    .ThenInclude(r => r.ProductStandardTestProcedure)
            .Include(r => r.Resources)
                .ThenInclude(rr => rr.Resource)
            .FirstOrDefaultAsync(r => r.Id == routeId);

        if (route == null)
            return Error.NotFound("Route.NotFound", $"Route with ID {routeId} not found.");

        var routeDto = mapper.Map<RouteDto>(route);
        return Result.Success(routeDto);
    }

    public async Task<Result<IEnumerable<RouteDto>>> GetRoutes(Guid productId)
    {
        var query = await context
            .Routes.OrderBy(r => r.Order)
            .AsSplitQuery()
            .Include(r => r.Operation)
            .Include(r => r.WorkCenters)
                .ThenInclude(r => r.WorkCenter)
            .Include(r => r.ResponsibleUsers)
                .ThenInclude(r => r.User)
            .Include(r => r.ResponsibleRoles)
                .ThenInclude(r => r.Role)
            .Include(r => r.ResponsibleUsers)
                .ThenInclude(r => r.ProductAnalyticalRawData)
                    .ThenInclude(r => r.ProductStandardTestProcedure)
            .Include(r => r.ResponsibleRoles)
                .ThenInclude(r => r.ProductAnalyticalRawData)
                    .ThenInclude(r => r.ProductStandardTestProcedure)
            .Include(r => r.Resources)
                .ThenInclude(rr => rr.Resource)
            .Where(r => r.ProductId == productId)
            .ToListAsync();

        return mapper.Map<List<RouteDto>>(query);
    }

    public async Task<Result> UpdateRoute(UpdateRouteRequest request, Guid routeId, Guid userId)
    {
        var route = await context
            .Routes.AsSplitQuery()
            .Include(r => r.Resources)
            .Include(route => route.ResponsibleRoles)
            .Include(route => route.ResponsibleUsers)
            .Include(route => route.WorkCenters)
            .FirstOrDefaultAsync(r => r.Id == routeId);

        if (route == null)
            return Error.NotFound("Route.NotFound", $"Route with ID {routeId} not found.");

        await context.RouteResources.Where(x => x.RouteId == routeId).ExecuteDeleteAsync();
        await context.RouteResponsibleRoles.Where(x => x.RouteId == routeId).ExecuteDeleteAsync();
        await context.RouteResponsibleUsers.Where(x => x.RouteId == routeId).ExecuteDeleteAsync();
        await context.RouteWorkCenters.Where(x => x.RouteId == routeId).ExecuteDeleteAsync();

        route.Resources.Clear();
        route.ResponsibleRoles.Clear();
        route.ResponsibleUsers.Clear();
        route.WorkCenters.Clear();

        mapper.Map(request, route);
        route.LastUpdatedById = userId;

        context.Routes.Update(route);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteRoute(Guid routeId, Guid userId)
    {
        await context.Routes.Where(r => r.Id == routeId).ExecuteDeleteAsync();

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Guid>> CreateProductPackage(
        List<CreateProductPackageRequest> request,
        Guid productId,
        Guid userId
    )
    {
        var product = await context
            .Products.AsSplitQuery()
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
                if (
                    HasCircularDependency(
                        newPackage.MaterialId,
                        newPackage.DirectLinkMaterialId.Value,
                        product.Packages.ToList()
                    )
                )
                {
                    return Error.Failure(
                        "Product.Package",
                        $"Circular dependency detected with MaterialId {newPackage.MaterialId} and DirectLinkMaterialId {newPackage.DirectLinkMaterialId}"
                    );
                }
            }
        }

        // Remove old packages if they exist
        if (product.Packages.Count != 0)
        {
            await context.ProductPackages.Where(x => x.ProductId == productId).ExecuteDeleteAsync();
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

    private bool HasCircularDependency(
        Guid materialId,
        Guid directLinkMaterialId,
        List<ProductPackage> existingPackages
    )
    {
        var visited = new HashSet<Guid>(); // Track visited materials
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
            var nextPackage = existingPackages.FirstOrDefault(p =>
                p.MaterialId == currentMaterialId
            );
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
        var productPackage = await context
            .ProductPackages.AsSplitQuery()
            .Include(p => p.Product)
            .Include(p => p.Material)
            .Include(s => s.ProductPacking)
                .ThenInclude(p => p.PackingLists)
            .FirstOrDefaultAsync(p => p.ProductId == productPackageId);

        if (productPackage == null)
            return Error.NotFound(
                "ProductPackage.NotFound",
                $"Product package with ID {productPackageId} not found."
            );

        var productPackageDto = mapper.Map<ProductPackageDto>(productPackage);
        return Result.Success(productPackageDto);
    }

    public async Task<Result<IEnumerable<ProductPackageDto>>> GetProductPackages(Guid productId)
    {
        var query = await context
            .ProductPackages.AsSplitQuery()
            .Include(p => p.Material)
            .Include(s => s.ProductPacking)
                .ThenInclude(p => p.PackingLists)
            .Where(p => p.ProductId == productId)
            .ToListAsync();

        return mapper.Map<List<ProductPackageDto>>(query);
    }

    public async Task<Result> UpdateProductPackage(
        CreateProductPackageRequest request,
        Guid productPackageId,
        Guid userId
    )
    {
        var productPackage = await context
            .ProductPackages.AsSplitQuery()
            .Include(p => p.Product)
                .ThenInclude(p => p.Packages) // Include related packages for validation
            .FirstOrDefaultAsync(p => p.Id == productPackageId);

        if (productPackage == null)
            return Error.NotFound(
                "ProductPackage.NotFound",
                $"Product package with ID {productPackageId} not found."
            );

        // Prevent self-referencing update
        if (
            request.DirectLinkMaterialId.HasValue
            && request.DirectLinkMaterialId.Value == request.MaterialId
        )
        {
            return Error.Failure(
                "Product.Package",
                "DirectLinkMaterialId cannot be the same as MaterialId."
            );
        }

        // Check for circular dependency
        if (request.DirectLinkMaterialId.HasValue)
        {
            if (
                HasCircularDependency(
                    request.MaterialId,
                    request.DirectLinkMaterialId.Value,
                    productPackage.Product.Packages.ToList()
                )
            )
            {
                return Error.Failure(
                    "Product.Package",
                    $"Circular dependency detected with MaterialId {productPackage.MaterialId} and DirectLinkMaterialId {productPackage.DirectLinkMaterialId}"
                );
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
        var productPackage = await context.ProductPackages.FirstOrDefaultAsync(p =>
            p.Id == productPackageId
        );

        if (productPackage == null)
            return Error.NotFound(
                "ProductPackage.NotFound",
                $"Product package with ID {productPackageId} not found."
            );

        await context.ProductPackages.Where(x => x.Id == productPackageId).ExecuteDeleteAsync();

        context.ProductPackages.Update(productPackage);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Guid>> CreateProductPacking(
        List<CreateProductPacking> request,
        Guid productId,
        Guid userId
    )
    {
        var existingPackings = await context
            .ProductPackings.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(p => p.PackingLists)
            .Where(p => p.ProductId == productId)
            .ToListAsync();

        var updatedExistingIds = new HashSet<Guid>();

        foreach (var incoming in request)
        {
            var existing = existingPackings.FirstOrDefault(e =>
                (incoming.Id.HasValue && e.Id == incoming.Id.Value)
                || (!incoming.Id.HasValue && e.Name == incoming.Name)
            );

            if (existing != null)
            {
                // Update
                mapper.Map(incoming, existing);
                existing.PackingLists = incoming
                    .PackingLists.Select(mapper.Map<ProductPackingList>)
                    .ToList();
                updatedExistingIds.Add(existing.Id);
            }
            else
            {
                // Add
                var newPacking = mapper.Map<ProductPacking>(incoming);
                newPacking.ProductId = productId;
                newPacking.PackingLists = incoming
                    .PackingLists.Select(mapper.Map<ProductPackingList>)
                    .ToList();
                await context.ProductPackings.AddAsync(newPacking);
            }
        }

        var toDelete = existingPackings.Where(e => !updatedExistingIds.Contains(e.Id)).ToList();

        if (toDelete.Count != 0)
        {
            var toDeleteIds = toDelete.Select(d => d.Id).ToList();

            var actualUsedIds = await context
                .BatchPackagingRecords.Where(bpr =>
                    bpr.ProductPackingId.HasValue
                    && toDeleteIds.Contains(bpr.ProductPackingId.Value)
                )
                .Select(bpr => bpr.ProductPackingId.Value)
                .Distinct()
                .ToListAsync();

            if (actualUsedIds.Count != 0)
            {
                var usedNames = toDelete
                    .Where(d => actualUsedIds.Contains(d.Id))
                    .Select(d => d.Name)
                    .ToList();
                return Error.Failure(
                    "ProductPacking.Delete",
                    $"Cannot delete the following packing styles as they are already used in"
                        + $" batch packaging records: {string.Join(", ", usedNames)}"
                );
            }

            await context
                .ProductPackings.Where(p => toDeleteIds.Contains(p.Id))
                .ExecuteDeleteAsync();
        }

        await context.SaveChangesAsync();
        return productId;
    }

    public async Task<Result<IEnumerable<ProductPackingDto>>> GetProductPackings(Guid productId)
    {
        var query = await context
            .ProductPackings.AsSplitQuery()
            .Include(p => p.PackingLists.OrderBy(pp => pp.Order))
                .ThenInclude(p => p.Uom)
            .Include(p => p.BasePackingUoM)
            .Where(p => p.ProductId == productId)
            .ToListAsync();

        return mapper.Map<List<ProductPackingDto>>(query);
    }

    public async Task<Result<Guid>> CreateFinishedProduct(
        List<CreateFinishedProductRequest> request,
        Guid productId,
        Guid userId
    )
    {
        var product = await context
            .Products.AsSplitQuery()
            .Include(product => product.FinishedProducts)
            .FirstOrDefaultAsync(p => p.Id == productId);
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
        var product = await context
            .Products.AsSplitQuery()
            .Include(product => product.BillOfMaterials)
            .FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
            return ProductErrors.NotFound(productId);

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
        var equipment = await context
            .Equipments.AsSplitQuery()
            .Include(e => e.UoM)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == equipmentId);

        return equipment is null
            ? Error.NotFound("Equipment.NotFound", "Equipment with this Id not found")
            : mapper.Map<EquipmentDto>(equipment);
    }

    // Get paginated list of Equipments
    public async Task<Result<Paginateable<IEnumerable<EquipmentDto>>>> GetEquipments(
        int page,
        int pageSize,
        string searchQuery
    )
    {
        var query = context
            .Equipments.AsSplitQuery()
            .Include(e => e.UoM)
            .Include(e => e.Department)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, e => e.Name, e => e.EquipmentNumber);
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
        return mapper.Map<List<EquipmentDto>>(
            await context
                .Equipments.AsSplitQuery()
                .Include(e => e.UoM)
                .Include(e => e.Department)
                .ToListAsync()
        );
    }

    // Update Equipment
    public async Task<Result> UpdateEquipment(
        CreateEquipmentRequest request,
        Guid equipmentId,
        Guid userId
    )
    {
        var existingEquipment = await context.Equipments.FirstOrDefaultAsync(e =>
            e.Id == equipmentId
        );
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
            "PRODUCT NAME",
            "PRODUCT CODE",
            "CATEGORY",
            "BASE UOM",
            "COMPOSITION UNIT QTY",
            "EQUIPMENT",
            "FULL BATCH SIZE",
            "DEPARTMENT CODE",
            "LABEL CLAIMS",
        };

        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        var categories = await context
            .ProductCategories.AsNoTracking()
            .ToDictionaryAsync(c => c.Name.ToLower(), c => c.Id);

        var uoms = await context
            .UnitOfMeasures.AsNoTracking()
            .ToDictionaryAsync(u => u.Symbol.ToLower(), u => u.Id);

        var equipments = await context
            .Equipments.AsNoTracking()
            .ToDictionaryAsync(e => e.Name.ToLower(), e => e.Id);

        var departments = await context
            .Departments.AsNoTracking()
            .ToDictionaryAsync(d => d.Code, d => d.Id);

        var existingCodes = await context
            .Products.IgnoreQueryFilters()
            .Select(p => p.Code)
            .ToHashSetAsync();

        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            string GetCell(string header) => worksheet.Cells[row, headers[header]].Text.Trim();

            var productCode = GetCell("PRODUCT CODE");
            if (string.IsNullOrWhiteSpace(productCode) || existingCodes.Contains(productCode))
                continue;

            var categoryName = GetCell("CATEGORY").ToLower();
            var baseUomName = GetCell("BASE UOM").ToLower();
            var equipmentName = GetCell("EQUIPMENT").ToLower();
            var departmentCode = GetCell("DEPARTMENT CODE");

            var product = new Product
            {
                Name = GetCell("PRODUCT NAME"),
                Code = productCode,
                GenericName = GetCell("GENERIC NAME"),
                StorageCondition = GetCell("STORAGE CONDITION"),
                PackageStyle = GetCell("PACK STYLE"),
                FilledWeight = GetCell("FILLED WEIGHT/VOLUME"),
                ShelfLife = GetCell("SHELF LIFE"),
                ActionUse = GetCell("ACTION AND USE"),
                FdaRegistrationNumber = GetCell("FDA REGISTRATION NUMBER"),
                MasterFormulaNumber = GetCell("MASTER FORMULA NUMBER"),
                PrimaryPackDescription = "",
                SecondaryPackDescription = "",
                TertiaryPackDescription = "",
                CategoryId = categories.TryGetValue(categoryName, out var categoryId)
                    ? categoryId
                    : null,
                BaseUomId = uoms.TryGetValue(baseUomName, out var baseUom) ? baseUom : null,
                EquipmentId = equipments.TryGetValue(equipmentName, out var equipmentId)
                    ? equipmentId
                    : null,
                DepartmentId = departments.TryGetValue(departmentCode, out var departmentId)
                    ? departmentId
                    : null,
                BaseQuantity = decimal.TryParse(GetCell("COMPOSITION UNIT QTY"), out var bq)
                    ? bq
                    : 0,
                FullBatchSize = decimal.TryParse(GetCell("FULL BATCH SIZE"), out var fbs) ? fbs : 0,
                LabelClaim = GetCell("LABEL CLAIMS"),
            };

            products.Add(product);
            existingCodes.Add(productCode);
        }

        if (products.Count != 0)
        {
            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }

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
            "PRODUCT NAME",
            "PRODUCT CODE",
            "ORDER",
            "MATERIAL TYPE",
            "COMPONENT MATERIAL",
            "COMPONENT MATERIAL CODE",
            "QUANTITY",
            "UOM",
            "GRADE",
            "CAS NUMBER",
            "FUNCTION",
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

            var product = await context
                .Products.AsNoTracking()
                .IgnoreAutoIncludes()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Code == productCode);
            if (product == null)
                continue;

            var material = context
                .Materials.AsNoTracking()
                .IgnoreAutoIncludes()
                .FirstOrDefault(m => m.Code == materialCode);
            if (material == null)
                continue;

            var uom = await context.UnitOfMeasures.FirstOrDefaultAsync(u =>
                u.Name.ToLower() == uomName.ToLower()
            );
            var materialType = await context
                .MaterialTypes.AsNoTracking()
                .IgnoreAutoIncludes()
                .FirstOrDefaultAsync(mt => mt.Name.ToLower() == materialTypeName.ToLower());

            if (!bomMap.TryGetValue(productCode, out var billOfMaterial))
            {
                billOfMaterial = new BillOfMaterial
                {
                    ProductId = product.Id,
                    Version = 1, // or dynamic version logic
                    IsActive = true,
                    Items = [],
                };
                context.BillOfMaterials.Add(billOfMaterial);
                bomMap[productCode] = billOfMaterial;

                // Create a ProductBillOfMaterial entry
                var productBom = new ProductBillOfMaterial
                {
                    ProductId = product.Id,
                    BillOfMaterial = billOfMaterial,
                    Quantity = decimal.TryParse(GetCell("QUANTITY"), out var quantity)
                        ? quantity
                        : 0,
                    Version = 1,
                    EffectiveDate = DateTime.UtcNow,
                    IsActive = true,
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
                Order = int.TryParse(GetCell("ORDER"), out var order) ? order : 0,
                IsSubstitutable = false,
                BaseQuantity = decimal.TryParse(GetCell("QUANTITY"), out var baseQuantity)
                    ? baseQuantity
                    : 0,
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
            "PRODUCT NAME",
            "PRODUCT CODE",
            "COMPONENT MATERIAL",
            "COMPONENT MATERIAL CODE",
            "BASE QUANTITY",
            "DIRECT LINK MATERIAL",
            "DIRECT LINK MATERIAL CODE",
            "UNIT CAPACITY",
            "PACKING EXCESS",
            "MATERIALS THICKNESS",
            "OTHER STANDARDS",
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

            var product = await context
                .Products.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Code == productCode);
            if (product == null)
                continue;

            var material = await context.Materials.FirstOrDefaultAsync(m =>
                m.Code == componentMaterialCode
            );
            if (material == null)
                continue;

            var directLinkMaterial = await context.Materials.FirstOrDefaultAsync(m =>
                m.Code == directLinkMaterialCode
            );

            var productPackage = new ProductPackage
            {
                ProductId = product.Id,
                MaterialId = material.Id,
                DirectLinkMaterialId = directLinkMaterial?.Id,
                BaseQuantity = decimal.TryParse(GetCell("BASE QUANTITY"), out var baseQty)
                    ? baseQty
                    : 0,
                UnitCapacity = decimal.TryParse(GetCell("UNIT CAPACITY"), out var unitCap)
                    ? unitCap
                    : 0,
                PackingExcessMargin = decimal.TryParse(GetCell("PACKING EXCESS"), out var excess)
                    ? excess
                    : 0,
                MaterialThickness = GetCell("MATERIALS THICKNESS"),
                OtherStandards = GetCell("OTHER STANDARDS"),
            };

            packages.Add(productPackage);
        }

        await context.ProductPackages.AddRangeAsync(packages);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ImportProductStockFromExcel(IFormFile file)
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
            "Warehouse",
            "Product Code",
            "Product Name",
            "Packing Style",
            "Total Quantity",
            "UOM",
            "Batch No.",
            "FGTN ID",
            "AR No.",
            "Manufacturing Date",
            "Expiry Date",
        };

        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        // 1. SCAN EXCEL FOR FILTER CRITERIA
        var excelProductCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excelPackingStyles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uomSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            string GetRaw(string h) =>
                headers.TryGetValue(h, out var col) ? worksheet.Cells[row, col].Text.Trim() : null;
            var pCode = GetRaw("Product Code");
            var pStyle = GetRaw("Packing Style");
            var uom = GetRaw("UOM");

            if (!string.IsNullOrEmpty(pCode))
                excelProductCodes.Add(pCode);
            if (!string.IsNullOrEmpty(pStyle))
                excelPackingStyles.Add(pStyle);
            if (!string.IsNullOrEmpty(uom))
                uomSymbols.Add(uom);
        }

        // 2. FETCH DEFAULTS AND LOOKUPS
        // Fetch Product Packing with Product Hierarchy
        var packingData = await context
            .ProductPackings.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(p => p.Product)
            .Where(pp =>
                excelPackingStyles.Contains(pp.Name) || excelProductCodes.Contains(pp.Product.Code)
            )
            .ToListAsync();

        // Create a composite lookup: "ProductCode|PackingName"
        var packingLookup = packingData.ToDictionary(
            pp => $"{pp.Product.Code.Trim()}|{pp.Name.Trim()}",
            pp => pp,
            StringComparer.OrdinalIgnoreCase
        );

        // Fetch Production Schedule Products for matching
        var pspData = await context
            .ProductionScheduleProducts.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(psp => psp.Product)
            .Include(psp => psp.ProductPacking)
            .Where(psp => excelProductCodes.Contains(psp.Product.Code))
            .ToListAsync();

        var pspIds = pspData.Select(p => p.Id).ToList();

        // Fetch Production Activities and their steps for these PSPs
        var activityData = await context
            .ProductionActivities.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(pa => pa.Steps)
            .Where(pa => pspIds.Contains(pa.ProductionScheduleProductId))
            .ToListAsync();

        var activityLookup = activityData
            .GroupBy(pa => pa.ProductionScheduleProductId)
            .ToDictionary(g => g.Key, g => g.First());

        var pspLookup = pspData
            .GroupBy(psp => $"{psp.Product.Code.Trim()}|{(psp.ProductPacking?.Name ?? "").Trim()}")
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var uoms = await context
            .UnitOfMeasures.Where(u => uomSymbols.Contains(u.Symbol))
            .ToListAsync();

        var uomLookUp = uoms.ToDictionary(u => u.Symbol, u => u, StringComparer.Ordinal);

        var warehouses = await context
            .Warehouses.Where(w => w.Type == WarehouseType.FinishedGoodsStorage)
            .ToDictionaryAsync(w => w.Name.ToLower(), w => w);

        var manufacturingRecords = new List<BatchManufacturingRecord>();
        var packagingRecords = new List<BatchPackagingRecord>();
        var finishedGoodsTransferNotes = new List<FinishedGoodsTransferNote>();

        // 3. PROCESS ROWS
        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            string GetCell(string h) =>
                headers.TryGetValue(h, out var col) ? worksheet.Cells[row, col].Text.Trim() : null;

            var productCode = GetCell("Product Code").Trim();
            var packingStyle = GetCell("Packing Style").Trim();
            var batchNo = GetCell("Batch No.").Trim();
            var uomSymbol = GetCell("UOM").Trim();

            if (string.IsNullOrEmpty(productCode) || string.IsNullOrEmpty(batchNo))
                continue;

            // Resolve Packing & Product
            var packingKey = $"{productCode}|{packingStyle}";
            if (!packingLookup.TryGetValue(packingKey, out var packing))
            {
                return Error.NotFound(
                    "ProductPacking",
                    $"Row {row}: Packing style '{packingStyle}' for Product '{productCode}' not found."
                );
            }

            // Resolve Production Schedule Product
            if (!pspLookup.TryGetValue(packingKey, out var psp))
            {
                return Error.NotFound(
                    "ProductionScheduleProduct",
                    $"Row {row}: Production schedule for Product '{productCode}' and Packing '{packingStyle}' not found."
                );
            }

            // Resolve Activity Step from PSP
            if (!activityLookup.TryGetValue(psp.Id, out var activity))
            {
                return Error.Validation(
                    "ProductionActivity",
                    $"Row {row}: No production activity found for Product '{productCode}' and Packing '{packingStyle}'."
                );
            }

            var activityStep = activity.Steps.OrderBy(s => s.Order).LastOrDefault();
            if (activityStep == null)
            {
                return Error.Validation(
                    "ProductionActivityStep",
                    $"Row {row}: No production activity steps found for Product '{productCode}' and Packing '{packingStyle}'."
                );
            }

            // Parse shared data
            decimal.TryParse(GetCell("Total Quantity"), out var quantity);
            var mfgDate = GetCell("Manufacturing Date");
            var expiryDate = GetCell("Expiry Date");

            // 4. Create Manufacturing Record
            var bmr = new BatchManufacturingRecord
            {
                Id = Guid.NewGuid(),
                ProductionScheduleProductId = psp.Id,
                ProductionActivityStepId = activityStep.Id,
                BatchNumber = batchNo,
                ManufacturingDate = ParseDate(mfgDate),
                ExpiryDate = ParseDate(expiryDate),
                BatchQuantity = quantity,
                Status = BatchManufacturingStatus.Approved, // Set appropriate default status
                IssuedDate = DateTime.UtcNow,
            };
            manufacturingRecords.Add(bmr);

            // 5. Create Packaging Record
            packagingRecords.Add(
                new BatchPackagingRecord
                {
                    Id = Guid.NewGuid(),
                    ProductionScheduleProductId = psp.Id,
                    ProductionActivityStepId = activityStep.Id,
                    ProductPackingId = packing.Id,
                    BatchNumber = batchNo,
                    ManufacturingDate = ParseDate(mfgDate),
                    ExpiryDate = ParseDate(expiryDate),
                    BatchQuantity = quantity,
                    IssuedDate = DateTime.UtcNow,
                }
            );

            // 6. Create Finished Goods Transfer Note
            finishedGoodsTransferNotes.Add(
                new FinishedGoodsTransferNote
                {
                    Id = Guid.NewGuid(),
                    TransferNoteNumber = GetCell("FGTN ID"),
                    ToWarehouseId = warehouses.TryGetValue(
                        GetCell("Warehouse").ToLower(),
                        out var warehouse
                    )
                        ? warehouse.Id
                        : null,
                    TotalQuantity = quantity,
                    QuantityReceived = quantity,
                    ProductPackingId = packing.Id,
                    BatchManufacturingRecordId = bmr.Id,
                    Approved = true,
                    UoMId = uomLookUp.TryGetValue(uomSymbol, out var uom) ? uom.Id : null,
                }
            );
        }

        // 6. SAVE EVERYTHING
        await context.BatchManufacturingRecords.AddRangeAsync(manufacturingRecords);
        await context.BatchPackagingRecords.AddRangeAsync(packagingRecords);
        await context.FinishedGoodsTransferNotes.AddRangeAsync(finishedGoodsTransferNotes);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<byte[]>> ExportProductStockToExcel(
        Guid userId,
        Guid? departmentId,
        Division? departmentDivision
    )
    {
        var user = await context
            .Users.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return UserErrors.NotFound(userId);

        if (user.Department == null)
        {
            return Error.Failure("User.NoDepartment", "User does not belong to any department.");
        }

        var division = departmentDivision ?? user.Department.Division;

        var fgtnList = await context
            .FinishedGoodsTransferNotes.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(f => f.ToWarehouse)
            .Include(f => f.ProductPacking)
                .ThenInclude(pp => pp.Product)
            .Include(f => f.BatchManufacturingRecord)
            .Where(f => f.IsApproved && f.TotalQuantity > 0 && !f.DeletedAt.HasValue)
            .Where(f => f.ToWarehouse.Division == division)
            .Where(f => f.ProductPacking.Product.Division == division)
            .Where(f => f.ToWarehouse.Type == WarehouseType.FinishedGoodsStorage)
            .Where(f =>
                !departmentId.HasValue || f.ProductPacking.Product.DepartmentId == departmentId
            )
            .ToListAsync();

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Product Stock");

        // Headers matching requiredHeaders in Import
        string[] headers =
        {
            "Warehouse",
            "Product Code",
            "Product Name",
            "Packing Style",
            "Total Quantity",
            "Batch No.",
            "FGTN ID",
            "AR No.",
            "Manufacturing Date",
            "Expiry Date",
        };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var fgtn in fgtnList)
        {
            worksheet.Cells[row, 1].Value = fgtn.ToWarehouse?.Name;
            worksheet.Cells[row, 2].Value = fgtn.ProductPacking?.Product?.Code;
            worksheet.Cells[row, 3].Value = fgtn.ProductPacking?.Product?.Name;
            worksheet.Cells[row, 4].Value = fgtn.ProductPacking?.Name;
            //worksheet.Cells[row, 5].Value = fgtn.TotalQuantity;
            // worksheet.Cells[row, 6].Value = fgtn.BatchManufacturingRecord?.BatchNumber;
            // worksheet.Cells[row, 7].Value = fgtn.TransferNoteNumber;
            // worksheet.Cells[row, 8].Value = fgtn.QarNumber;
            // worksheet.Cells[row, 9].Value =
            //     fgtn.BatchManufacturingRecord?.ManufacturingDate?.ToString("yyyy-MM-dd");
            // worksheet.Cells[row, 10].Value = fgtn.BatchManufacturingRecord?.ExpiryDate?.ToString(
            //     "yyyy-MM-dd"
            // );
            row++;
        }

        worksheet.Cells.AutoFitColumns();

        return Result.Success(await package.GetAsByteArrayAsync());
    }

    public async Task<Result<byte[]>> ExportProductsToExcel(
        Guid userId,
        Guid? departmentId,
        Division? departmentDivision
    )
    {
        var user = await context
            .Users.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);
        if (user == null)
            return UserErrors.NotFound(userId);

        if (user.Department == null)
        {
            return Error.Failure("User.NoDepartment", "User does not belong to any department.");
        }

        var division = departmentDivision ?? user.Department.Division;

        var warehouseName =
            division == Division.BetaLactam ? "Beta Warehouse" : "Non-Beta Warehouse";

        var products = await context
            .Products.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(p => p.Packings)
            .Where(p => !p.DeletedAt.HasValue)
            .Where(p => p.Division == division)
            .Where(p => !departmentId.HasValue || p.DepartmentId == departmentId)
            .Where(p => !p.DeletedAt.HasValue)
            .ToListAsync();

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Products");

        // Headers matching requiredHeaders in Import
        string[] headers =
        {
            "Warehouse",
            "Product Code",
            "Product Name",
            "Packing Style",
            "Total Quantity",
            "UOM",
            "Batch No.",
            "FGTN ID",
            "AR No.",
            "Manufacturing Date",
            "Expiry Date",
        };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var product in products)
        {
            if (product.Packings.Any())
            {
                foreach (var packing in product.Packings)
                {
                    worksheet.Cells[row, 1].Value = warehouseName;
                    worksheet.Cells[row, 2].Value = product.Code;
                    worksheet.Cells[row, 3].Value = product.Name;
                    worksheet.Cells[row, 4].Value = packing.Name;
                    row++;
                }
            }
            else
            {
                worksheet.Cells[row, 1].Value = warehouseName;
                worksheet.Cells[row, 2].Value = product.Code;
                worksheet.Cells[row, 3].Value = product.Name;
                row++;
            }
        }

        worksheet.Cells.AutoFitColumns();

        return Result.Success(await package.GetAsByteArrayAsync());
    }

    public async Task<Result> ImportEquipmentFromExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return UploadErrors.EmptyFile;

        var equipmentsToInsert = new List<Equipment>();

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

        // Mapping headers based on your requirements
        var requiredHeaders = new[]
        {
            "EQUIPMENT NO",
            "EQUIPMENT NAME",
            "UOM",
            "DEPARTMENT CODE",
            "Storage Location",
        };

        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        // Lookups for Foreign Keys
        var uoms = await context
            .UnitOfMeasures.AsNoTracking()
            .ToDictionaryAsync(u => u.Symbol, u => u.Id);

        var departments = await context
            .Departments.AsNoTracking()
            .ToDictionaryAsync(d => d.Code.ToLower(), d => d.Id);

        var existingNumbers = await context
            .Equipments.IgnoreQueryFilters()
            .Select(e => e.EquipmentNumber)
            .ToHashSetAsync();

        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            string GetCell(string header) =>
                headers.TryGetValue(header, out var header1)
                    ? worksheet.Cells[row, header1].Text.Trim()
                    : string.Empty;

            var equipmentNo = GetCell("EQUIPMENT NO");
            if (
                equipmentNo != "-"
                && existingNumbers.Contains(equipmentNo)
                && !string.IsNullOrWhiteSpace(equipmentNo)
            )
                return Error.Validation(
                    "EquipmentNo",
                    $"Equipment number {equipmentNo} already exists. See row {row}"
                );

            var uomSymbol = GetCell("UOM").ToLower();
            var deptName = GetCell("DEPARTMENT CODE").ToLower();

            // Business logic for boolean "Storage" check
            var isStorageStr = GetCell("STORAGE").ToLower();
            bool isStorage = isStorageStr is "yes" or "true" or "1";

            // Business logic for Relevance Check
            var relCheckStr = GetCell("RELEVANT FOR CAPACITY PLANNING").ToLower();
            bool relevanceCheck = relCheckStr is "yes" or "true" or "1";

            var equipment = new Equipment
            {
                EquipmentNumber = equipmentNo,
                Name = GetCell("EQUIPMENT NAME"),
                Model = GetCell("MODEL"),
                SerialNumber = GetCell("SERIAL NO"),
                Location = GetCell("Storage Location"),
                IsStorage = isStorage,
                RelevanceCheck = relevanceCheck,
                CapacityQuantity = decimal.TryParse(GetCell("CAPACITY QUANTITY"), out var cq)
                    ? cq
                    : 0,
                UoMId = uoms.TryGetValue(uomSymbol, out var uomId) ? uomId : null,
                DepartmentId = departments.TryGetValue(deptName, out var deptId)
                    ? deptId
                    : Guid.Empty,
            };

            // Basic Validation: Ensure Guid IDs are found before adding
            if (equipment.DepartmentId == Guid.Empty)
            {
                return Error.Validation(
                    "MissingValue",
                    $"Missing value for department at row {row}"
                );
            }
            equipmentsToInsert.Add(equipment);
            existingNumbers.Add(equipmentNo);
        }

        if (equipmentsToInsert.Count != 0)
        {
            await context.Equipments.AddRangeAsync(equipmentsToInsert);
            await context.SaveChangesAsync();
        }

        return Result.Success();
    }

    DateTime? ParseDate(string input)
    {
        if (
            DateTime.TryParseExact(
                input,
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var d
            )
        )
        {
            // Specify that this date is UTC to prevent local time offsets
            return DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }
        return null;
    }
}
