using System.Globalization;
using System.Linq.Expressions;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Reports.Warehouse;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SHARED;
using SHARED.Requests;

namespace APP.Repository;

public class MaterialRepository(ApplicationDbContext context, IMapper mapper) : IMaterialRepository
{
    // ************* CRUD for Materials *************
    // Create Material
    public async Task<Result<Guid>> CreateMaterial(CreateMaterialRequest request, Guid userId)
    {
        var material = mapper.Map<Material>(request);
        material.CreatedById = userId;
        await context.Materials.AddAsync(material);
        await context.SaveChangesAsync();

        return material.Id;
    }

    // Get Material by ID
    public async Task<Result<MaterialDto>> GetMaterial(Guid materialId)
    {
        var material = await context
            .Materials.AsSplitQuery()
            .Include(m => m.MaterialCategory) // Include category if needed
            .FirstOrDefaultAsync(m => m.Id == materialId);

        return material is null
            ? MaterialErrors.NotFound(materialId)
            : mapper.Map<MaterialDto>(material);
    }

    // Get a paginated list of Materials
    public async Task<Result<Paginateable<IEnumerable<MaterialDto>>>> GetMaterials(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind kind
    )
    {
        var query = context
            .Materials.AsSplitQuery()
            .Include(m => m.MaterialCategory)
            .Where(m => m.Kind == kind)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Name, m => m.Description, m => m.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialDto>
        );
    }

    public async Task<Result<Paginateable<IEnumerable<MaterialDto>>>> GetMaterialsNotLinkedToArd(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind kind
    )
    {
        var query = context
            .Materials.Where(m => m.Kind == kind)
            .Where(m =>
                !context.MaterialAnalyticalRawData.Any(ard =>
                    ard.MaterialStandardTestProcedure.MaterialId == m.Id
                )
            )
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Name, m => m.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialDto>
        );
    }

    public async Task<Result<List<MaterialCategoryDto>>> GetMaterialCategories(
        MaterialKind? materialKind
    )
    {
        var materialCategories =
            materialKind != null
                ? await context
                    .MaterialCategories.Where(m => m.MaterialKind == materialKind)
                    .ToListAsync()
                : await context.MaterialCategories.ToListAsync();
        return mapper.Map<List<MaterialCategoryDto>>(materialCategories);
    }

    public async Task<Result<List<MaterialDto>>> GetMaterials()
    {
        return mapper.Map<List<MaterialDto>>(
            await context.Materials.AsSplitQuery().Include(m => m.MaterialCategory).ToListAsync()
        );
    }

    // Update Material
    public async Task<Result> UpdateMaterial(
        CreateMaterialRequest request,
        Guid materialId,
        Guid userId
    )
    {
        var existingMaterial = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
        if (existingMaterial is null)
        {
            return MaterialErrors.NotFound(materialId);
        }

        mapper.Map(request, existingMaterial);
        existingMaterial.LastUpdatedById = userId;

        context.Materials.Update(existingMaterial);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateReOrderLevel(Guid materialId, int reOrderLevel, Guid userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var material = await context.MaterialDepartments.FirstOrDefaultAsync(m =>
            m.MaterialId == materialId && m.DepartmentId == user.DepartmentId
        );
        if (material is null)
        {
            return MaterialErrors.NotFound(materialId);
        }

        material.ReOrderLevel = reOrderLevel;
        material.LastUpdatedById = userId;

        context.MaterialDepartments.Update(material);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete Material (soft delete)
    public async Task<Result> DeleteMaterial(Guid materialId, Guid userId)
    {
        var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
        if (material is null)
        {
            return MaterialErrors.NotFound(materialId);
        }

        material.DeletedAt = DateTime.UtcNow;
        material.LastDeletedById = userId;

        context.Materials.Update(material);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* CRUD for Material Batches *************

    // Create Material Batch
    public async Task<Result> CreateMaterialBatch(
        List<CreateMaterialBatchRequest> request,
        Guid userId
    )
    {
        var batches = mapper.Map<List<MaterialBatch>>(request);

        foreach (var batch in batches)
        {
            batch.CreatedById = userId;
        }

        // Add batches to the database
        await context.MaterialBatches.AddRangeAsync(batches);
        await context.SaveChangesAsync();

        // Now create initial movements for each batch
        foreach (var batch in batches)
        {
            var initialLocationId = request
                .FirstOrDefault(r => r.MaterialId == batch.MaterialId)
                ?.MaterialId;

            if (initialLocationId.HasValue)
            {
                var movement = new MassMaterialBatchMovement
                {
                    BatchId = batch.Id,
                    ToWarehouseId = initialLocationId.Value,
                    Quantity = batch.TotalQuantity, // All material moved to the initial location
                    MovedAt = DateTime.UtcNow,
                    MovedById = userId,
                    MovementType = MovementType.ToWarehouse,
                };

                // Add the movement entry to the context
                await context.MassMaterialBatchMovements.AddAsync(movement);
            }
        }

        // Save changes to the database
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> CreateMaterialBatchWithoutBatchMovement(
        List<CreateMaterialBatchRequest> request,
        Guid userId
    )
    {
        if (request.Count == 0)
            return Error.Validation("Material.Batches", "Must have at least one batch.");

        var providedBatchNumbers = request
            .Where(r => !string.IsNullOrEmpty(r.BatchNumber))
            .Select(r => r.BatchNumber.Trim())
            .ToList();

        if (providedBatchNumbers.Count != 0)
        {
            // Check for duplicates within the request itself
            var duplicateInRequest = providedBatchNumbers
                .GroupBy(bn => bn, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();

            if (duplicateInRequest != null)
            {
                return Error.Validation(
                    "BatchNumber",
                    $"Duplicate batch number '{duplicateInRequest}' found in request."
                );
            }

            // Check for duplicates already in the database
            var existingBatchNumbers = await context
                .MaterialBatches.Where(mb => providedBatchNumbers.Contains(mb.BatchNumber))
                .Select(mb => mb.BatchNumber)
                .ToListAsync();

            if (existingBatchNumbers.Count != 0)
            {
                return Error.Validation(
                    "BatchNumber",
                    $"Batch number(s) '{string.Join(", ", existingBatchNumbers)}' already exist."
                );
            }
        }

        var batches = mapper.Map<List<MaterialBatch>>(request);

        await context.MaterialBatches.AddRangeAsync(batches);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    // Get Material Batch by ID
    public async Task<Result<MaterialBatchDto>> GetMaterialBatch(Guid batchId)
    {
        var batch = await context
            .MaterialBatches.Include(b => b.Material)
            .Include(b => b.Events)
                .ThenInclude(m => m.User)
            .Include(b => b.Events)
                .ThenInclude(m => m.ConsumptionWarehouse)
            .Include(b => b.MassMovements)
                .ThenInclude(m => m.FromWarehouse)
            .Include(b => b.MassMovements)
                .ThenInclude(m => m.ToWarehouse)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch is null)
            return MaterialErrors.NotFound(batchId);

        var batchDto = mapper.Map<MaterialBatchDto>(batch);
        batchDto.Locations = GetCurrentLocations(batchDto);
        return batchDto;
    }

    // Get paginated list of Material Batches
    public async Task<Result<Paginateable<IEnumerable<MaterialBatchDto>>>> GetMaterialBatches(
        int page,
        int pageSize,
        string searchQuery
    )
    {
        var query = context
            .MaterialBatches.AsSplitQuery()
            .Include(b => b.Material)
            .Include(b => b.Events)
                .ThenInclude(m => m.User)
            .Include(b => b.Events)
                .ThenInclude(m => m.ConsumptionWarehouse)
            .Include(b => b.MassMovements)
                .ThenInclude(m => m.FromWarehouse)
            .Include(b => b.MassMovements)
                .ThenInclude(m => m.ToWarehouse)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, b => b.Material.Name);
        }

        var result = await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialBatchDto>
        );

        var batches = result.Data.ToList();
        foreach (var batch in batches)
        {
            batch.Locations = GetCurrentLocations(batch);
        }
        result.Data = batches;
        return result;
    }

    public async Task<Result<List<MaterialBatchDto>>> GetMaterialBatchesByMaterialId(
        Guid materialId
    )
    {
        var query = await context
            .MaterialBatches.AsSplitQuery()
            .Include(b => b.Material)
            .Include(b => b.Events)
                .ThenInclude(m => m.User)
            .Include(b => b.Events)
                .ThenInclude(m => m.ConsumptionWarehouse)
            .Include(b => b.MassMovements)
                .ThenInclude(m => m.FromWarehouse)
            .Include(b => b.MassMovements)
                .ThenInclude(m => m.ToWarehouse)
            .Where(b => b.MaterialId == materialId)
            .ToListAsync();

        var batches = mapper.Map<List<MaterialBatchDto>>(query);

        foreach (var batch in batches)
        {
            batch.Locations = GetCurrentLocations(batch);
        }

        return batches;
    }

    public async Task<Result<Paginateable<IEnumerable<MaterialDetailsDto>>>> GetApprovedMaterials(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind kind,
        Guid userId
    )
    {
        // 1. Fetch User and Department Info once
        var user = await context
            .Users.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.DepartmentId == null)
            return UserErrors.NotFound(userId);

        var warehouse =
            kind == MaterialKind.Raw
                ? user.GetUserRawWarehouse()
                : user.GetUserPackagingWarehouse();
        if (warehouse is null)
            return UserErrors.WarehouseNotFound(kind);

        // 2. Build the base query for Materials
        var query = context
            .ShelfMaterialBatches.IgnoreQueryFilters()
            .AsSplitQuery()
            .OrderBy(m => m.MaterialBatch.Material.Name)
            .Where(m =>
                m.MaterialBatch.Material.Kind == kind
                && m.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId
                    == warehouse.Id
                && (
                    m.MaterialBatch.Status == BatchStatus.Available
                    || m.MaterialBatch.Status == BatchStatus.Frozen
                )
            )
            .Select(m => m.MaterialBatch.Material)
            .Distinct();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Name, m => m.Description, m => m.Code);
        }

        // 3. Paginate the IDs first to keep the memory footprint small
        var paginatedResult = await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            m => m
        );

        var materialIds = paginatedResult.Data.Select(m => m.Id).ToList();

        var stocks = await context
            .ShelfMaterialBatches.IgnoreQueryFilters()
            .AsSplitQuery()
            .Where(s =>
                materialIds.Contains(s.MaterialBatch.MaterialId)
                && s.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId
                    == warehouse.Id
            )
            .GroupBy(s => s.MaterialBatch.MaterialId)
            .Select(g => new { MaterialId = g.Key, Total = g.Sum(s => s.Quantity) })
            .ToDictionaryAsync(x => x.MaterialId, x => x.Total);

        var uoms = await context
            .MaterialDepartments.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(md => md.UoM)
            .Where(md =>
                materialIds.Contains(md.MaterialId) && md.DepartmentId == user.DepartmentId
            )
            .ToDictionaryAsync(md => md.MaterialId, md => mapper.Map<UnitOfMeasureDto>(md.UoM));

        var materialDetails = paginatedResult
            .Data.Select(m => new MaterialDetailsDto
            {
                Material = mapper.Map<MaterialDto>(m),
                UnitOfMeasure = uoms.GetValueOrDefault(m.Id),
                TotalAvailableQuantity = stocks.GetValueOrDefault(m.Id, 0),
            })
            .ToList();

        return Result.Success(
            new Paginateable<IEnumerable<MaterialDetailsDto>>
            {
                Data = materialDetails,
                PageIndex = paginatedResult.PageIndex,
                PageCount = paginatedResult.PageCount,
                TotalRecordCount = paginatedResult.TotalRecordCount,
                StartPageIndex = paginatedResult.StartPageIndex,
                NumberOfPagesToShow = paginatedResult.NumberOfPagesToShow,
                StopPageIndex = paginatedResult.StopPageIndex,
            }
        );
    }

    public async Task<
        Result<Paginateable<IEnumerable<MaterialDetailsDto>>>
    > GetApprovedMaterialsByDepartment(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind kind,
        Guid warehouseId,
        Guid departmentId
    )
    {
        var warehouse = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);

        if (warehouse is null)
            return Error.NotFound("Warehouse.NotFound", "Warehouse not found");

        var query = context
            .ShelfMaterialBatches.Where(m =>
                m.MaterialBatch.Material.Kind == kind
                && (
                    m.MaterialBatch.Status == BatchStatus.Available
                    || m.MaterialBatch.Status == BatchStatus.Frozen
                )
                && m.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId
                    == warehouse.Id
                && m.WarehouseLocationShelf
                    .WarehouseLocationRack
                    .WarehouseLocation
                    .Warehouse
                    .DepartmentId == departmentId
            )
            .Select(m => m.MaterialBatch.Material)
            .Distinct();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Name, m => m.Description);
        }

        var paginatedResult = await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialDto>
        );

        var materialDetails = new List<MaterialDetailsDto>();

        foreach (var m in paginatedResult.Data)
        {
            var totalAvailableQuantity = await GetMassMaterialStockInWarehouse(m.Id, warehouse.Id);
            if (totalAvailableQuantity.IsFailure)
                return totalAvailableQuantity.Errors;

            var unitOfMeasureDto = await context
                .MaterialDepartments.Where(md => md.DepartmentId == departmentId)
                .Select(md => mapper.Map<UnitOfMeasureDto>(md.UoM))
                .FirstOrDefaultAsync();

            if (unitOfMeasureDto == null)
                return Error.NotFound(
                    "MaterialDepartment.NotFound",
                    "Unit of measure not found for this department."
                );

            materialDetails.Add(
                new MaterialDetailsDto
                {
                    Material = m,
                    UnitOfMeasure = unitOfMeasureDto,
                    TotalAvailableQuantity = totalAvailableQuantity.Value,
                }
            );
        }

        var result = new Paginateable<IEnumerable<MaterialDetailsDto>>
        {
            Data = materialDetails,
            PageIndex = paginatedResult.PageIndex,
            PageCount = paginatedResult.PageCount,
            TotalRecordCount = paginatedResult.TotalRecordCount,
            StartPageIndex = paginatedResult.StartPageIndex,
            NumberOfPagesToShow = paginatedResult.NumberOfPagesToShow,
            StopPageIndex = paginatedResult.StopPageIndex,
        };

        return Result.Success(result);
    }

    public async Task<Result<List<MaterialDetailsDto>>> GetApprovedMaterialsByDepartmentV2(
        MaterialKind? kind,
        Guid? departmentId,
        Guid? materialCategoryId
    )
    {
        var query = context
            .ShelfMaterialBatches.IgnoreQueryFilters()
            .Where(m => !m.DeletedAt.HasValue)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            query = query.Where(s =>
                s.WarehouseLocationShelf
                    .WarehouseLocationRack
                    .WarehouseLocation
                    .Warehouse
                    .DepartmentId == departmentId
            );
        }

        if (kind.HasValue)
        {
            query = query.Where(s => s.MaterialBatch.Material.Kind == kind.Value);
        }

        if (materialCategoryId.HasValue)
        {
            query = query.Where(s =>
                s.MaterialBatch.Material.MaterialCategoryId == materialCategoryId.Value
            );
        }

        var raw = await query
            .GroupBy(s => new
            {
                s.MaterialBatch.Material.Id,
                s.MaterialBatch.Material.Name,
                s.MaterialBatch.Material.Code,
                s.MaterialBatch.Material.Kind,
                s.MaterialBatch.Material.MaterialCategoryId,
            })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Name,
                g.Key.Code,
                g.Key.Kind,
                g.Key.MaterialCategoryId,
                TotalAvailableQuantity = g.Sum(x => x.Quantity),
            })
            .ToListAsync();

        if (raw.Count == 0)
            return Result.Success(new List<MaterialDetailsDto>());

        var materialIds = raw.Select(r => r.Id).ToList();

        var uoms = await context
            .MaterialDepartments.AsNoTracking()
            .Include(md => md.UoM)
            .Where(md => md.DepartmentId == departmentId && materialIds.Contains(md.MaterialId))
            .ToDictionaryAsync(md => md.MaterialId, md => mapper.Map<UnitOfMeasureDto>(md.UoM));

        var result = raw.Select(r => new MaterialDetailsDto
            {
                Material = new MaterialDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Code = r.Code,
                    Kind = r.Kind,
                    MaterialCategory = r.MaterialCategoryId.HasValue
                        ? new MaterialCategoryDto { Id = r.MaterialCategoryId.Value }
                        : null,
                },
                UnitOfMeasure = uoms.GetValueOrDefault(r.Id),
                TotalAvailableQuantity = r.TotalAvailableQuantity,
            })
            .ToList();

        return result;
    }

    public async Task<
        Result<Paginateable<IEnumerable<MaterialBatchDepartmentDto>>>
    > GetMaterialsWithBatchesAndDepartments(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind? kind,
        Guid? departmentId
    )
    {
        var query = context
            .Materials.Include(m => m.Batches)
            .Include(m => m.Departments)
                .ThenInclude(md => md.Department)
            .AsQueryable();

        if (kind.HasValue)
        {
            query = query.Where(m => m.Kind == kind.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(m =>
                m.Departments.Any(md => md.DepartmentId == departmentId.Value)
            );
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Name, m => m.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            m => new MaterialBatchDepartmentDto
            {
                Material = mapper.Map<MaterialDto>(m),

                Batches = m.Batches.Select(mapper.Map<MaterialBatchDto>).ToList(),
                ProductionDepartments = departmentId.HasValue
                    // Only selected department
                    ? m
                        .Departments.Where(md => md.DepartmentId == departmentId.Value)
                        .Select(md => new MaterialDepartmentDto
                        {
                            Department = new CollectionItemDto
                            {
                                Id = md.DepartmentId,
                                Name = md.Department.Name,
                            },
                        })
                        .ToList()
                    // All departments
                    : m
                        .Departments.Select(md => new MaterialDepartmentDto
                        {
                            Department = new CollectionItemDto
                            {
                                Id = md.DepartmentId,
                                Name = md.Department.Name,
                            },
                        })
                        .ToList(),
            }
        );
    }

    public async Task<
        Result<Paginateable<IEnumerable<ShelfMaterialBatchDto>>>
    > GetMaterialBatchesByMaterialIdV2(int page, int pageSize, Guid materialId, Guid userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
        if (material is null)
            return MaterialErrors.NotFound(materialId);

        var warehouse =
            material.Kind == MaterialKind.Raw
                ? user.GetUserRawWarehouse()
                : user.GetUserPackagingWarehouse();

        if (warehouse is null)
            return UserErrors.WarehouseNotFound(material.Kind);

        var query = context
            .ShelfMaterialBatches.AsSplitQuery()
            .Include(m => m.WarehouseLocationShelf)
            .Include(m => m.MaterialBatch)
                .ThenInclude(mb => mb.Checklist)
                    .ThenInclude(cl => cl.Manufacturer)
            .Where(m =>
                m.MaterialBatch.MaterialId == materialId
                && m.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Id
                    == warehouse.Id
                && m.MaterialBatch.Status == BatchStatus.Available
            )
            .OrderBy(m => m.MaterialBatch.ExpiryDate)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ShelfMaterialBatchDto>
        );
    }

    public async Task<Result<decimal>> GetMaterialsInTransit(Guid materialId)
    {
        return await context
            .ShipmentInvoiceItems.Where(s =>
                s.MaterialId == materialId
                && context.ShipmentDocuments.Any(sd =>
                    sd.ShipmentInvoiceId == s.ShipmentInvoiceId && sd.ArrivedAt == null
                )
            ) // Check if linked document hasn't arrived
            .SumAsync(s => s.ExpectedQuantity);
    }

    private List<CurrentLocationDto> GetCurrentLocations(MaterialBatchDto batch)
    {
        // Dictionary to track the total quantity at each location
        var locationQuantities = new Dictionary<CollectionItemDto, decimal>();

        // Track the movements and update the locations accordingly
        foreach (var movement in batch.MassMovements)
        {
            var fromLocation = movement.FromWarehouse;
            var toLocation = movement.ToWarehouse;

            // If moving to a location, increase the quantity at the destination
            if (toLocation is not null)
            {
                locationQuantities.TryAdd(toLocation, 0);
                locationQuantities[toLocation] += movement.Quantity;
            }

            // If moving from a location, decrease the quantity at the origin
            if (fromLocation is not null)
            {
                locationQuantities.TryAdd(fromLocation, 0);
                locationQuantities[fromLocation] -= movement.Quantity;

                // Ensure no negative quantities
                if (locationQuantities[fromLocation] < 0)
                {
                    locationQuantities[fromLocation] = 0;
                }
            }
        }

        // Convert dictionary to list of CurrentLocationDto and return
        return locationQuantities
            .Select(kvp => new CurrentLocationDto
            {
                Location = kvp.Key,
                QuantityAtLocation = kvp.Value,
            })
            .ToList();
    }

    private List<CurrentLocation> GetCurrentLocations(MaterialBatch batch)
    {
        // Dictionary to track the total quantity at each location
        var locationQuantities = new Dictionary<Warehouse, decimal>();

        // Track the movements and update the locations accordingly
        foreach (var movement in batch.MassMovements)
        {
            var fromLocation = movement.FromWarehouse;
            var toLocation = movement.ToWarehouse;

            // If moving to a location, increase the quantity at the destination
            if (toLocation is not null)
            {
                locationQuantities.TryAdd(toLocation, 0);
                locationQuantities[toLocation] += movement.Quantity;
            }

            // If moving from a location, decrease the quantity at the origin
            if (fromLocation is not null)
            {
                locationQuantities.TryAdd(fromLocation, 0);
                locationQuantities[fromLocation] -= movement.Quantity;

                // Ensure no negative quantities
                if (locationQuantities[fromLocation] < 0)
                {
                    locationQuantities[fromLocation] = 0;
                }
            }
        }

        // Convert dictionary to list of CurrentLocationDto and return
        return locationQuantities
            .Select(kvp => new CurrentLocation
            {
                Location = kvp.Key,
                QuantityAtLocation = kvp.Value,
            })
            .ToList();
    }

    // Update Material Batch
    public async Task<Result> UpdateMaterialBatch(
        CreateMaterialBatchRequest request,
        Guid batchId,
        Guid userId
    )
    {
        var existingBatch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == batchId);
        if (existingBatch is null)
        {
            return MaterialErrors.NotFound(batchId);
        }

        mapper.Map(request, existingBatch);
        existingBatch.LastUpdatedById = userId;

        context.MaterialBatches.Update(existingBatch);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete Material Batch (soft delete)
    public async Task<Result> DeleteMaterialBatch(Guid batchId, Guid userId)
    {
        var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == batchId);
        if (batch is null)
        {
            return MaterialErrors.NotFound(batchId);
        }

        batch.DeletedAt = DateTime.UtcNow;
        batch.LastDeletedById = userId;

        context.MaterialBatches.Update(batch);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<decimal>> CheckStockLevel(Guid materialId)
    {
        var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
        if (material == null)
        {
            return MaterialErrors.NotFound(materialId);
        }

        var totalStock = await context
            .MaterialBatches.Where(b =>
                b.MaterialId == materialId && b.Status == BatchStatus.Available
            )
            .SumAsync(b => b.TotalQuantity - b.ConsumedQuantity);

        return totalStock;
    }

    public async Task<Result> MoveMaterialBatchByMaterial(
        MoveMaterialBatchRequest request,
        Guid userId
    )
    {
        var material = await context
            .Materials.Include(m => m.Batches) // Include batches for detailed processing
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId);

        if (material is null)
        {
            return MaterialErrors.NotFound(request.MaterialId);
        }

        var remainingQuantityToMove = request.Quantity;

        // Iterate through batches, prioritizing those with earlier expiry dates
        foreach (var batch in material.Batches.OrderBy(m => m.ExpiryDate))
        {
            // Get the stock for this specific batch at the fromLocation
            var batchStockAtFromWarehouse = await GetBatchStockInLocation(
                batch.Id,
                request.FromWarehouseId
            );

            if (batchStockAtFromWarehouse <= 0)
            {
                continue; // Skip batches with no stock at this location
            }

            // Determine how much to move from this batch
            var quantityToMoveFromBatch = Math.Min(
                remainingQuantityToMove,
                batchStockAtFromWarehouse
            );

            // Create a movement entry for the batch
            var movement = new MassMaterialBatchMovement
            {
                BatchId = batch.Id,
                FromWarehouseId = request.FromWarehouseId,
                ToWarehouseId = request.ToWarehouseId,
                Quantity = quantityToMoveFromBatch,
                MovedAt = DateTime.UtcNow,
                MovedById = userId,
                MovementType = MovementType.BetweenLocations,
            };

            await context.MassMaterialBatchMovements.AddAsync(movement);

            // Create a corresponding event for the move
            var batchEvent = new MaterialBatchEvent
            {
                BatchId = batch.Id,
                Quantity = quantityToMoveFromBatch,
                Type = EventType.Moved,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
            };

            await context.MaterialBatchEvents.AddAsync(batchEvent);

            // Reduce the remaining quantity to move
            remainingQuantityToMove -= quantityToMoveFromBatch;

            if (remainingQuantityToMove <= 0)
            {
                break; // Exit the loop if the desired quantity is moved
            }
        }

        if (remainingQuantityToMove > 0)
        {
            // Not enough stock across all batches to fulfill the request
            return MaterialErrors.InsufficientStock;
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ApproveMaterialBatch(Guid batchId, Guid userId)
    {
        var materialBatch = await context.MaterialBatches.FirstOrDefaultAsync(mb =>
            mb.Id == batchId
        );
        if (materialBatch == null)
        {
            return Error.NotFound("MaterialBatch.NotFound", "Material batch not found.");
        }

        materialBatch.Status = BatchStatus.Approved;
        materialBatch.DateApproved = DateTime.UtcNow;

        context.MaterialBatches.Update(materialBatch);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    private async Task<decimal> GetBatchStockInLocation(Guid batchId, Guid locationId)
    {
        // Sum of quantities moved to this location for the specific batch
        var quantityMovedToWarehouse = await context
            .MassMaterialBatchMovements.Where(m =>
                m.BatchId == batchId && m.ToWarehouseId == locationId
            )
            .SumAsync(m => m.Quantity);

        // Sum of quantities moved out of this location for the specific batch
        var quantityMovedOutOfLocation = await context
            .MassMaterialBatchMovements.Where(m =>
                m.BatchId == batchId && m.FromWarehouseId == locationId
            )
            .SumAsync(m => m.Quantity);

        // Sum of quantities consumed at this location for the specific batch
        var quantityConsumedAtLocation = await context
            .MaterialBatchEvents.Where(e =>
                e.BatchId == batchId
                && e.ConsumptionWarehouseId == locationId
                && e.Type == EventType.Consumed
            )
            .SumAsync(e => e.Quantity);

        // Calculate the total available quantity for the batch in this location
        var totalBatchStockInLocation =
            quantityMovedToWarehouse - quantityMovedOutOfLocation - quantityConsumedAtLocation;

        return totalBatchStockInLocation;
    }

    public async Task<Result> MoveMaterialBatch(
        Guid batchId,
        Guid fromLocationId,
        Guid toLocationId,
        decimal quantity,
        Guid userId
    )
    {
        var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch is null)
        {
            return MaterialErrors.NotFound(batchId);
        }

        // Get the current stock at the source location using the existing method
        var currentStockAtFromWarehouse = await GetMassMaterialStockInWarehouse(
            batch.MaterialId,
            fromLocationId
        );

        if (currentStockAtFromWarehouse.Value < quantity)
        {
            return MaterialErrors.InsufficientStock; // Not enough stock in source location to move
        }

        // Proceed with the movement - updating the batch (no need to adjust ConsumedQuantity here)
        var movement = new MassMaterialBatchMovement
        {
            BatchId = batchId,
            FromWarehouseId = fromLocationId,
            ToWarehouseId = toLocationId,
            Quantity = quantity,
            MovedAt = DateTime.UtcNow,
            MovedById = userId,
            MovementType = MovementType.BetweenLocations, // Regular movement
        };

        // Add the movement entry to the context
        await context.MassMaterialBatchMovements.AddAsync(movement);

        // Create the corresponding event for the move
        var batchEvent = new MaterialBatchEvent
        {
            BatchId = batchId,
            Quantity = quantity,
            Type = EventType.Moved, // Reflecting the movement event
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        };

        // Add the event entry to the context
        await context.MaterialBatchEvents.AddAsync(batchEvent);

        // Save changes to the database
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> MoveMaterialBatchV2(
        MoveShelfMaterialBatchRequest request,
        Guid userId
    )
    {
        var shelfMaterialBatch = await context
            .ShelfMaterialBatches.AsSplitQuery()
            .Include(shelfMaterialBatch => shelfMaterialBatch.MaterialBatch)
                .ThenInclude(materialBatch => materialBatch.Material)
            .FirstOrDefaultAsync(b => b.Id == request.ShelfMaterialBatchId);

        if (shelfMaterialBatch is null)
        {
            return MaterialErrors.NotFound(request.ShelfMaterialBatchId);
        }

        // Calculate the total quantity to be moved
        var totalQuantityToMove = request.MovedShelfBatchMaterials.Sum(m => m.Quantity);

        if (totalQuantityToMove > shelfMaterialBatch.Quantity)
        {
            return MaterialErrors.InsufficientStock; // Not enough stock in source shelf to move
        }

        foreach (var movedBatch in request.MovedShelfBatchMaterials)
        {
            var targetShelf = await context
                .WarehouseLocationShelves.AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(w => w.WarehouseLocationRack)
                    .ThenInclude(w => w.WarehouseLocation)
                        .ThenInclude(w => w.Warehouse)
                .FirstOrDefaultAsync(w => w.Id == movedBatch.WarehouseLocationShelfId);

            if (targetShelf == null)
                return Error.NotFound(
                    "Warehouse.Shelf",
                    "No matching shelf found in first warehouse for material batch"
                );

            var warehouseType = targetShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Type;

            var materialKind = shelfMaterialBatch.MaterialBatch.Material.Kind;

            switch (warehouseType)
            {
                case WarehouseType.RawMaterialStorage when materialKind != MaterialKind.Raw:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse and material do not belong together. Warehouse is raw whiles material is packaging"
                    );
                case WarehouseType.PackagedStorage when materialKind != MaterialKind.Package:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse and material do not belong together. Warehouse is packaging whiles material is raw"
                    );
                case WarehouseType.FinishedGoodsStorage or WarehouseType.Production:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse type does not allow shelf to be assigned"
                    );
                default:
                    await context.ShelfMaterialBatches.AddAsync(
                        new ShelfMaterialBatch
                        {
                            WarehouseLocationShelfId = movedBatch.WarehouseLocationShelfId,
                            MaterialBatchId = shelfMaterialBatch.MaterialBatchId,
                            Quantity = movedBatch.Quantity,
                            UoMId = movedBatch.UomId,
                            Note = movedBatch.Note,
                            CreatedAt = DateTime.UtcNow,
                        }
                    );
                    break;
            }

            shelfMaterialBatch.Quantity -= movedBatch.Quantity;

            if (shelfMaterialBatch.Quantity == 0)
            {
                context.ShelfMaterialBatches.Remove(shelfMaterialBatch);
            }
            else
            {
                context.ShelfMaterialBatches.Update(shelfMaterialBatch);
            }

            var batchEvent = new MaterialBatchEvent
            {
                BatchId = shelfMaterialBatch.MaterialBatchId,
                Quantity = movedBatch.Quantity,
                Type = EventType.Moved,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
            };

            await context.MaterialBatchEvents.AddAsync(batchEvent);
        }

        // Save changes to the database
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> SupplyMaterialBatchToWarehouse(
        SupplyMaterialBatchRequest request,
        Guid userId
    )
    {
        var materialBatch = await context
            .MaterialBatches.AsSplitQuery()
            .Include(materialBatch => materialBatch.Material)
            .FirstOrDefaultAsync(mb => mb.Id == request.MaterialBatchId);

        if (materialBatch == null)
        {
            return Error.NotFound("MaterialBatch.NotFound", "Material batch not found");
        }

        var totalQuantityToAssign = request.ShelfMaterialBatches.Sum(s => s.Quantity);

        if (totalQuantityToAssign > materialBatch.QuantityUnassigned)
        {
            return MaterialErrors.InsufficientStock; // Not enough stock in source shelf to move
        }

        var warehouseLocationShelfIds = request
            .ShelfMaterialBatches.Select(i => i.WarehouseLocationShelfId)
            .ToList();

        var shelves = await context
            .WarehouseLocationShelves.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(warehouseLocationShelf => warehouseLocationShelf.WarehouseLocationRack)
                .ThenInclude(warehouseLocationRack => warehouseLocationRack.WarehouseLocation)
                    .ThenInclude(warehouseLocation => warehouseLocation.Warehouse)
            .Where(s => warehouseLocationShelfIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s);

        foreach (var shelfBatch in request.ShelfMaterialBatches)
        {
            if (
                !shelves.TryGetValue(shelfBatch.WarehouseLocationShelfId, out var shelf)
                || shelf == null
            )
            {
                return Error.NotFound(
                    "Shelf.NotFound",
                    $"Shelf with ID {shelfBatch.WarehouseLocationShelfId} not found"
                );
            }

            var warehouseType = shelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Type;

            var materialKind = materialBatch.Material.Kind;

            switch (warehouseType)
            {
                case WarehouseType.RawMaterialStorage when materialKind != MaterialKind.Raw:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse and material do not belong together. Warehouse is raw whiles material is packaging"
                    );
                case WarehouseType.PackagedStorage when materialKind != MaterialKind.Package:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse and material do not belong together. Warehouse is packaging whiles material is raw"
                    );
                case WarehouseType.FinishedGoodsStorage or WarehouseType.Production:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse type does not allow shelf to be assigned"
                    );
                default:
                    var shelfMaterialBatch = mapper.Map<ShelfMaterialBatch>(shelfBatch);
                    shelfMaterialBatch.MaterialBatchId = request.MaterialBatchId;
                    await context.ShelfMaterialBatches.AddAsync(shelfMaterialBatch);
                    break;
            }

            var movement = new MassMaterialBatchMovement
            {
                BatchId = request.MaterialBatchId,
                ToWarehouseId = shelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Id,
                Quantity = shelfBatch.Quantity,
                MovedAt = DateTime.UtcNow,
                MovedById = userId,
                MovementType = MovementType.ToWarehouse,
            };

            await context.MassMaterialBatchMovements.AddAsync(movement);

            var batchEvent = new MaterialBatchEvent
            {
                BatchId = request.MaterialBatchId,
                Quantity = shelfBatch.Quantity,
                Type = EventType.Supplied,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
            };

            await context.MaterialBatchEvents.AddAsync(batchEvent);
        }

        var warehouse = await context
            .Warehouses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w =>
                w.Id
                == context
                    .WarehouseLocationShelves.FirstOrDefault(s =>
                        s.Id == request.ShelfMaterialBatches.First().WarehouseLocationShelfId
                    )
                    .WarehouseLocationRack.WarehouseLocation.Warehouse.Id
            );

        var history = await context
            .BinCardInformation.AsSplitQuery()
            .IgnoreQueryFilters()
            .Where(b =>
                b.MaterialBatch.MaterialId == materialBatch.MaterialId
                && b.WarehouseId == warehouse.Id
            )
            .Select(b => new { b.QuantityReceived, b.QuantityIssued })
            .ToListAsync();

        var previousBalance =
            history.Sum(x => x.QuantityReceived) - history.Sum(x => x.QuantityIssued);

        var currentBalance = previousBalance + totalQuantityToAssign;

        var arNumber = await context
            .MaterialSamplings.Where(s => s.MaterialBatchId == materialBatch.Id)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.ArNumber)
            .FirstOrDefaultAsync();

        var binCardEvent = new BinCardInformation
        {
            MaterialBatchId = materialBatch.Id,
            Description = warehouse.Name,
            WayBill = "N/A",
            ArNumber = arNumber ?? "N/A",
            QuantityReceived = totalQuantityToAssign,
            QuantityIssued = 0,
            BalanceQuantity = currentBalance,
            UoMId = materialBatch.UoMId,
            CreatedAt = DateTime.UtcNow,
            WarehouseId = warehouse.Id,
        };

        await context.BinCardInformation.AddAsync(binCardEvent);

        materialBatch.QuantityAssigned += totalQuantityToAssign;

        if (materialBatch.QuantityAssigned >= materialBatch.TotalQuantity)
        {
            materialBatch.Status = BatchStatus.Available;
        }

        var grn = await context
            .Grns.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(g => g.MaterialBatches)
            .FirstOrDefaultAsync(g =>
                g.MaterialBatches.Any(mb => mb.Id == request.MaterialBatchId)
            );

        if (grn != null)
        {
            var batches = grn.MaterialBatches;

            if (batches == null || batches.Count == 0)
            {
                grn.Status = Status.Pending;
            }
            else if (batches.All(b => b.Status == BatchStatus.Available))
            {
                grn.Status = Status.Completed;
            }
            else if (batches.Any(b => b.Status == BatchStatus.Available))
            {
                grn.Status = Status.Partial;
            }
            else
            {
                grn.Status = Status.Pending;
            }
        }

        if (!request.MaterialReturnNoteId.HasValue)
        {
            await context.SaveChangesAsync();
            return Result.Success();
        }

        var materialReturnNote = await context
            .MaterialReturnNotes.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(m => m.FullReturns)
                .ThenInclude(fr => fr.MaterialBatchReservedQuantity)
            .Include(m => m.PartialReturns)
            .FirstOrDefaultAsync(m => m.Id == request.MaterialReturnNoteId.Value);

        if (materialReturnNote is null)
            return Error.NotFound("Material.Return", "Material return note not found");

        if (materialReturnNote.IsFullReturn)
        {
            var fullReturn = materialReturnNote.FullReturns.FirstOrDefault(fr =>
                fr.MaterialBatchReservedQuantity.MaterialBatchId == request.MaterialBatchId
            );

            if (fullReturn is not null)
                fullReturn.Returned = true;

            if (materialReturnNote.FullReturns.All(fr => fr.Returned))
                materialReturnNote.Status = MaterialReturnStatus.Completed;
        }
        else
        {
            var partialReturn = materialReturnNote.PartialReturns.FirstOrDefault(pr =>
                pr.MaterialBatchId == request.MaterialBatchId
            );

            if (partialReturn is not null)
                partialReturn.Returned = true;

            if (materialReturnNote.PartialReturns.All(pr => pr.Returned))
                materialReturnNote.Status = MaterialReturnStatus.Completed;
        }

        // mark the batch as returned
        materialBatch.ReturnDate = DateTime.UtcNow;

        context.MaterialReturnNotes.Update(materialReturnNote);
        context.MaterialBatches.Update(materialBatch);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<List<MaterialStockByWarehouseDto>> GetStockByWarehouse(Guid materialId)
    {
        // Get all warehouses
        var warehouses = await context.Warehouses.ToListAsync();
        var stockByWarehouse = new List<MaterialStockByWarehouseDto>();

        foreach (var warehouse in warehouses)
        {
            var stockResult = await GetMassMaterialStockInWarehouse(materialId, warehouse.Id);
            if (stockResult.IsSuccess && stockResult.Value > 0)
            {
                stockByWarehouse.Add(
                    new MaterialStockByWarehouseDto
                    {
                        Warehouse = mapper.Map<CollectionItemDto>(warehouse),
                        TotalQuantity = stockResult.Value,
                    }
                );
            }
        }

        return stockByWarehouse;
    }

    public async Task<Result<List<DepartmentDto>>> GetDepartmentsWithEnoughStock(
        Guid materialId,
        decimal quantity
    )
    {
        var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
        if (material is null)
            return MaterialErrors.NotFound(materialId);

        var warehouseType =
            material.Kind == MaterialKind.Raw
                ? WarehouseType.RawMaterialStorage
                : WarehouseType.PackagedStorage;

        var departments = await context.Departments.ToListAsync();

        var result = new List<DepartmentDto>();

        foreach (var department in departments)
        {
            // Get all warehouses associated with this department
            var warehouse = await context
                .Warehouses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(dw =>
                    dw.DepartmentId == department.Id && dw.Type == warehouseType
                );

            if (warehouse is null)
            {
                continue;
            }

            decimal totalStock = 0;

            var stockResult = await GetMassMaterialStockInWarehouse(materialId, warehouse.Id);
            if (stockResult.IsSuccess)
            {
                totalStock = stockResult.Value;
            }

            if (totalStock >= quantity)
            {
                result.Add(mapper.Map<DepartmentDto>(department));
            }
        }

        return result;
    }

    public async Task<List<MaterialStockByDepartmentDto>> GetStockByDepartment(Guid materialId)
    {
        // Get all departments
        var departments = await context.Departments.ToListAsync();
        var stockByDepartment = new List<MaterialStockByDepartmentDto>();

        foreach (var department in departments)
        {
            // Get all warehouses associated with this department
            var warehouseIds = await context
                .Warehouses.Where(dw => dw.DepartmentId == department.Id)
                .Select(dw => dw.Id)
                .ToListAsync();

            decimal totalStock = 0;

            foreach (var warehouseId in warehouseIds)
            {
                var stockResult = await GetMassMaterialStockInWarehouse(materialId, warehouseId);
                if (stockResult.IsSuccess)
                {
                    totalStock += stockResult.Value;
                }
            }

            if (totalStock > 0)
            {
                stockByDepartment.Add(
                    new MaterialStockByDepartmentDto
                    {
                        Department = mapper.Map<CollectionItemDto>(department),
                        TotalQuantity = totalStock,
                    }
                );
            }
        }

        return stockByDepartment;
    }

    public async Task<Result<decimal>> GetMaterialStockInWarehouseByBatch(
        Guid batchId,
        Guid warehouseId
    )
    {
        var totalQuantityInWarehouse = await context
            .ShelfMaterialBatches.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(smb => smb.WarehouseLocationShelf)
                .ThenInclude(shelf => shelf.WarehouseLocationRack)
                    .ThenInclude(rack => rack.WarehouseLocation)
            .Where(smb =>
                smb.MaterialBatchId == batchId
                && smb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId
                    == warehouseId
                && !smb.DeletedAt.HasValue
            )
            .SumAsync(smb => smb.Quantity);

        return totalQuantityInWarehouse;
    }

    public async Task<Result<decimal>> GetProductStockInWarehouseByBatch(
        Guid productId,
        Guid warehouseId
    )
    {
        // Sum of quantities moved to this location (incoming batches)
        var batchesInLocation = await context
            .FinishedProductBatchMovements.Include(m => m.Product)
            .Include(m => m.ToWarehouse)
            .Where(m => m.ProductId == productId && m.ToWarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);

        // Sum of quantities moved out of this location (outgoing batches)
        var batchesMovedOut = await context
            .FinishedProductBatchMovements.Include(m => m.Product)
            .Include(m => m.FromWarehouse)
            .Where(m =>
                m.ProductId == productId
                && m.FromWarehouse != null
                && m.FromWarehouseId == warehouseId
            )
            .SumAsync(m => m.Quantity);

        // Sum of the consumed quantities at this location for the given material
        // var batchesConsumedAtLocation = await context.FinishedProductBatchEvents
        //     .Include(m => m.Batch)
        //     .Include(m => m.ConsumptionWarehouse)
        //     .Where(e => e.BatchId == batchId
        //                 && e.ConsumptionWarehouse != null
        //                 && e.ConsumptionWarehouseId == warehouseId
        //                 && e.Type == EventType.Consumed)
        //     .SumAsync(e => e.Quantity);

        // Calculate the total available quantity for the material in this location
        var totalQuantityInLocation = batchesInLocation - batchesMovedOut; // - batchesConsumedAtLocation;

        return totalQuantityInLocation;
    }

    public async Task<Result<decimal>> GetMassMaterialStockInWarehouse(
        Guid materialId,
        Guid warehouseId
    )
    {
        var batchesInLocation = await context
            .MassMaterialBatchMovements.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(m => m.Batch)
            .Include(m => m.ToWarehouse)
            .Where(m =>
                m.Batch.Status == BatchStatus.Available
                && m.Batch.MaterialId == materialId
                && m.ToWarehouseId == warehouseId
            )
            .SumAsync(m => m.Quantity);

        var batchesMovedOut = await context
            .MassMaterialBatchMovements.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(m => m.Batch)
            .Include(m => m.FromWarehouse)
            .Where(m =>
                m.Batch.Status == BatchStatus.Available
                && m.Batch.MaterialId == materialId
                && m.FromWarehouse != null
                && m.FromWarehouseId == warehouseId
            )
            .SumAsync(m => m.Quantity);

        var batchesConsumedAtLocation = await context
            .MaterialBatchEvents.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(m => m.Batch)
            .Include(m => m.ConsumptionWarehouse)
            .Where(e =>
                e.Batch.Status == BatchStatus.Available
                && e.Batch.MaterialId == materialId
                && e.ConsumptionWarehouse != null
                && e.ConsumptionWarehouseId == warehouseId
                && e.Type == EventType.Consumed
            )
            .SumAsync(e => e.Quantity);

        var batchReservedQuantities = await context
            .MaterialBatchReservedQuantities.AsSplitQuery()
            .Include(m => m.MaterialBatch)
            .Where(m =>
                m.MaterialBatch.MaterialId == materialId /* && m.WarehouseId == warehouseId*/
            )
            .SumAsync(e => e.Quantity);

        var totalQuantityInLocation =
            batchesInLocation
            - batchesMovedOut
            - batchesConsumedAtLocation
            - batchReservedQuantities;

        return Math.Max(totalQuantityInLocation, 0);
    }

    public async Task<Result<decimal>> GetShelfMaterialStockInWarehouse(
        Guid materialId,
        Guid warehouseId
    )
    {
        // Sum of all quantities for shelves in the given warehouse for the given material
        var totalQuantity = await context
            .ShelfMaterialBatches.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(s => s.MaterialBatch)
            .Include(s => s.WarehouseLocationShelf)
                .ThenInclude(wls => wls.WarehouseLocationRack)
                    .ThenInclude(w => w.WarehouseLocation)
                        .ThenInclude(wl => wl.Warehouse)
            .Where(s =>
                s.MaterialBatch.MaterialId == materialId
                && s.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId
                    == warehouseId
                && !s.DeletedAt.HasValue
            )
            .SumAsync(s => s.Quantity);

        return Math.Max(totalQuantity, 0);
    }

    public async Task<Result<IEnumerable<ShelfMaterialBatchDto>>> GetShelfMaterialsAcrossWarehouses(
        Guid materialId,
        Guid? departmentId,
        bool? onlyAboutToExpire
    )
    {
        var shelfMaterialBatches = await context
            .ShelfMaterialBatches.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(s => s.MaterialBatch)
            .Include(s => s.WarehouseLocationShelf)
                .ThenInclude(wls => wls.WarehouseLocationRack)
                    .ThenInclude(w => w.WarehouseLocation)
                        .ThenInclude(wl => wl.Warehouse)
            .Where(s => s.MaterialBatch.MaterialId == materialId && !s.DeletedAt.HasValue)
            .ToListAsync();

        if (departmentId.HasValue)
        {
            shelfMaterialBatches = shelfMaterialBatches
                .Where(s =>
                    s.WarehouseLocationShelf
                        .WarehouseLocationRack
                        .WarehouseLocation
                        .Warehouse
                        .DepartmentId == departmentId
                )
                .ToList();
        }
        var shelfMaterialBatchesDto = mapper.Map<List<ShelfMaterialBatchDto>>(shelfMaterialBatches);

        shelfMaterialBatchesDto = shelfMaterialBatchesDto
            .OrderByDescending(s => s.MaterialBatch.AboutToExpire)
            .ThenByDescending(s => s.MaterialBatch.Expired)
            .ThenByDescending(s => s.MaterialBatch.ExpiryDate)
            .ToList();

        if (onlyAboutToExpire.HasValue)
        {
            if (onlyAboutToExpire.Value)
            {
                shelfMaterialBatchesDto = shelfMaterialBatchesDto
                    .Where(s => s.MaterialBatch.AboutToExpire)
                    .ToList();
            }
        }

        return shelfMaterialBatchesDto;
    }

    public async Task<Result<IEnumerable<ShelfMaterialBatchDto>>> GetShelfMaterialsAcrossWarehouses(
        string searchQuery,
        Guid departmentId
    )
    {
        var shelfMaterialBatches = context
            .ShelfMaterialBatches.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(s => s.MaterialBatch)
                .ThenInclude(mb => mb.Material)
            .Include(s => s.WarehouseLocationShelf)
                .ThenInclude(wls => wls.WarehouseLocationRack)
                    .ThenInclude(w => w.WarehouseLocation)
                        .ThenInclude(wl => wl.Warehouse)
            .Where(s =>
                s.WarehouseLocationShelf
                    .WarehouseLocationRack
                    .WarehouseLocation
                    .Warehouse
                    .DepartmentId == departmentId
                && !s.DeletedAt.HasValue
                && s.Quantity != 0
            )
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            shelfMaterialBatches = shelfMaterialBatches.WhereSearch(
                searchQuery,
                s => s.MaterialBatch.Material.Code,
                s => s.MaterialBatch.Material.Name
            );
        }

        var shelfMaterialBatchesDto = mapper.Map<List<ShelfMaterialBatchDto>>(
            await shelfMaterialBatches.ToListAsync()
        );

        shelfMaterialBatchesDto = shelfMaterialBatchesDto
            .OrderByDescending(s => s.MaterialBatch.AboutToExpire)
            .ThenByDescending(s => s.MaterialBatch.Expired)
            .ThenByDescending(s => s.MaterialBatch.ExpiryDate)
            .ToList();

        return shelfMaterialBatchesDto;
    }

    public async Task<Result<decimal>> GetFrozenMaterialStockInWarehouse(
        Guid materialId,
        Guid warehouseId
    )
    {
        // Sum of quantities moved to this location (incoming batches)
        var batchesInLocation = await context
            .MassMaterialBatchMovements.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(m => m.Batch)
            .Include(m => m.ToWarehouse)
            .Where(m =>
                m.Batch.Status == BatchStatus.Frozen
                && m.Batch.MaterialId == materialId
                && m.ToWarehouseId == warehouseId
            )
            .SumAsync(m => m.Quantity);

        // Sum of quantities moved out of this location (outgoing batches)
        var batchesMovedOut = await context
            .MassMaterialBatchMovements.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(m => m.Batch)
            .Include(m => m.FromWarehouse)
            .Where(m =>
                m.Batch.Status == BatchStatus.Frozen
                && m.Batch.MaterialId == materialId
                && m.FromWarehouse != null
                && m.FromWarehouseId == warehouseId
            )
            .SumAsync(m => m.Quantity);

        // Sum of the consumed quantities at this location for the given material
        var batchesConsumedAtLocation = await context
            .MaterialBatchEvents.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(m => m.Batch)
            .Include(m => m.ConsumptionWarehouse)
            .Where(e =>
                e.Batch.Status == BatchStatus.Frozen
                && e.Batch.MaterialId == materialId
                && e.ConsumptionWarehouse != null
                && e.ConsumptionWarehouseId == warehouseId
                && e.Type == EventType.Consumed
            )
            .SumAsync(e => e.Quantity);

        // Calculate the total available quantity for the material in this location
        var totalQuantityInLocation =
            batchesInLocation - batchesMovedOut - batchesConsumedAtLocation;

        return totalQuantityInLocation;
    }

    public async Task<Result<List<MaterialBatchDto>>> GetFrozenMaterialBatchesInWarehouse(
        Guid materialId,
        Guid warehouseId
    )
    {
        var frozenBatches = await context
            .MaterialBatches.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(b => b.Material)
            .Include(b => b.UoM)
            .Where(b =>
                b.Status == BatchStatus.Frozen
                && b.MaterialId == materialId
                && context.MassMaterialBatchMovements.Any(m =>
                    m.BatchId == b.Id && m.ToWarehouseId == warehouseId
                )
                && !context.MassMaterialBatchMovements.Any(m =>
                    m.BatchId == b.Id && m.FromWarehouseId == warehouseId
                )
            )
            .ToListAsync();

        return mapper.Map<List<MaterialBatchDto>>(frozenBatches);
    }

    public async Task<Result<List<BatchToSupply>>> GetFrozenBatchesForRequisitionItem(
        Guid materialId,
        Guid warehouseId,
        decimal requestedQuantity
    )
    {
        var result = new List<BatchToSupply>();
        var remainingQuantityToFulfill = requestedQuantity;

        // Fetch frozen batches in FIFO order
        var frozenBatches = await context
            .MaterialBatches.AsSplitQuery()
            .Include(b => b.Material)
            .Include(b => b.UoM)
            .Where(b =>
                b.Status == BatchStatus.Frozen
                && b.MaterialId == materialId
                && context.MassMaterialBatchMovements.Any(m =>
                    m.BatchId == b.Id && m.ToWarehouseId == warehouseId
                )
                && !context.MassMaterialBatchMovements.Any(m =>
                    m.BatchId == b.Id && m.FromWarehouseId == warehouseId
                )
            )
            .OrderBy(b => b.ExpiryDate) // FIFO (First-In, First-Out)
            .ToListAsync();

        foreach (var batch in frozenBatches)
        {
            if (remainingQuantityToFulfill <= 0)
                break; // Stop once the required quantity is fulfilled

            // Get the available quantity in the warehouse for this batch
            var availableQuantityResult = await GetMaterialStockInWarehouseByBatch(
                batch.Id,
                warehouseId
            );
            if (availableQuantityResult.IsFailure)
            {
                continue;
            }

            var availableQuantity = availableQuantityResult.Value;
            if (availableQuantity <= 0)
                continue; // Skip batches with no stock

            // Determine how much can be taken from this batch
            var quantityToTake = Math.Min(availableQuantity, remainingQuantityToFulfill);

            // Add batch to the result list
            var batchDto = mapper.Map<MaterialBatchListDto>(batch);
            result.Add(new BatchToSupply { Batch = batchDto, QuantityToTake = quantityToTake });

            remainingQuantityToFulfill -= quantityToTake; // Reduce the required quantity
        }

        if (remainingQuantityToFulfill > 0)
        {
            return Error.Failure(
                "Batch.Failure",
                $"Not enough frozen stock available to supply {requestedQuantity}. Short by {remainingQuantityToFulfill}."
            );
        }

        return result;
    }

    public Result<List<BatchLocation>> BatchesNeededToBeConsumed(
        Guid materialId,
        Guid warehouseId,
        decimal quantity
    )
    {
        var result = new List<BatchLocation>();
        var remainingQuantityToFulfill = quantity;

        // Fetch batches sorted by expiry date (FIFO order)
        var batches = context
            .MaterialBatches.AsSplitQuery()
            .Where(b =>
                b.MaterialId == materialId
                && b.MassMovements.Any(m => m.ToWarehouseId == warehouseId)
            ) // Ensure the batch is in the warehouse
            .OrderBy(b => b.ExpiryDate) // FIFO
            .Include(b => b.MassMovements)
                .ThenInclude(m => m.ToWarehouse)
            .Include(b => b.MassMovements)
                .ThenInclude(m => m.FromWarehouse)
            .ToList();

        foreach (var batch in batches)
        {
            if (remainingQuantityToFulfill <= 0)
                break; // Stop once the required quantity is fulfilled

            var currentLocations = GetCurrentLocations(batch);
            foreach (var currentLocation in currentLocations)
            {
                if (currentLocation.Location.Id != warehouseId)
                    continue; // Ensure we're looking at the correct warehouse

                if (remainingQuantityToFulfill <= 0)
                    break; // Stop if we've met the required quantity

                if (currentLocation.QuantityAtLocation <= 0)
                    continue; // Skip batches with no remaining stock

                // Determine how much could potentially be taken from this batch
                var quantityToConsider = Math.Min(
                    currentLocation.QuantityAtLocation,
                    remainingQuantityToFulfill
                );

                // Add batch to the result list
                result.Add(
                    new BatchLocation
                    {
                        ConsumptionLocation = mapper.Map<WarehouseDto>(currentLocation.Location),
                        Batch = mapper.Map<MaterialBatchDto>(batch),
                        QuantityToUse = quantityToConsider,
                    }
                );

                remainingQuantityToFulfill -= quantityToConsider; // Reduce the required quantity
            }
        }

        if (remainingQuantityToFulfill > 0)
        {
            return Error.Failure(
                "Batch.Failure",
                $"Not enough stock available to fulfill {quantity}. Short by {remainingQuantityToFulfill}."
            );
        }

        return result;
    }

    public async Task<Result<List<BatchToSupply>>> BatchesToSupplyForGivenQuantity(
        Guid materialId,
        Guid warehouseId,
        decimal quantity
    )
    {
        var result = new List<BatchToSupply>();
        var remainingQuantityToFulfill = quantity;

        // Fetch batches in the given warehouse, sorted by return date (if any) and expiry (FIFO)
        var batches = await context
            .MaterialBatches.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(b => b.UoM)
            .Include(b => b.ShelfMaterialBatches)
                .ThenInclude(smb => smb.WarehouseLocationShelf)
                    .ThenInclude(shelf => shelf.WarehouseLocationRack)
                        .ThenInclude(rack => rack.WarehouseLocation)
            .Where(b =>
                b.MaterialId == materialId
                && b.ShelfMaterialBatches.Any(smb =>
                    smb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId
                    == warehouseId
                )
            )
            .OrderBy(b => b.ReturnDate == null) // false (not null) first, true (null) last
            .ThenBy(b => b.ReturnDate) // earliest non-null return dates first
            .ThenBy(b => b.ExpiryDate)
            .ToListAsync();

        foreach (var batch in batches)
        {
            if (remainingQuantityToFulfill <= 0)
                break; // Stop if we've met the required quantity

            // Look at the shelf-level quantities
            var shelfBatches = batch
                .ShelfMaterialBatches.Where(smb =>
                    smb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId
                    == warehouseId
                )
                .OrderBy(_ => batch.ReturnDate == null) // returned batches first
                .ThenBy(_ => batch.ReturnDate) // earliest return date first
                .ThenBy(_ => batch.ExpiryDate) // then by expiry
                .ToList();

            foreach (var shelfBatch in shelfBatches)
            {
                if (remainingQuantityToFulfill <= 0)
                    break;

                if (shelfBatch.Quantity <= 0)
                    continue;

                var quantityToTake = Math.Min(shelfBatch.Quantity, remainingQuantityToFulfill);

                var batchDto = mapper.Map<MaterialBatchListDto>(batch);
                result.Add(
                    new BatchToSupply
                    {
                        Batch = batchDto,
                        QuantityToTake = quantityToTake,
                        WarehouseLocationShelfId = shelfBatch.WarehouseLocationShelfId, // shelf origin
                    }
                );

                remainingQuantityToFulfill -= quantityToTake;
            }
        }

        if (remainingQuantityToFulfill > 0)
        {
            return Error.Failure(
                "Batch.Failure",
                $"Not enough stock available to supply {quantity}. Short by {remainingQuantityToFulfill}."
            );
        }

        return result;
    }

    public async Task<Result> ConsumeMaterialAtLocation(
        Guid batchId,
        Guid locationId,
        decimal quantity,
        Guid userId
    )
    {
        var materialBatchEvent = new MaterialBatchEvent
        {
            BatchId = batchId,
            Quantity = quantity,
            UserId = userId,
            Type = EventType.Consumed,
            ConsumptionWarehouseId = locationId,
            ConsumedAt = DateTime.UtcNow,
        };

        // Optionally update the batch's consumed quantity
        var materialBatch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == batchId);

        if (materialBatch == null)
            return Error.Failure("Material.Batch", "Material batch not found.");

        materialBatch.ConsumedQuantity += quantity;
        context.MaterialBatches.Update(materialBatch);

        // Add the event to the context
        await context.MaterialBatchEvents.AddAsync(materialBatchEvent);

        // Save changes to the database
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> FreezeMaterialBatchAsync(Guid batchId)
    {
        var materialBatch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == batchId);

        if (materialBatch == null)
            return Error.Failure("Material.Batch", "Material batch not found.");

        materialBatch.Status = BatchStatus.Frozen;
        context.MaterialBatches.Update(materialBatch);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ReserveQuantityFromBatchForProduction(
        Guid batchId,
        Guid warehouseId,
        Guid productionScheduleProductId,
        decimal quantity,
        Guid? uoMId,
        Guid? warehouseLocationShelfId
    )
    {
        // 1️⃣ Load the shelf batch if shelf is provided
        if (warehouseLocationShelfId.HasValue)
        {
            var shelfBatch = await context
                .ShelfMaterialBatches.IgnoreQueryFilters()
                .FirstOrDefaultAsync(smb =>
                    smb.MaterialBatchId == batchId
                    && smb.WarehouseLocationShelfId == warehouseLocationShelfId.Value
                    && !smb.DeletedAt.HasValue
                );

            if (shelfBatch == null)
                return Error.NotFound(
                    "ShelfMaterialBatch",
                    "No shelf allocation found for this batch."
                );

            if (shelfBatch.Quantity < quantity)
                return Error.Validation(
                    "ShelfMaterialBatch",
                    $"Not enough stock on the shelf to reserve."
                        + $" Available shelf quantity: {shelfBatch.Quantity}. Required shelf quantity: {quantity}"
                );
        }

        // 2️⃣ Create the reservation entry
        var reservation = new MaterialBatchReservedQuantity
        {
            MaterialBatchId = batchId,
            WarehouseId = warehouseId,
            ProductionScheduleProductId = productionScheduleProductId,
            Quantity = quantity,
            UoMId = uoMId,
            WarehouseLocationShelfId = warehouseLocationShelfId,
        };

        await context.MaterialBatchReservedQuantities.AddAsync(reservation);

        // 3️⃣ Save changes
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<
        List<MaterialBatchReservedQuantityDto>
    > GetReservedBatchesAndQuantityForProductionWarehouse(
        Guid materialId,
        Guid warehouseId,
        Guid productionScheduleProductId
    )
    {
        return mapper.Map<List<MaterialBatchReservedQuantityDto>>(
            await context
                .MaterialBatchReservedQuantities.AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(r => r.MaterialBatch)
                    .ThenInclude(b => b.Material)
                .Include(b => b.WarehouseLocationShelf)
                .Where(r =>
                    r.MaterialBatch.MaterialId == materialId
                    && r.WarehouseId == warehouseId
                    && r.ProductionScheduleProductId == productionScheduleProductId
                    && r.DeletedAt == null
                )
                .ToListAsync()
        );
    }

    public async Task<
        List<MaterialBatchReservedQuantityDto>
    > GetConsumedBatchesAndQuantityForProductionWarehouse(
        Guid materialId,
        Guid warehouseId,
        Guid productionScheduleProductId
    )
    {
        return mapper.Map<List<MaterialBatchReservedQuantityDto>>(
            await context
                .MaterialBatchReservedQuantities.AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(r => r.MaterialBatch)
                    .ThenInclude(b => b.Material)
                .Include(b => b.WarehouseLocationShelf)
                .Where(r =>
                    r.MaterialBatch.MaterialId == materialId
                    && r.WarehouseId == warehouseId
                    && r.ProductionScheduleProductId == productionScheduleProductId
                    && r.DeletedAt != null
                )
                .ToListAsync()
        );
    }

    public async Task<Result> ConsumeMaterialAtLocation(
        Material material,
        Guid locationId,
        decimal quantity,
        Guid userId
    )
    {
        var materialBatchEvents = new List<MaterialBatchEvent>();
        var remainingQuantityToConsume = quantity;

        foreach (var batch in material.Batches.OrderBy(b => b.ExpiryDate))
        {
            if (remainingQuantityToConsume <= 0)
                break; // Stop if we've consumed all the required quantity

            if (batch.RemainingQuantity <= 0)
                continue; // Skip batches with no remaining stock

            // Consume the minimum of what's available in the batch or the remaining needed quantity
            var quantityToConsumeFromThisBatch = Math.Min(
                batch.RemainingQuantity,
                remainingQuantityToConsume
            );

            // Create a batch event for this consumption
            var materialBatchEvent = new MaterialBatchEvent
            {
                BatchId = batch.Id,
                Quantity = quantityToConsumeFromThisBatch,
                UserId = userId,
                Type = EventType.Consumed,
                ConsumptionWarehouseId = locationId,
                ConsumedAt = DateTime.UtcNow,
            };

            // Update batch quantities
            batch.ConsumedQuantity += quantityToConsumeFromThisBatch;
            remainingQuantityToConsume -= quantityToConsumeFromThisBatch;

            materialBatchEvents.Add(materialBatchEvent);
            context.MaterialBatches.Update(batch);
        }

        if (remainingQuantityToConsume > 0)
        {
            return Error.Failure(
                "Batch.Consume",
                $"Not enough stock available to consume the requested quantity. Remaining: {remainingQuantityToConsume}"
            );
        }

        // Add all batch events to the context
        await context.MaterialBatchEvents.AddRangeAsync(materialBatchEvents);

        // Save changes to the database
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<WarehouseStockDto>>> GetMaterialStockAcrossWarehouses(
        Guid materialId
    )
    {
        var warehouses = await context.Warehouses.IgnoreQueryFilters().ToListAsync();

        var warehouseStockList = new List<WarehouseStockDto>();

        foreach (var warehouse in warehouses)
        {
            // 1️⃣ Get raw stock in warehouse
            var stockResult = await GetShelfMaterialStockInWarehouse(materialId, warehouse.Id);
            if (!stockResult.IsSuccess)
                continue;
            var grossStock = stockResult.Value;

            // 2️⃣ Add reserved if this is a production warehouse
            var reservedQty = await context
                .MaterialBatchReservedQuantities.Where(r =>
                    r.WarehouseId == warehouse.Id
                    && r.MaterialBatch.MaterialId == materialId
                    && r.DeletedAt == null
                )
                .SumAsync(r => r.Quantity);

            var finalStock = grossStock;

            if (warehouse.Type == WarehouseType.Production)
            {
                // Production stock = gross stock + reserved (because reservations are physically there)
                finalStock += reservedQty;
            }
            else
            {
                // Other warehouses = gross stock - reserved (because those materials left the shelf)
                finalStock -= reservedQty;
            }

            if (finalStock <= 0)
                continue;

            warehouseStockList.Add(
                new WarehouseStockDto
                {
                    Warehouse = mapper.Map<WarehouseDto>(warehouse),
                    StockQuantity = finalStock,
                }
            );
        }

        warehouseStockList = warehouseStockList.OrderByDescending(w => w.StockQuantity).ToList();
        return Result.Success(warehouseStockList);
    }

    public async Task<Result> ImportMaterialsFromExcel(IFormFile file, MaterialKind kind)
    {
        if (file == null || file.Length == 0)
            return UploadErrors.EmptyFile;

        var materials = new List<Material>();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return UploadErrors.WorksheetNotFound;

        // Read headers
        var headers = new Dictionary<string, int>();
        for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            headers[worksheet.Cells[1, col].Text.Trim()] = col;
        }

        // Validate required headers
        var requiredHeaders = new[] { "Code", "Name", "Category" };
        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        // Read data rows
        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var categoryName = worksheet.Cells[row, headers["Category"]].Text.Trim().ToLower();
            var category = context.MaterialCategories.FirstOrDefault(m =>
                m.Name != null && m.Name.Trim().ToLower() == categoryName
            );
            string pharmacopoeia = null;

            try
            {
                pharmacopoeia = worksheet.Cells[row, headers["Pharmacopoeia"]].Text.Trim();
            }
            catch (Exception)
            {
                //ignore
            }

            var material = new Material
            {
                Code = worksheet.Cells[row, headers["Code"]].Text.Trim(),
                Name = worksheet.Cells[row, headers["Name"]].Text.Trim(),
                Description = "",
                Pharmacopoeia = pharmacopoeia,
                MaterialCategoryId = category?.Id,
                Kind = kind, // Replace with your logic for Kind if needed
            };

            materials.Add(material);
        }

        materials = materials.DistinctBy(m => m.Code).ToList();
        await context.Materials.AddRangeAsync(materials);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ImportMaterialsFromExcel(string filePath, MaterialKind kind)
    {
        if (!File.Exists(filePath))
            return UploadErrors.EmptyFile;

        var materials = new List<Material>();

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
            return UploadErrors.WorksheetNotFound;

        // Read headers
        var headers = new Dictionary<string, int>();
        for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            headers[worksheet.Cells[1, col].Text.Trim()] = col;
        }

        // Validate required headers
        var requiredHeaders = new[]
        {
            "Code",
            "Name",
            "Description",
            "Pharmacopoeia",
            "Category",
            "MinimumStockLevel",
            "MaximumStockLevel",
        };
        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        // Read data rows
        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var categoryName = worksheet.Cells[row, headers["Category"]].Text.Trim();
            var category = await context.MaterialCategories.FirstOrDefaultAsync(m =>
                m.Name.Equals(categoryName, StringComparison.CurrentCultureIgnoreCase)
            );

            if (category == null)
                return UploadErrors.CategoryNotFound(categoryName);

            var material = new Material
            {
                Code = worksheet.Cells[row, headers["Code"]].Text.Trim(),
                Name = worksheet.Cells[row, headers["Name"]].Text.Trim(),
                Description = worksheet.Cells[row, headers["Description"]].Text.Trim(),
                Pharmacopoeia = worksheet.Cells[row, headers["Pharmacopoeia"]].Text.Trim(),
                MaterialCategoryId = category.Id,
                //MinimumStockLevel = int.TryParse(worksheet.Cells[row, headers["MinimumStockLevel"]].Text.Trim(), out var minStock) ? minStock : 0,
                //MaximumStockLevel = int.TryParse(worksheet.Cells[row, headers["MaximumStockLevel"]].Text.Trim(), out var maxStock) ? maxStock : 0,
                Kind = kind,
            };

            materials.Add(material);
        }

        await context.Materials.AddRangeAsync(materials);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateBatchStatus(UpdateBatchStatusRequest request, Guid userId)
    {
        if (!Enum.TryParse(typeof(BatchStatus), request.Status, true, out var status))
        {
            return Error.Validation("BatchStatus.Invalid", "Invalid batch status.");
        }

        var materialBatches = await context
            .MaterialBatches.Where(mb => request.MaterialBatchIds.Contains(mb.Id))
            .ToListAsync();

        if (materialBatches.Count != request.MaterialBatchIds.Count)
        {
            return Error.NotFound(
                "MaterialBatch.NotFound",
                "One or more material batches not found"
            );
        }

        foreach (var batch in materialBatches)
        {
            batch.Status = (BatchStatus)status;
            batch.UpdatedAt = DateTime.UtcNow;
            batch.LastUpdatedById = userId;
        }

        context.MaterialBatches.UpdateRange(materialBatches);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> CreateMaterialDepartment(
        List<CreateMaterialDepartment> materialDepartments,
        Guid userId
    )
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return UserErrors.NotFound(userId);

        if (!user.DepartmentId.HasValue)
            return UserErrors.DepartmentNotFound;

        // prevent duplicates in request
        if (
            materialDepartments.Select(m => m.MaterialId).Distinct().Count()
            != materialDepartments.Count
        )
            return Error.Validation(
                "MaterialDepartment.Validation",
                "Cant have more than one of the same material Id in the list"
            );

        var departmentId = user.DepartmentId.Value;
        var materialIds = materialDepartments.Select(m => m.MaterialId).ToList();

        // fetch existing once
        var existingMaterialDepartments = await context
            .MaterialDepartments.Where(m =>
                m.DepartmentId == departmentId && materialIds.Contains(m.MaterialId)
            )
            .ToDictionaryAsync(m => m.MaterialId, m => m);

        var newEntities = new List<MaterialDepartment>();

        foreach (var dto in materialDepartments)
        {
            if (existingMaterialDepartments.TryGetValue(dto.MaterialId, out var existing))
            {
                // update existing
                existing.ReOrderLevel = dto.ReOrderLevel;
                existing.MinimumStockLevel = dto.MinimumStockLevel;
                existing.MaximumStockLevel = dto.MaximumStockLevel;
                existing.UoMId = dto.UoMId;
            }
            else
            {
                // create new
                newEntities.Add(
                    new MaterialDepartment
                    {
                        MaterialId = dto.MaterialId,
                        DepartmentId = departmentId,
                        ReOrderLevel = dto.ReOrderLevel,
                        MaximumStockLevel = dto.MaximumStockLevel,
                        MinimumStockLevel = dto.MinimumStockLevel,
                        UoMId = dto.UoMId,
                    }
                );
            }
        }

        if (newEntities.Count > 0)
            await context.MaterialDepartments.AddRangeAsync(newEntities);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> RemoveMaterialDepartment(Guid userId, Guid materialId)
    {
        var user = await context
            .Users.Select(u => new { u.Id, u.DepartmentId })
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return UserErrors.NotFound(userId);

        if (!user.DepartmentId.HasValue)
            return UserErrors.DepartmentNotFound;

        var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
        if (material == null)
            return MaterialErrors.NotFound(materialId);

        var warehouse =
            material.Kind == MaterialKind.Raw
                ? await context
                    .Warehouses.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(w =>
                        w.DepartmentId == user.DepartmentId
                        && w.Type == WarehouseType.RawMaterialStorage
                    )
                : await context
                    .Warehouses.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(w =>
                        w.DepartmentId == user.DepartmentId
                        && w.Type == WarehouseType.PackagedStorage
                    );

        if (warehouse == null)
            return Error.NotFound("Warehouse.NotFound", "Warehouse not found");

        var warehouseStock = await GetShelfMaterialStockInWarehouse(materialId, warehouse.Id);
        if (warehouseStock.IsFailure)
            return warehouseStock.Error;

        if (warehouseStock.Value > 0)
        {
            return Error.Validation(
                "Material.Department",
                "Cannot unlink material, stock for material exists in the warehosue"
            );
        }

        var rowsAffected = await context.Database.ExecuteSqlRawAsync(
            """

                    DELETE FROM "MaterialDepartments"
                    WHERE "DepartmentId" = {0} AND "MaterialId" = {1}
            """,
            user.DepartmentId.Value,
            materialId
        );

        if (rowsAffected == 0)
            return Error.NotFound(
                "MaterialDepartment.NotFound",
                "Material not assigned to this department"
            );

        return Result.Success();
    }

    public async Task<
        Result<Paginateable<IEnumerable<MaterialWithWarehouseStockDto>>>
    > GetMaterialsThatHaveNotBeenLinked(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind? kind,
        Guid userId
    )
    {
        var user = await context
            .Users.Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return UserErrors.NotFound(userId);

        if (!user.DepartmentId.HasValue)
        {
            return UserErrors.DepartmentNotFound;
        }

        var linkedMaterialIds = await context
            .MaterialDepartments.Include(m => m.Material)
            .Where(m => m.DepartmentId == user.DepartmentId)
            .Select(m => m.MaterialId)
            .ToListAsync();

        var unlinkedMaterials = context
            .Materials.Where(m => !linkedMaterialIds.Contains(m.Id))
            .AsQueryable();

        if (kind.HasValue)
        {
            unlinkedMaterials = unlinkedMaterials.Where(m => m.Kind == kind);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            unlinkedMaterials = unlinkedMaterials.WhereSearch(
                searchQuery,
                m => m.Name,
                m => m.Code
            );
        }

        var results = await PaginationHelper.GetPaginatedResultAsync(
            unlinkedMaterials,
            page,
            pageSize,
            mapper.Map<MaterialWithWarehouseStockDto>
        );
        foreach (var result in results.Data)
        {
            var warehouseType =
                result.Kind == MaterialKind.Raw
                    ? WarehouseType.RawMaterialStorage
                    : WarehouseType.PackagedStorage;
            var warehouse = await context.Warehouses.FirstOrDefaultAsync(w =>
                w.DepartmentId == user.DepartmentId && w.Type == warehouseType
            );
            if (warehouse == null)
                continue;
            var warehouseStockResult = await GetMassMaterialStockInWarehouse(
                result.Id,
                warehouse.Id
            );
            if (warehouseStockResult.IsFailure)
                continue;
            result.WarehouseStock = warehouseStockResult.Value;
        }

        return results;
    }

    public async Task<
        Result<Paginateable<IEnumerable<MaterialDepartmentWithWarehouseStockDto>>>
    > GetMaterialDepartments(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind? kind,
        Guid? materialCategoryId,
        string sortLabel,
        SortDirection? sortDirection,
        Guid userId
    )
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return UserErrors.NotFound(userId);

        var query = context
            .MaterialDepartments.AsSplitQuery()
            .Include(m => m.Material)
                .ThenInclude(m => m.MaterialCategory)
            .Include(m => m.UoM)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Material.Name, q => q.Material.Code);
        }

        if (!user.DepartmentId.HasValue)
        {
            return UserErrors.DepartmentNotFound;
        }

        if (user.DepartmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == user.DepartmentId.Value);
        }

        if (kind.HasValue)
        {
            query = query.Where(q => q.Material.Kind == kind);
        }

        if (materialCategoryId.HasValue)
        {
            query = query.Where(m => m.Material.MaterialCategoryId == materialCategoryId.Value);
        }

        query = ApplySorting(query, sortLabel.ToLower(), sortDirection);

        var results = await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialDepartmentWithWarehouseStockDto>
        );

        //results.Data = results.Data.ToList();
        foreach (var result in results.Data)
        {
            var warehouseType =
                result.Material.Kind == MaterialKind.Raw
                    ? WarehouseType.RawMaterialStorage
                    : WarehouseType.PackagedStorage;

            var warehouse = await context
                .Warehouses.IgnoreQueryFilters()
                .AsSplitQuery()
                .FirstOrDefaultAsync(w =>
                    w.DepartmentId == user.DepartmentId && w.Type == warehouseType
                );

            if (warehouse == null)
            {
                return Error.NotFound("Warehouse", "Warehouse not found");
            }

            var warehouseStockResult = await GetShelfMaterialStockInWarehouse(
                result.Material.Id,
                warehouse.Id
            );
            if (warehouseStockResult.IsFailure)
                continue;

            result.WarehouseStock = warehouseStockResult.Value;

            result.PendingStockTransferQuantity = await context
                .StockTransferSources.AsSplitQuery()
                .Include(s => s.StockTransfer)
                .Where(s =>
                    s.StockTransfer.MaterialId == result.Material.Id
                    && s.FromDepartmentId == user.DepartmentId
                    && s.Status == StockTransferStatus.InProgress
                )
                .SumAsync(s => s.Quantity);

            result.ReservedQuantity = await context
                .MaterialBatchReservedQuantities.AsSplitQuery()
                .Include(m => m.MaterialBatch)
                .Where(m =>
                    m.MaterialBatch.MaterialId == result.Material.Id
                    && m.WarehouseId == warehouse.Id
                )
                .SumAsync(e => e.Quantity);
        }

        return results;
    }

    private static IQueryable<MaterialDepartment> ApplySorting(
        IQueryable<MaterialDepartment> query,
        string label,
        SortDirection? direction
    )
    {
        if (string.IsNullOrEmpty(label))
            return query.OrderBy(m => m.Material.Name);

        Expression<Func<MaterialDepartment, object>> keySelector = label.ToLower() switch
        {
            "materialcode" => m => m.Material.Code,
            "categoryname" => m => m.Material.MaterialCategory.Name,
            _ => m => m.Material.Name,
        };

        if (!direction.HasValue)
            return query.OrderBy(keySelector);

        return direction == SortDirection.Ascending
            ? query.OrderBy(keySelector)
            : query.OrderByDescending(keySelector);
    }

    public async Task<Result<UnitOfMeasureDto>> GetUnitOfMeasureForMaterialDepartment(
        Guid materialId,
        Guid userId
    )
    {
        var user = await context
            .Users.Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return UserErrors.NotFound(userId);

        if (!user.DepartmentId.HasValue)
        {
            return UserErrors.DepartmentNotFound;
        }

        return mapper.Map<UnitOfMeasureDto>(
            (
                await context
                    .MaterialDepartments.AsSplitQuery()
                    .Include(materialDepartment => materialDepartment.UoM)
                    .FirstOrDefaultAsync(m =>
                        m.MaterialId == materialId && m.DepartmentId == user.DepartmentId.Value
                    )
            )?.UoM
        );
    }

    public async Task<
        Result<Paginateable<IEnumerable<HoldingMaterialTransferDto>>>
    > GetHoldingMaterialTransfers(
        int page,
        int pageSize,
        string searchQuery,
        bool withProcessed,
        Guid departmentId,
        MaterialKind? kind
    )
    {
        var query = context
            .HoldingMaterialTransfers.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(m => m.Batches)
                .ThenInclude(b => b.MaterialBatch)
                    .ThenInclude(m => m.Material)
            .Include(m => m.Batches)
                .ThenInclude(b => b.MaterialBatch)
                    .ThenInclude(b => b.UoM)
            .Include(m => m.Batches)
                .ThenInclude(b => b.UoM)
            .Include(m => m.Batches)
                .ThenInclude(b => b.SourceWarehouse)
            .Include(m => m.Batches)
                .ThenInclude(b => b.DestinationWarehouse)
            .Where(q =>
                q.Batches.Select(b => b.DestinationWarehouse.DepartmentId).Contains(departmentId)
            )
            .AsQueryable();

        query = withProcessed
            ? query
            : query.Where(q => q.Status == HoldingMaterialTransferStatus.Pending);

        if (kind.HasValue)
        {
            query = query.Where(q => q.Batches.Any(b => b.MaterialBatch.Material.Kind == kind));
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<HoldingMaterialTransferDto>
        );
    }

    public async Task<Result> MoveMaterialBatchToWarehouseFromHolding(
        SupplyMaterialBatchFromHoldingRequest request,
        Guid userId
    )
    {
        var materialBatch = await context
            .MaterialBatches.AsSplitQuery()
            .Include(materialBatch => materialBatch.Material)
            .FirstOrDefaultAsync(mb => mb.Id == request.MaterialBatchId);

        if (materialBatch == null)
        {
            return Error.NotFound("MaterialBatch.NotFound", "Material batch not found");
        }

        var totalQuantityToAssign = request.ShelfMaterialBatches.Sum(s => s.Quantity);

        if (totalQuantityToAssign > materialBatch.QuantityUnassigned)
        {
            return MaterialErrors.InsufficientStock; // Not enough stock in source shelf to move
        }

        var holdingMaterial = await context
            .HoldingMaterialTransfers.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(holdingMaterialTransfer => holdingMaterialTransfer.Batches)
                .ThenInclude(holdingMaterialTransferBatch =>
                    holdingMaterialTransferBatch.SourceWarehouse
                )
            .Include(holdingMaterialTransfer => holdingMaterialTransfer.Batches)
                .ThenInclude(holdingMaterialTransferBatch =>
                    holdingMaterialTransferBatch.DestinationWarehouse
                )
                    .ThenInclude(warehouse => warehouse.ArrivalLocation)
            .Include(holdingMaterialTransfer => holdingMaterialTransfer.Batches)
                .ThenInclude(holdingMaterialTransferBatch =>
                    holdingMaterialTransferBatch.MaterialBatch
                )
            .FirstOrDefaultAsync(m => m.Id == request.HoldingMaterialId);

        if (holdingMaterial is null)
            return Error.NotFound("HoldingMaterial.NotFound", "HoldingMaterial not found");

        foreach (var movedBatch in request.ShelfMaterialBatches)
        {
            var fromWarehouse = holdingMaterial
                .Batches.FirstOrDefault(b => b.MaterialBatchId == materialBatch.Id)
                ?.SourceWarehouse;

            if (fromWarehouse is null)
                return Error.Validation(
                    "HoldingMaterial.FromWarehouse",
                    "No source warehouse associated with holding material"
                );

            var toWarehouse = holdingMaterial
                .Batches.FirstOrDefault(b => b.MaterialBatchId == materialBatch.Id)
                ?.DestinationWarehouse;

            if (toWarehouse is null)
                return Error.Validation(
                    "HoldingMaterial.ToWarehouse",
                    "No destination warehouse associated with holding material"
                );

            var movement = new MassMaterialBatchMovement
            {
                BatchId = materialBatch.Id,
                FromWarehouseId = fromWarehouse.Id,
                ToWarehouseId = toWarehouse.Id,
                Quantity = movedBatch.Quantity,
                MovedAt = DateTime.UtcNow,
                MovedById = userId,
            };

            await context.MassMaterialBatchMovements.AddAsync(movement);

            var materialBatchEvent = new MaterialBatchEvent
            {
                BatchId = materialBatch.Id,
                Quantity = movedBatch.Quantity,
                Type = EventType.Moved,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
            };

            await context.MaterialBatchEvents.AddAsync(materialBatchEvent);

            materialBatch.StockTransferId = holdingMaterial.StockTransferId;

            context.MaterialBatches.Update(materialBatch);

            var targetShelf = await context
                .WarehouseLocationShelves.AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(w => w.WarehouseLocationRack)
                    .ThenInclude(w => w.WarehouseLocation)
                        .ThenInclude(w => w.Warehouse)
                .FirstOrDefaultAsync(w => w.Id == movedBatch.WarehouseLocationShelfId);

            if (targetShelf == null)
                return Error.NotFound(
                    "Warehouse.Shelf",
                    "No matching shelf found in first warehouse for material batch"
                );

            var warehouseType = targetShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Type;

            var materialKind = materialBatch.Material.Kind;

            switch (warehouseType)
            {
                case WarehouseType.RawMaterialStorage when materialKind != MaterialKind.Raw:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse and material do not belong together. Warehouse is raw whiles material is packaging"
                    );
                case WarehouseType.PackagedStorage when materialKind != MaterialKind.Package:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse and material do not belong together. Warehouse is packaging whiles material is raw"
                    );
                case WarehouseType.FinishedGoodsStorage or WarehouseType.Production:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse type does not allow shelf to be assigned"
                    );
                default:
                    await context.ShelfMaterialBatches.AddAsync(
                        new ShelfMaterialBatch
                        {
                            WarehouseLocationShelfId = movedBatch.WarehouseLocationShelfId,
                            MaterialBatchId = materialBatch.Id,
                            Quantity = movedBatch.Quantity,
                            UoMId = movedBatch.UomId,
                            Note = movedBatch.Note,
                            CreatedAt = DateTime.UtcNow,
                        }
                    );
                    break;
            }

            materialBatch.QuantityAssigned += movedBatch.Quantity;

            if (materialBatch.QuantityAssigned >= totalQuantityToAssign)
            {
                materialBatch.Status = BatchStatus.Available;
            }
        }

        holdingMaterial.Status = HoldingMaterialTransferStatus.Transferred;
        context.HoldingMaterialTransfers.Update(holdingMaterial);
        context.MaterialBatches.Update(materialBatch);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> MoveMaterialBatchToWarehouseFromDistribute(
        SupplyMaterialBatchFromHMaterialDistribute request,
        Guid userId
    )
    {
        var materialBatch = await context
            .MaterialBatches.AsSplitQuery()
            .Include(materialBatch => materialBatch.Material)
            .FirstOrDefaultAsync(mb => mb.Id == request.MaterialBatchId);

        if (materialBatch == null)
        {
            return Error.NotFound("MaterialBatch.NotFound", "Material batch not found");
        }

        var grn = await context
            .Grns.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(g => g.MaterialBatches)
            .FirstOrDefaultAsync(g =>
                g.MaterialBatches.Any(mb => mb.Id == request.MaterialBatchId)
            );

        var totalQuantityToAssign = request.ShelfMaterialBatches.Sum(s => s.Quantity);

        if (totalQuantityToAssign > materialBatch.QuantityUnassigned)
        {
            return MaterialErrors.InsufficientStock; // Not enough stock in source shelf to move
        }

        var distributeMaterial = await context
            .DistributeMaterials.AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(distributeMaterial => distributeMaterial.DistributedRequisitionItem)
                .ThenInclude(distributedRequisitionItem =>
                    distributedRequisitionItem.RequisitionItem
                )
            .Include(distributeMaterial => distributeMaterial.DistributedRequisitionItem)
                .ThenInclude(distributedRequisitionItem =>
                    distributedRequisitionItem.DistributedRequisitionMaterial
                )
            .FirstOrDefaultAsync(m => m.Id == request.DistributeMaterialId);

        if (distributeMaterial is null)
            return Error.NotFound("DistributeMaterial.NotFound", "DistributeMaterial not found");

        var arNumber = await context
            .MaterialSamplings.Where(s => s.MaterialBatchId == request.MaterialBatchId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.ArNumber)
            .FirstOrDefaultAsync();

        var movements = new List<MassMaterialBatchMovement>();
        var materialBatchEvents = new List<MaterialBatchEvent>();
        var binCards = new List<BinCardInformation>();

        foreach (var movedBatch in request.ShelfMaterialBatches)
        {
            movements.Add(
                new MassMaterialBatchMovement
                {
                    BatchId = materialBatch.Id,
                    ToWarehouseId = distributeMaterial.WarehouseId,
                    Quantity = movedBatch.Quantity,
                    MovedAt = DateTime.UtcNow,
                    MovedById = userId,
                }
            );

            materialBatchEvents.Add(
                new MaterialBatchEvent
                {
                    BatchId = materialBatch.Id,
                    Quantity = movedBatch.Quantity,
                    Type = EventType.Moved,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                }
            );

            var warehouse = await context
                .Warehouses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(w => w.Id == distributeMaterial.WarehouseId);

            var history = await context
                .BinCardInformation.AsSplitQuery()
                .IgnoreQueryFilters()
                .Where(b =>
                    b.MaterialBatch.MaterialId == materialBatch.MaterialId
                    && b.WarehouseId == distributeMaterial.WarehouseId
                )
                .Select(b => new { b.QuantityReceived, b.QuantityIssued })
                .ToListAsync();

            var previousBalance =
                history.Sum(x => x.QuantityReceived) - history.Sum(x => x.QuantityIssued);

            var currentBalance = previousBalance + movedBatch.Quantity;

            binCards.Add(
                new BinCardInformation
                {
                    MaterialBatchId = materialBatch.Id,
                    UoMId = materialBatch.UoMId,
                    WayBill = grn?.GrnNumber,
                    ArNumber = arNumber ?? "",
                    QuantityReceived = movedBatch.Quantity,
                    QuantityIssued = 0,
                    BalanceQuantity = currentBalance,
                    WarehouseId = warehouse?.Id,
                }
            );

            var targetShelf = await context
                .WarehouseLocationShelves.AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(w => w.WarehouseLocationRack)
                    .ThenInclude(w => w.WarehouseLocation)
                        .ThenInclude(w => w.Warehouse)
                .FirstOrDefaultAsync(w => w.Id == movedBatch.WarehouseLocationShelfId);

            if (targetShelf == null)
                return Error.NotFound(
                    "Warehouse.Shelf",
                    "No matching shelf found in first warehouse for material batch"
                );

            var warehouseType = targetShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Type;

            var materialKind = materialBatch.Material.Kind;

            switch (warehouseType)
            {
                case WarehouseType.RawMaterialStorage when materialKind != MaterialKind.Raw:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse and material do not belong together."
                            + " Warehouse is raw whiles material is packaging"
                    );
                case WarehouseType.PackagedStorage when materialKind != MaterialKind.Package:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse and material do not belong together."
                            + " Warehouse is packaging whiles material is raw"
                    );
                case WarehouseType.FinishedGoodsStorage or WarehouseType.Production:
                    return Error.Validation(
                        "Warehouse.Shelf.Material.Kind",
                        "Warehouse type does not allow shelf to be assigned"
                    );
                default:
                    await context.ShelfMaterialBatches.AddAsync(
                        new ShelfMaterialBatch
                        {
                            WarehouseLocationShelfId = movedBatch.WarehouseLocationShelfId,
                            MaterialBatchId = materialBatch.Id,
                            Quantity = movedBatch.Quantity,
                            UoMId = movedBatch.UomId,
                            Note = movedBatch.Note,
                            CreatedAt = DateTime.UtcNow,
                        }
                    );
                    break;
            }

            materialBatch.QuantityAssigned += movedBatch.Quantity;

            if (materialBatch.QuantityAssigned >= totalQuantityToAssign)
            {
                materialBatch.Status = BatchStatus.Available;
            }
        }

        await context.MassMaterialBatchMovements.AddRangeAsync(movements);
        await context.MaterialBatchEvents.AddRangeAsync(materialBatchEvents);
        await context.BinCardInformation.AddRangeAsync(binCards);

        distributeMaterial.DistributedRequisitionItem.DistributedRequisitionMaterial.Status =
            DistributedRequisitionMaterialStatus.Distributed;
        distributeMaterial.Status = DistributeMaterialStatus.Distributed;
        distributeMaterial.DistributedRequisitionItem.RequisitionItem.QuantityReceived +=
            request.ShelfMaterialBatches.Sum(b => b.Quantity);
        context.DistributeMaterials.Update(distributeMaterial);
        context.MaterialBatches.Update(materialBatch);

        if (grn != null)
        {
            var batches = grn.MaterialBatches;

            if (batches == null || batches.Count == 0)
            {
                grn.Status = Status.Pending;
            }
            else if (batches.All(b => b.Status == BatchStatus.Available))
            {
                grn.Status = Status.Completed;
            }
            else if (batches.Any(b => b.Status == BatchStatus.Available))
            {
                grn.Status = Status.Partial;
            }
            else
            {
                grn.Status = Status.Pending;
            }
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<ReservedMaterialReportDto>>> GetReservedQuantitiesForMaterial(
        Guid materialId,
        Guid? departmentId
    )
    {
        var reservedMaterialQuery = context
            .MaterialBatchReservedQuantities.AsSplitQuery()
            .IgnoreQueryFilters()
            .Where(r =>
                r.MaterialBatch.MaterialId == materialId
                && r.DeletedAt == null
                && (!departmentId.HasValue || r.Warehouse.DepartmentId == departmentId.Value)
            )
            .AsNoTracking();

        var reservedMaterials = await reservedMaterialQuery
            .Select(r => new ReservedMaterialReportDto
            {
                MaterialName = r.MaterialBatch.Material.Name,
                MaterialCode = r.MaterialBatch.Material.Code,
                ProductName = r.ProductionScheduleProduct.Product.Name,
                ProductCode = r.ProductionScheduleProduct.Product.Code,
                ReservedQuantity = r.Quantity,
                UomSymbol = r.UoM.Symbol,
                WarehouseName = r.Warehouse.Name,
                DepartmentName = r.Warehouse.Department.Name,
                DateTime = r.CreatedAt,
                Schedule = r.ProductionScheduleProduct.ProductionSchedule.Code,
                ProductBatchNumber = r.ProductionScheduleProduct.BatchNumber,
                ArNumber = context
                    .MaterialSamplings.Where(s => s.MaterialBatchId == r.MaterialBatchId)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.ArNumber)
                    .FirstOrDefault(),
                ManufacturingDate = r.MaterialBatch.ManufacturingDate,
                ExpiryDate = r.MaterialBatch.ExpiryDate,
                BalanceQuantity =
                    r.MaterialBatch.TotalQuantity
                    - r.MaterialBatch.ConsumedQuantity
                    - r.MaterialBatch.ReservedQuantities.Where(rq => rq.DeletedAt == null)
                        .Sum(rq => rq.Quantity),
                MaterialBatchNumber = r.MaterialBatch.BatchNumber,
            })
            .ToListAsync();

        return Result.Success(reservedMaterials);
    }

    public async Task<Result> ImportMaterialBatchesFromExcel(IFormFile file, Guid userId)
    {
        if (file == null || file.Length == 0)
            return UploadErrors.EmptyFile;

        var batches = new List<MaterialBatch>();

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
            if (!string.IsNullOrWhiteSpace(header))
                headers[header] = col;
        }

        var requiredHeaders = new[]
        {
            "Warehouse",
            "Warehouse Code",
            "Location",
            "Rack",
            "Shelf",
            "Material Code",
            "Material Name",
            "Batch Number",
            "Waybill",
            "AR Number",
            "Manufacturing Date",
            "Expiry Date",
            "Quantity",
            "UOM",
            "Retest Date",
        };

        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            string GetCell(string h) => worksheet.Cells[row, headers[h]].Text.Trim();

            var materialCode = GetCell("Material Code");
            var uomName = GetCell("UOM");
            var warehouseCode = GetCell("Warehouse Code");

            var material = await context.Materials.FirstOrDefaultAsync(m => m.Code == materialCode);
            if (material is null)
                return Error.NotFound(
                    "Material.NotFound",
                    $"Material with code '{materialCode}' not found (row {row})"
                );

            var uom = await context.UnitOfMeasures.FirstOrDefaultAsync(u => u.Name == uomName);
            if (uom is null)
                return Error.NotFound("Uom.NotFound", $"UOM '{uomName}' not found (row {row})");

            var shelfName = GetCell("Shelf");
            var rackName = GetCell("Rack");
            var warehouseLocationName = GetCell("Location");

            var shelf = await context
                .WarehouseLocationShelves.AsSplitQuery()
                .Include(s => s.WarehouseLocationRack)
                    .ThenInclude(r => r.WarehouseLocation)
                        .ThenInclude(l => l.Warehouse)
                .FirstOrDefaultAsync(s =>
                    s.Name == shelfName
                    && s.WarehouseLocationRack.Name == rackName
                    && s.WarehouseLocationRack.WarehouseLocation.Name == warehouseLocationName
                    && s.WarehouseLocationRack.WarehouseLocation.Warehouse.Name == warehouseCode
                );

            if (shelf is null)
                return Error.NotFound("Warehouse.Shelf", $"Shelf not found (row {row})");

            var quantity = decimal.TryParse(GetCell("Quantity"), out var qty) ? qty : 0;

            var batch = new MaterialBatch
            {
                MaterialId = material.Id,
                BatchNumber = GetCell("Batch Number"),
                TotalQuantity = quantity,
                UoMId = uom.Id,
                Status = BatchStatus.Received,
                DateReceived = DateTime.UtcNow,
                ManufacturingDate = DateTime.TryParse(GetCell("Manufacturing Date"), out var mfg)
                    ? mfg
                    : null,
                ExpiryDate = DateTime.TryParse(GetCell("Expiry Date"), out var exp) ? exp : null,
                RetestDate = DateTime.TryParse(GetCell("Retest Date"), out var retest)
                    ? retest
                    : null,
                CreatedById = userId,
                QuantityAssigned = 0,
                QuantityPerContainer = quantity,
                NumberOfContainers = 1,
            };

            var movement = new MassMaterialBatchMovement
            {
                Id = Guid.NewGuid(),
                BatchId = batch.Id,
                ToWarehouseId = shelf.WarehouseLocationRack.WarehouseLocation.WarehouseId,
                Quantity = quantity,
                MovedAt = DateTime.UtcNow,
                MovedById = userId,
                MovementType = MovementType.ToWarehouse,
            };

            var shelfBatch = new ShelfMaterialBatch
            {
                Id = Guid.NewGuid(),
                MaterialBatchId = batch.Id,
                WarehouseLocationShelfId = shelf.Id,
                Quantity = quantity,
                UoMId = uom.Id,
                Note = $"Waybill: {GetCell("Waybill")}, AR#: {GetCell("AR Number")}",
                CreatedAt = DateTime.UtcNow,
            };

            batches.Add(batch);
            await context.MassMaterialBatchMovements.AddAsync(movement);
            await context.ShelfMaterialBatches.AddAsync(shelfBatch);
        }

        await context.MaterialBatches.AddRangeAsync(batches);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<List<MaterialBatchDto>>> GetExpiredMaterialBatches(
        MaterialFilter filter
    )
    {
        var query = await context
            .MaterialBatches.AsSplitQuery()
            .Include(b => b.MassMovements)
            .Include(b => b.Material)
            .Include(b => b.Checklist)
            .Include(b => b.Grn)
            .Where(b => b.ExpiryDate < DateTime.UtcNow)
            .ToListAsync();

        var batches = mapper.Map<List<MaterialBatchDto>>(query);

        foreach (var batch in batches)
        {
            batch.Locations = GetCurrentLocations(batch);
        }

        if (filter.StartDate.HasValue)
        {
            batches = batches.Where(b => b.ExpiryDate >= filter.StartDate.Value).ToList();
        }

        if (filter.EndDate.HasValue)
        {
            batches = batches.Where(b => b.ExpiryDate < filter.EndDate.Value.AddDays(1)).ToList();
        }

        if (filter.WarehouseIds.Count != 0)
        {
            batches = batches
                .Where(b =>
                    b.Locations.Any(l =>
                        l.Location?.Id != null && filter.WarehouseIds.Contains(l.Location.Id.Value)
                    )
                )
                .ToList();
        }

        return batches;
    }

    public async Task<Result<List<MaterialDto>>> GetMaterialsNotLinkedToSpec(MaterialKind kind)
    {
        var linkedMaterialIds = await context
            .MaterialSpecifications.Select(ms => ms.MaterialId)
            .ToListAsync();

        var materials = await context
            .Materials.Where(m => !linkedMaterialIds.Contains(m.Id) && m.Kind == kind)
            .ToListAsync();

        return Result.Success(mapper.Map<List<MaterialDto>>(materials));
    }

    public async Task<Result<Paginateable<IEnumerable<MaterialRejectDto>>>> GetMaterialRejected(
        int page,
        int pageSize,
        string searchQuery,
        MaterialKind? kind
    )
    {
        var query = context
            .MaterialRejects.AsSplitQuery()
            .Include(m => m.MaterialBatch)
                .ThenInclude(m => m.Material)
            .Include(m => m.Response)
            .OrderByDescending(m => m.CreatedAt)
            .AsQueryable();

        if (kind.HasValue)
        {
            query = query.Where(q => q.MaterialBatch.Material.Kind == kind.Value);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                q => q.MaterialBatch.BatchNumber,
                q => q.MaterialBatch.Material.Name,
                q => q.MaterialBatch.Material.Description,
                q => q.MaterialBatch.Material.Code
            );
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialRejectDto>
        );
    }

    public async Task<Result> ImportMaterialStockFromExcel(IFormFile file)
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

        if (worksheet.Dimension == null)
            return UploadErrors.EmptyFile;

        // --- HEADERS ---
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            var header = worksheet.Cells[1, col].Text.Trim();
            if (!string.IsNullOrEmpty(header))
                headers[header] = col;
        }

        string GetCell(int row, string h) =>
            headers.TryGetValue(h, out var col) ? worksheet.Cells[row, col].Text.Trim() : null;

        // --- SCAN ---
        var excelBatchNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excelShelfCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excelUomSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var excelMaterialCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            excelBatchNumbers.Add(GetCell(row, "Batch No.") ?? string.Empty);

            var s = GetCell(row, "Shelves");
            var u = GetCell(row, "UOM");
            var m = GetCell(row, "Material Code");

            if (!string.IsNullOrEmpty(s))
                excelShelfCodes.Add(s);
            if (!string.IsNullOrEmpty(u))
                excelUomSymbols.Add(u);
            if (!string.IsNullOrEmpty(m))
                excelMaterialCodes.Add(m);
        }

        // --- LOOKUPS ---
        var uoms = await context
            .UnitOfMeasures.Where(u => excelUomSymbols.Contains(u.Symbol))
            .ToListAsync();

        var uomLookup = uoms.ToDictionary(
            u => u.Symbol.Trim(),
            u => u.Id,
            StringComparer.OrdinalIgnoreCase
        );

        var shelfLookup = await context
            .WarehouseLocationShelves.Where(s => excelShelfCodes.Contains(s.Code))
            .Select(s => new
            {
                ShelfId = s.Id,
                Key = s.WarehouseLocationRack.WarehouseLocation.Warehouse.Name + "|" + s.Code,
            })
            .ToDictionaryAsync(x => x.Key, x => x.ShelfId, StringComparer.OrdinalIgnoreCase);

        var materials = await context
            .Materials.Where(m => excelMaterialCodes.Contains(m.Code))
            .ToDictionaryAsync(m => m.Code.Trim(), m => m, StringComparer.OrdinalIgnoreCase);

        var batches = await context
            .MaterialBatches.Include(b => b.Material)
            .Where(b =>
                (
                    excelBatchNumbers.Contains(b.BatchNumber)
                    || (b.BatchNumber == null && excelBatchNumbers.Contains(""))
                ) && excelMaterialCodes.Contains(b.Material.Code)
            )
            .ToListAsync();
        var batchLookup = batches.ToDictionary(
            b => (b.MaterialId, (b.BatchNumber ?? "").Trim().ToUpperInvariant()),
            b => b
        );

        // --- AGGREGATION ---
        var aggregation =
            new Dictionary<(Guid ShelfId, Guid BatchId), (decimal Qty, Guid? UomId, string Note)>();
        var newBatches = new List<MaterialBatch>();
        var binCards = new List<BinCardInformation>();
        var materialUomConsistency = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        );

        for (var row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var warehouse = GetCell(row, "Warehouse");
            var shelfCode = GetCell(row, "Shelves");
            var materialCode = GetCell(row, "Material Code");
            var batchNoRaw = GetCell(row, "Batch No.") ?? "";
            var batchNo = batchNoRaw.ToUpperInvariant();
            var uomSymbol = GetCell(row, "UOM");
            var waybill = GetCell(row, "Waybill");
            var arNo = GetCell(row, "AR No.");
            var expiryDateStr = GetCell(row, "Expiry Date");
            var mfgDateStr = GetCell(row, "Manufacturing Date");

            var qty = decimal.TryParse(GetCell(row, "Quantity"), out var q) ? q : 0;

            if (string.IsNullOrEmpty(shelfCode))
                continue;

            if (!materials.TryGetValue(materialCode, out var material))
                return Error.NotFound(
                    "Material",
                    $"Row {row}: Material '{materialCode}' not found."
                );

            // --- UOM CONSISTENCY ---
            if (!string.IsNullOrEmpty(uomSymbol))
            {
                if (materialUomConsistency.TryGetValue(materialCode, out var existingUom))
                {
                    if (!existingUom.Equals(uomSymbol, StringComparison.OrdinalIgnoreCase))
                        return Error.Validation(
                            "Material.Consistency",
                            $"Row {row}: Material '{materialCode}' has inconsistent UOM."
                        );
                }
                else
                {
                    materialUomConsistency[materialCode] = uomSymbol;
                }
            }

            var batchKey = (material.Id, batchNo);

            if (!batchLookup.TryGetValue(batchKey, out var batch))
            {
                batch = new MaterialBatch
                {
                    Id = Guid.NewGuid(),
                    MaterialId = material.Id,
                    BatchNumber = batchNo,
                    TotalQuantity = 0,
                    Status = BatchStatus.Available,
                    DateReceived = DateTime.UtcNow,
                    ExpiryDate = ParseDate(expiryDateStr),
                    ManufacturingDate = ParseDate(mfgDateStr),
                };

                batchLookup[batchKey] = batch;
                newBatches.Add(batch);
            }
            else
            {
                if (batch.ExpiryDate != ParseDate(expiryDateStr))
                    return Error.Validation("Batch.Consistency", $"Row {row}: Expiry mismatch.");

                if (batch.ManufacturingDate != ParseDate(mfgDateStr))
                    return Error.Validation("Batch.Consistency", $"Row {row}: MFG mismatch.");
            }

            var shelfKey = $"{warehouse}|{shelfCode}";
            if (!shelfLookup.TryGetValue(shelfKey, out var shelfId))
                return Error.NotFound("Shelf", $"Row {row}: Shelf '{shelfKey}' not found.");

            var uomId =
                uomSymbol != null && uomLookup.TryGetValue(uomSymbol, out var u) ? u : (Guid?)null;

            batch.TotalQuantity += qty;

            var key = (shelfId, batch.Id);

            if (aggregation.TryGetValue(key, out var existing))
            {
                aggregation[key] = (existing.Qty + qty, existing.UomId ?? uomId, existing.Note);
            }
            else
            {
                aggregation[key] = (qty, uomId, $"Imported via Excel. Waybill: {waybill}");
            }

            binCards.Add(
                new BinCardInformation
                {
                    Id = Guid.NewGuid(),
                    MaterialBatchId = batch.Id,
                    WarehouseId = Guid.Empty, // set properly if needed
                    UoMId = uomId,
                    WayBill = waybill,
                    ArNumber = arNo,
                    QuantityReceived = qty,
                    QuantityIssued = 0,
                    BalanceQuantity = qty,
                    Description = $"Stock Import - Batch: {batchNo}",
                    CreatedAt = DateTime.UtcNow,
                }
            );
        }

        // --- UPSERT ---
        var existingShelfBatches = await context
            .ShelfMaterialBatches.Where(x => x.DeletedAt == null)
            .ToListAsync();

        var inserts = new List<ShelfMaterialBatch>();

        foreach (var kv in aggregation)
        {
            var (shelfId, batchId) = kv.Key;
            var (qty, uomId, note) = kv.Value;

            var existing = existingShelfBatches.FirstOrDefault(x =>
                x.WarehouseLocationShelfId == shelfId && x.MaterialBatchId == batchId
            );

            if (existing != null)
            {
                existing.Quantity += qty;
                existing.UoMId ??= uomId;
                existing.Note = note;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                inserts.Add(
                    new ShelfMaterialBatch
                    {
                        Id = Guid.NewGuid(),
                        WarehouseLocationShelfId = shelfId,
                        MaterialBatchId = batchId,
                        Quantity = qty,
                        UoMId = uomId,
                        Note = note,
                    }
                );
            }
        }

        if (newBatches.Count > 0)
            await context.MaterialBatches.AddRangeAsync(newBatches);

        if (inserts.Count > 0)
            await context.ShelfMaterialBatches.AddRangeAsync(inserts);

        await context.BinCardInformation.AddRangeAsync(binCards);

        await context.SaveChangesAsync();

        return Result.Success();

        // --- HELPERS ---
        DateTime? ParseDate(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

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
                return DateTime.SpecifyKind(d, DateTimeKind.Utc);
            }

            return null;
        }
    }

    public async Task<Result<byte[]>> ExportMaterialStockToExcel(
        Guid userId,
        Guid? departmentId,
        MaterialKind? kind
    )
    {
        var user = await context
            .Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return UserErrors.NotFound(userId);

        departmentId ??= user.DepartmentId;

        var warehouses = await context
            .Warehouses.Where(w =>
                (!departmentId.HasValue || w.DepartmentId == departmentId)
                && w.Type != WarehouseType.Production
            )
            .ToListAsync();

        if (kind.HasValue)
        {
            warehouses =
                kind == MaterialKind.Raw
                    ? warehouses.Where(w => w.Type != WarehouseType.PackagedStorage).ToList()
                    : warehouses.Where(w => w.Type != WarehouseType.RawMaterialStorage).ToList();
        }

        var materialDepartments = await context
            .MaterialDepartments.AsSplitQuery()
            .Include(md => md.Material)
            .Include(md => md.UoM)
            .Where(md =>
                (!departmentId.HasValue || md.DepartmentId == departmentId)
                && (!kind.HasValue || md.Material.Kind == kind)
            )
            .ToListAsync();

        ExcelPackage.License.SetNonCommercialPersonal("Oryx");
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Material Stock");

        string[] headers =
        {
            "Warehouse",
            "Shelves",
            "Batch No.",
            "UOM",
            "Material Code",
            "Material Name",
            "Waybill",
            "AR No.",
            "Expiry Date",
            "Manufacturing Date",
            "Quantity",
        };
        for (var i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var warehouse in warehouses)
        {
            var deptMaterials = materialDepartments.Where(md =>
                md.DepartmentId == warehouse.DepartmentId
            );
            foreach (var md in deptMaterials)
            {
                worksheet.Cells[row, 1].Value = warehouse.Name;
                worksheet.Cells[row, 4].Value = md.UoM?.Symbol;
                worksheet.Cells[row, 5].Value = md.Material?.Code;
                worksheet.Cells[row, 6].Value = md.Material?.Name;
                row++;
            }
        }

        worksheet.Cells.AutoFitColumns();

        return Result.Success(await package.GetAsByteArrayAsync());
    }

    public async Task<Result<MaterialBatchCountDto>> GetMaterialBatchCount(
        Guid warehouseId,
        Guid? materialId,
        MaterialKind? materialKind,
        Guid? departmentId
    )
    {
        var exists = await context.Warehouses.AnyAsync(w => w.Id == warehouseId);

        if (!exists)
            return Error.NotFound("Warehouse.NotFound", "Warehouse not found.");

        var query = context
            .MaterialBatchReservedQuantities.Include(r => r.Warehouse)
            .Where(r => r.WarehouseId == warehouseId)
            .AsQueryable();

        if (materialId.HasValue)
        {
            query = query.Where(r => r.MaterialBatch.Material.Id == materialId.Value);
        }

        if (materialKind.HasValue)
        {
            query = query.Where(r => r.MaterialBatch.Material.Kind == materialKind.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(r => r.Warehouse.DepartmentId == departmentId.Value);
        }

        var distinctBatchCount = await query.Select(r => r.MaterialBatchId).Distinct().CountAsync();

        var result = new MaterialBatchCountDto
        {
            WarehouseId = warehouseId,
            BatchCount = distinctBatchCount,
        };

        return Result.Success(result);
    }
}
