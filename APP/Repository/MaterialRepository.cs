using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Departments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
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
        var material = await context.Materials
            .Include(m => m.MaterialCategory)  // Include category if needed
            .FirstOrDefaultAsync(m => m.Id == materialId);

        return material is null
            ? MaterialErrors.NotFound(materialId)
            : mapper.Map<MaterialDto>(material);
    }

    // Get paginated list of Materials
    public async Task<Result<Paginateable<IEnumerable<MaterialDto>>>> GetMaterials(int page, int pageSize, string searchQuery, MaterialKind kind)
    {
        var query = context.Materials
            .AsSplitQuery()
            .Include(m => m.MaterialCategory)
            .Where(m => m.Kind == kind)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Name, m => m.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialDto>
        );
    }
    
    public async Task<Result<List<MaterialCategoryDto>>> GetMaterialCategories(MaterialKind? materialKind)
    {
        var materialCategories = materialKind != null
            ? await context.MaterialCategories.Where(m => m.MaterialKind == materialKind)
                .ToListAsync()
            : await context.MaterialCategories
                .ToListAsync();
        return mapper.Map<List<MaterialCategoryDto>>(materialCategories);
    }
    
    public async Task<Result<List<MaterialDto>>> GetMaterials()
    {
        return mapper.Map<List<MaterialDto>>(await context.Materials
            .AsSplitQuery()
            .Include(m => m.MaterialCategory)
            .ToListAsync());
    }

    // Update Material
    public async Task<Result> UpdateMaterial(CreateMaterialRequest request, Guid materialId, Guid userId)
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
    public async Task<Result> CreateMaterialBatch(List<CreateMaterialBatchRequest> request, Guid userId)
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
            var initialLocationId = request.FirstOrDefault(r => r.MaterialId == batch.MaterialId)?.MaterialId;
        
            if (initialLocationId.HasValue)
            {
                var movement = new MassMaterialBatchMovement
                {
                    BatchId = batch.Id,
                    ToWarehouseId = initialLocationId.Value,
                    Quantity = batch.TotalQuantity, // All material moved to the initial location
                    MovedAt = DateTime.UtcNow,
                    MovedById = userId,
                    MovementType = MovementType.ToWarehouse
                };

                // Add the movement entry to the context
                await context.MassMaterialBatchMovements.AddAsync(movement);
            }
        }

        // Save changes to the database
        await context.SaveChangesAsync();

        return Result.Success();
    }
    
    
    public async Task<Result> CreateMaterialBatchWithoutBatchMovement(List<CreateMaterialBatchRequest> request, Guid userId)
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

        // Save changes to the database
        await context.SaveChangesAsync();

        return Result.Success();
    }


    // Get Material Batch by ID
    public async Task<Result<MaterialBatchDto>> GetMaterialBatch(Guid batchId)
    {
        var batch = await context.MaterialBatches
            .Include(b => b.Material)
            .Include(b => b.Events).ThenInclude(m => m.User)
            .Include(b => b.Events).ThenInclude(m => m.ConsumptionWarehouse)
            .Include(b => b.MassMovements).ThenInclude(m => m.FromWarehouse)
            .Include(b => b.MassMovements).ThenInclude(m => m.ToWarehouse)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch is null) return MaterialErrors.NotFound(batchId); 
        
        var batchDto = mapper.Map<MaterialBatchDto>(batch);
        batchDto.Locations = GetCurrentLocations(batchDto);
        return batchDto;
    }

    // Get paginated list of Material Batches
    public async Task<Result<Paginateable<IEnumerable<MaterialBatchDto>>>> GetMaterialBatches(int page, int pageSize, string searchQuery)
    {
        var query = context.MaterialBatches
            .Include(b => b.Material)
            .Include(b => b.Events).ThenInclude(m => m.User)
            .Include(b => b.Events).ThenInclude(m => m.ConsumptionWarehouse)
            .Include(b => b.MassMovements).ThenInclude(m => m.FromWarehouse)
            .Include(b => b.MassMovements).ThenInclude(m => m.ToWarehouse)
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
    
    public async Task<Result<List<MaterialBatchDto>>> GetMaterialBatchesByMaterialId(Guid materialId)
    {
        var query =  await context.MaterialBatches
            .Include(b => b.Material)
            .Include(b => b.Events).ThenInclude(m => m.User)
            .Include(b => b.Events).ThenInclude(m => m.ConsumptionWarehouse)
            .Include(b => b.MassMovements).ThenInclude(m => m.FromWarehouse)
            .Include(b => b.MassMovements).ThenInclude(m => m.ToWarehouse)
            .Where(b => b.MaterialId == materialId)
            .ToListAsync();

        var batches = mapper.Map<List<MaterialBatchDto>>(query);

        foreach (var batch in batches)
        {
            batch.Locations = GetCurrentLocations(batch);
        }

        return batches;
    }

    public async Task<Result<Paginateable<IEnumerable<MaterialDetailsDto>>>> GetApprovedMaterials(int page, int pageSize, string searchQuery, MaterialKind kind, Guid userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var warehouse =  kind == MaterialKind.Raw ?  user.GetUserRawWarehouse() : user.GetUserPackagingWarehouse();

        if (warehouse is null)
            return UserErrors.WarehouseNotFound(MaterialKind.Raw);
        
        var query = context.ShelfMaterialBatches
            .AsSplitQuery()
            .Include(m => m.MaterialBatch)
            .ThenInclude(mb => mb.Material)
            .Include(m => m.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation)
            .Where(m => m.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == warehouse.Id &&
                        (m.MaterialBatch.Status == BatchStatus.Available || m.MaterialBatch.Status == BatchStatus.Frozen))
            .Select(m => m.MaterialBatch.Material)
            .Distinct()
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, m => m.Name, m => m.Description);
        }

        var paginatedResult = await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialDto>);

        var materialIds = paginatedResult.Data.Select(m => m.Id).ToList();

        //gets only those assigned to shelf locations
        var totalAvailableQuantities = await context.ShelfMaterialBatches
            .Include(m => m.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation)
            .Where(smb => materialIds.Contains(smb.MaterialBatch.MaterialId) && smb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == warehouse.Id && smb.MaterialBatch.Status == BatchStatus.Available)
            .GroupBy(smb => smb.MaterialBatch.MaterialId)
            .Select(g => new { MaterialId = g.Key, TotalQuantity = g.Sum(smb => smb.Quantity), UnitOfMeasure = g.FirstOrDefault(h => h.MaterialBatch.MaterialId == g.Key).MaterialBatch.UoM })
            .ToListAsync();
        
        var materialDetails = paginatedResult.Data.Select(m => new MaterialDetailsDto
        {
            Material = m,
            UnitOfMeasure = mapper.Map<UnitOfMeasureDto>(totalAvailableQuantities.FirstOrDefault(q => q.MaterialId == m.Id)?.UnitOfMeasure),
            TotalAvailableQuantity = totalAvailableQuantities.FirstOrDefault(q => q.MaterialId == m.Id)?.TotalQuantity ?? 0
        }).ToList();

        var result = new Paginateable<IEnumerable<MaterialDetailsDto>>
        {
            Data = materialDetails,
            PageIndex = paginatedResult.PageIndex,
            PageCount = paginatedResult.PageCount,
            TotalRecordCount = paginatedResult.TotalRecordCount,
            StartPageIndex = paginatedResult.StartPageIndex,
            NumberOfPagesToShow = paginatedResult.NumberOfPagesToShow,
            StopPageIndex = paginatedResult.StopPageIndex
        };

        return Result.Success(result);
    }

    public async Task<Result<Paginateable<IEnumerable<ShelfMaterialBatchDto>>>> GetMaterialBatchesByMaterialIdV2(int page, int pageSize, Guid materialId, Guid userId)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
        if (material is null)
            return MaterialErrors.NotFound(materialId);

        var warehouse = material.Kind == MaterialKind.Raw ? user.GetUserRawWarehouse() : user.GetUserPackagingWarehouse();

        if (warehouse is null)
            return UserErrors.WarehouseNotFound(material.Kind);
        
        var query = context.ShelfMaterialBatches
            .AsSplitQuery()
            .Include(m=>m.WarehouseLocationShelf)
            .Include(m => m.MaterialBatch)
            .ThenInclude(mb=>mb.Checklist)
            .ThenInclude(cl=>cl.Manufacturer)
            .Where(m => m.MaterialBatch.MaterialId == materialId
                        && m.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Id == warehouse.Id && m.MaterialBatch.Status==BatchStatus.Available)
            .OrderBy(m=>m.MaterialBatch.ExpiryDate)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ShelfMaterialBatchDto>);
            
    }

    public async Task<Result<decimal>> GetMaterialsInTransit(Guid materialId)
    {
        return await context.ShipmentInvoiceItems
            .Where(s => s.MaterialId == materialId 
                        && context.ShipmentDocuments
                            .Any(sd => sd.ShipmentInvoiceId == s.ShipmentInvoiceId && sd.ArrivedAt == null)) // Check if linked document hasn't arrived
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
        return locationQuantities.Select(kvp => new CurrentLocationDto
        {
            Location = kvp.Key,
            QuantityAtLocation = kvp.Value
        }).ToList();
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
        return locationQuantities.Select(kvp => new CurrentLocation
        {
            Location = kvp.Key,
            QuantityAtLocation = kvp.Value
        }).ToList();
    }

    // Update Material Batch
    public async Task<Result> UpdateMaterialBatch(CreateMaterialBatchRequest request, Guid batchId, Guid userId)
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

        var totalStock = await context.MaterialBatches
            .Where(b => b.MaterialId == materialId && b.Status == BatchStatus.Available)
            .SumAsync(b => b.TotalQuantity - b.ConsumedQuantity);

        return totalStock;
    }
    
    public async Task<Result> MoveMaterialBatchByMaterial(MoveMaterialBatchRequest request, Guid userId)
    {
        var material = await context.Materials
            .Include(m => m.Batches) // Include batches for detailed processing
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
            var batchStockAtFromWarehouse = await GetBatchStockInLocation(batch.Id, request.FromWarehouseId);

            if (batchStockAtFromWarehouse <= 0)
            {
                continue; // Skip batches with no stock at this location
            }

            // Determine how much to move from this batch
            var quantityToMoveFromBatch = Math.Min(remainingQuantityToMove, batchStockAtFromWarehouse);

            // Create a movement entry for the batch
            var movement = new MassMaterialBatchMovement
            {
                BatchId = batch.Id,
                FromWarehouseId = request.FromWarehouseId,
                ToWarehouseId = request.ToWarehouseId,
                Quantity = quantityToMoveFromBatch,
                MovedAt = DateTime.UtcNow,
                MovedById = userId,
                MovementType = MovementType.BetweenLocations
            };

            await context.MassMaterialBatchMovements.AddAsync(movement);

            // Create a corresponding event for the move
            var batchEvent = new MaterialBatchEvent
            {
                BatchId = batch.Id,
                Quantity = quantityToMoveFromBatch,
                Type = EventType.Moved,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
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
        var materialBatch = await context.MaterialBatches.FirstOrDefaultAsync(mb => mb.Id == batchId);
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
        var quantityMovedToWarehouse = await context.MassMaterialBatchMovements
            .Where(m => m.BatchId == batchId && m.ToWarehouseId == locationId)
            .SumAsync(m => m.Quantity);

        // Sum of quantities moved out of this location for the specific batch
        var quantityMovedOutOfLocation = await context.MassMaterialBatchMovements
            .Where(m => m.BatchId == batchId && m.FromWarehouseId == locationId)
            .SumAsync(m => m.Quantity);

        // Sum of quantities consumed at this location for the specific batch
        var quantityConsumedAtLocation = await context.MaterialBatchEvents
            .Where(e => e.BatchId == batchId 
                        && e.ConsumptionWarehouseId == locationId 
                        && e.Type == EventType.Consumed)
            .SumAsync(e => e.Quantity);

        // Calculate the total available quantity for the batch in this location
        var totalBatchStockInLocation = quantityMovedToWarehouse - quantityMovedOutOfLocation - quantityConsumedAtLocation;

        return totalBatchStockInLocation;
    }

    public async Task<Result> MoveMaterialBatch(Guid batchId, Guid fromLocationId, Guid toLocationId, decimal quantity, Guid userId)
    {
        var batch = await context.MaterialBatches
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch is null)
        {
            return MaterialErrors.NotFound(batchId);
        }

        // Get the current stock at the source location using the existing method
        var currentStockAtFromWarehouse = await GetMassMaterialStockInWarehouse(batch.MaterialId, fromLocationId);

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
            MovementType = MovementType.BetweenLocations // Regular movement
        };

        // Add the movement entry to the context
        await context.MassMaterialBatchMovements.AddAsync(movement);

        // Create the corresponding event for the move
        var batchEvent = new MaterialBatchEvent
        {
            BatchId = batchId,
            Quantity = quantity,
            Type = EventType.Moved,  // Reflecting the movement event
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        // Add the event entry to the context
        await context.MaterialBatchEvents.AddAsync(batchEvent);

        // Save changes to the database
        await context.SaveChangesAsync();

        return Result.Success();
    }
    
    public async Task<Result> MoveMaterialBatchV2(MoveShelfMaterialBatchRequest request, Guid userId)
    {
        var shelfMaterialBatch = await context.ShelfMaterialBatches
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
            

            var newShelfMaterialBatch = new ShelfMaterialBatch
            {
                WarehouseLocationShelfId = movedBatch.WarehouseLocationShelfId,
                MaterialBatchId = shelfMaterialBatch.MaterialBatchId,
                Quantity = movedBatch.Quantity,
                UoM = await context.UnitOfMeasures.FindAsync(movedBatch.UomId),
                Note = movedBatch.Note,
                CreatedAt = DateTime.UtcNow
            };

            await context.ShelfMaterialBatches.AddAsync(newShelfMaterialBatch);

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
                CreatedAt = DateTime.UtcNow
            };

            await context.MaterialBatchEvents.AddAsync(batchEvent);
        }

        // Save changes to the database
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> SupplyMaterialBatchToWarehouse(SupplyMaterialBatchRequest request, Guid userId)
    {
        var materialBatch = await context.MaterialBatches
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

        foreach (var shelfBatch in request.ShelfMaterialBatches)
        {
            var shelf = await context.WarehouseLocationShelves
                .Include(warehouseLocationShelf => warehouseLocationShelf.WarehouseLocationRack)
                .ThenInclude(warehouseLocationRack => warehouseLocationRack.WarehouseLocation)
                .ThenInclude(warehouseLocation => warehouseLocation.Warehouse)
                .FirstOrDefaultAsync(s => s.Id == shelfBatch.WarehouseLocationShelfId);

            if (shelf == null)
            {
                return Error.NotFound("Shelf.NotFound", $"Shelf with ID {shelfBatch.WarehouseLocationShelfId} not found");
            }

            var shelfMaterialBatch = mapper.Map<ShelfMaterialBatch>(shelfBatch);
            shelfMaterialBatch.MaterialBatchId = request.MaterialBatchId;

            await context.ShelfMaterialBatches.AddAsync(shelfMaterialBatch);
            
            var movement = new MassMaterialBatchMovement
            {
                BatchId = request.MaterialBatchId,
                ToWarehouseId = shelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Id,
                Quantity = shelfBatch.Quantity,
                MovedAt = DateTime.UtcNow,
                MovedById = userId,
                MovementType = MovementType.ToWarehouse
            };
            
            await context.MassMaterialBatchMovements.AddAsync(movement);
            
            var batchEvent = new MaterialBatchEvent
            {
                BatchId = request.MaterialBatchId,
                Quantity = shelfBatch.Quantity,
                Type = EventType.Supplied, 
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            
            await context.MaterialBatchEvents.AddAsync(batchEvent);
        }

        var warehouse = await context.Warehouses
            .FirstOrDefaultAsync(w => w.Id == context.WarehouseLocationShelves
                .FirstOrDefault(s => s.Id == request.ShelfMaterialBatches.First().WarehouseLocationShelfId)
                .WarehouseLocationRack.WarehouseLocation.Warehouse.Id);
        
        var binCardEvent = new BinCardInformation
        {
            MaterialBatchId = materialBatch.Id,
            Description = warehouse.Name,
            WayBill = "N/A",
            ArNumber = "N/A",
            QuantityReceived = totalQuantityToAssign,
            QuantityIssued = 0,
            BalanceQuantity = (await GetMaterialStockInWarehouseByBatch(materialBatch.Id, warehouse.Id)).Value + totalQuantityToAssign,
            UoMId = materialBatch.UoMId,
            CreatedAt = DateTime.UtcNow
        };
        
        await context.BinCardInformation.AddAsync(binCardEvent);

        materialBatch.QuantityAssigned += totalQuantityToAssign;
        
        if(materialBatch.QuantityAssigned >= materialBatch.TotalQuantity)
        {
            materialBatch.Status = BatchStatus.Available;
        }

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
            var stockResult = await GetMaterialStockInWarehouse(materialId, warehouse.Id);
            if (stockResult.IsSuccess && stockResult.Value > 0)
            {
                stockByWarehouse.Add(new MaterialStockByWarehouseDto
                {
                    Warehouse = mapper.Map<CollectionItemDto>(warehouse),
                    TotalQuantity = stockResult.Value
                });
            }
        }

        return stockByWarehouse;
    }


    public async Task<Result<List<DepartmentDto>>> GetDepartmentsWithEnoughStock(Guid materialId, decimal quantity)
    {
        var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
        if (material is null)
            return MaterialErrors.NotFound(materialId);

        var warehouseType = material.Kind == MaterialKind.Raw
            ? WarehouseType.RawMaterialStorage
            : WarehouseType.PackagedStorage;
        
        var departments = await context.Departments
            .ToListAsync();

        var result = new List<DepartmentDto>();
        
        foreach (var department in departments)
        {
            // Get all warehouses associated with this department
            var warehouse = await context.Warehouses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(dw => dw.DepartmentId == department.Id && dw.Type == warehouseType);

            if (warehouse is null)
            {
                continue;
            }
            
            decimal totalStock = 0;
            
            var stockResult = await GetMaterialStockInWarehouse(materialId, warehouse.Id);
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
            var warehouseIds = await context.Warehouses
                .Where(dw => dw.DepartmentId == department.Id)
                .Select(dw => dw.Id)
                .ToListAsync();

            decimal totalStock = 0;

            foreach (var warehouseId in warehouseIds)
            {
                var stockResult = await GetMaterialStockInWarehouse(materialId, warehouseId);
                if (stockResult.IsSuccess)
                {
                    totalStock += stockResult.Value;
                }
            }

            if (totalStock > 0)
            {
                stockByDepartment.Add(new MaterialStockByDepartmentDto
                {
                    Department = mapper.Map<CollectionItemDto>(department),
                    TotalQuantity = totalStock
                });
            }
        }

        return stockByDepartment;
    }
    
    public async Task<Result<decimal>> GetMaterialStockInWarehouse(Guid materialId, Guid warehouseId)
    {
        // Sum of quantities moved to this location (incoming batches)
        var batchesInLocation = await context.MassMaterialBatchMovements
            .Include(m => m.Batch)
            .Include(m => m.ToWarehouse)
            .Where(m => m.Batch.MaterialId == materialId
                        && m.ToWarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);
    
        // Sum of quantities moved out of this location (outgoing batches)
        var batchesMovedOut = await context.MassMaterialBatchMovements
            .Include(m => m.Batch)
            .Include(m => m.FromWarehouse)
            .Where(m => m.Batch.MaterialId == materialId
                        && m.FromWarehouse != null && m.FromWarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);
    
        // Sum of the consumed quantities at this location for the given material
        var batchesConsumedAtLocation = await context.MaterialBatchEvents
            .Include(m => m.Batch)
            .Include(m => m.ConsumptionWarehouse)
            .Where(e => e.Batch.MaterialId == materialId
                        && e.ConsumptionWarehouse != null 
                        && e.ConsumptionWarehouseId == warehouseId
                        && e.Type == EventType.Consumed)
            .SumAsync(e => e.Quantity);

        // Calculate the total available quantity for the material in this location
        // Calculate the total available quantity for the material in this location
        var totalQuantityInLocation = batchesInLocation - batchesMovedOut - batchesConsumedAtLocation;

        return Math.Max(totalQuantityInLocation, 0);
    }
    
    public async Task<Result<decimal>> GetMaterialStockInWarehouseByBatch(Guid batchId, Guid warehouseId)
    {
        // Sum of quantities moved to this location (incoming batches)
        var batchesInLocation = await context.MassMaterialBatchMovements
            .Include(m => m.Batch)
            .Include(m => m.ToWarehouse)
            .Where(m => m.BatchId == batchId
                        && m.ToWarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);
    
        // Sum of quantities moved out of this location (outgoing batches)
        var batchesMovedOut = await context.MassMaterialBatchMovements
            .Include(m => m.Batch)
            .Include(m => m.FromWarehouse)
            .Where(m => m.BatchId == batchId
                        && m.FromWarehouse != null && m.FromWarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);
    
        // Sum of the consumed quantities at this location for the given material
        var batchesConsumedAtLocation = await context.MaterialBatchEvents
            .Include(m => m.Batch)
            .Include(m => m.ConsumptionWarehouse)
            .Where(e => e.BatchId == batchId
                        && e.ConsumptionWarehouse != null 
                        && e.ConsumptionWarehouseId == warehouseId
                        && e.Type == EventType.Consumed)
            .SumAsync(e => e.Quantity);

        // Calculate the total available quantity for the material in this location
        var totalQuantityInLocation = batchesInLocation - batchesMovedOut - batchesConsumedAtLocation;

        return totalQuantityInLocation;
    }
    
    public async Task<Result<decimal>> GetProductStockInWarehouseByBatch(Guid batchId, Guid warehouseId)
    {
        // Sum of quantities moved to this location (incoming batches)
        var batchesInLocation = await context.FinishedProductBatchMovements
            .Include(m => m.Batch)
            .Include(m => m.ToWarehouse)
            .Where(m => m.BatchId == batchId
                        && m.ToWarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);
    
        // Sum of quantities moved out of this location (outgoing batches)
        var batchesMovedOut = await context.FinishedProductBatchMovements
            .Include(m => m.Batch)
            .Include(m => m.FromWarehouse)
            .Where(m => m.BatchId == batchId
                        && m.FromWarehouse != null && m.FromWarehouseId == warehouseId)
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
        var totalQuantityInLocation = batchesInLocation - batchesMovedOut;// - batchesConsumedAtLocation;

        return totalQuantityInLocation;
    }
    public async Task<Result<decimal>> GetMassMaterialStockInWarehouse(Guid materialId, Guid warehouseId)
    {
        // Sum of quantities moved to this location (incoming batches)
        var batchesInLocation = await context.MassMaterialBatchMovements
            .Include(m => m.Batch)
            .Include(m => m.ToWarehouse)
            .Where(m => m.Batch.MaterialId == materialId
                        && m.ToWarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);
    
        // Sum of quantities moved out of this location (outgoing batches)
        var batchesMovedOut = await context.MassMaterialBatchMovements
            .Include(m => m.Batch)
            .Include(m => m.FromWarehouse)
            .Where(m => m.Batch.MaterialId == materialId
                        && m.FromWarehouse != null && m.FromWarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);
    
        // Sum of the consumed quantities at this location for the given material
        var batchesConsumedAtLocation = await context.MaterialBatchEvents
            .Include(m => m.Batch)
            .Include(m => m.ConsumptionWarehouse)
            .Where(e => e.Batch.MaterialId == materialId
                        && e.ConsumptionWarehouse != null 
                        && e.ConsumptionWarehouseId == warehouseId
                        && e.Type == EventType.Consumed)
            .SumAsync(e => e.Quantity);

        // Calculate the total available quantity for the material in this location
        var totalQuantityInLocation = batchesInLocation - batchesMovedOut - batchesConsumedAtLocation;

        return Math.Max(totalQuantityInLocation, 0);
    }
    
    public async Task<Result<decimal>> GetFrozenMaterialStockInWarehouse(Guid materialId, Guid warehouseId)
    {
        // Sum of quantities moved to this location (incoming batches)
        var batchesInLocation = await context.MassMaterialBatchMovements
            .IgnoreQueryFilters()
            .Include(m => m.Batch)
            .Include(m => m.ToWarehouse)
            .Where(m => m.Batch.Status==BatchStatus.Frozen && m.Batch.MaterialId == materialId
                                                           && m.ToWarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);
    
        // Sum of quantities moved out of this location (outgoing batches)
        var batchesMovedOut = await context.MassMaterialBatchMovements
            .IgnoreQueryFilters()
            .Include(m => m.Batch)
            .Include(m => m.FromWarehouse)
            .Where(m =>  m.Batch.Status==BatchStatus.Frozen && m.Batch.MaterialId == materialId
                                                            && m.FromWarehouse != null && m.FromWarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);
    
        // Sum of the consumed quantities at this location for the given material
        var batchesConsumedAtLocation = await context.MaterialBatchEvents
            .IgnoreQueryFilters()
            .Include(m => m.Batch)
            .Include(m => m.ConsumptionWarehouse)
            .Where(e =>  e.Batch.Status==BatchStatus.Frozen && e.Batch.MaterialId == materialId
                                          && e.ConsumptionWarehouse != null 
                                          && e.ConsumptionWarehouseId == warehouseId
                                          && e.Type == EventType.Consumed)
            .SumAsync(e => e.Quantity);

        // Calculate the total available quantity for the material in this location
        var totalQuantityInLocation = batchesInLocation - batchesMovedOut - batchesConsumedAtLocation;

        return totalQuantityInLocation;
    }
    
    
    public async Task<Result<List<MaterialBatchDto>>> GetFrozenMaterialBatchesInWarehouse(Guid materialId, Guid warehouseId)
    {
        var frozenBatches = await context.MaterialBatches
            .IgnoreQueryFilters()
            .Include(b => b.Material)
            .Include(b => b.UoM)
            .Where(b => b.Status==BatchStatus.Frozen && b.MaterialId == materialId &&
                        context.MassMaterialBatchMovements.Any(m => m.BatchId == b.Id && m.ToWarehouseId == warehouseId) &&
                        !context.MassMaterialBatchMovements.Any(m => m.BatchId == b.Id && m.FromWarehouseId == warehouseId))
            .ToListAsync();

        return mapper.Map<List<MaterialBatchDto>>(frozenBatches);
    }
    
    
    public async Task<Result<List<BatchToSupply>>> GetFrozenBatchesForRequisitionItem(Guid materialId, Guid warehouseId, decimal requestedQuantity)
    {
        var result = new List<BatchToSupply>();
        decimal remainingQuantityToFulfill = requestedQuantity;

        // Fetch frozen batches in FIFO order
        var frozenBatches = await context.MaterialBatches
            .Include(b => b.Material)
            .Include(b => b.UoM)
            .Where(b => b.Status == BatchStatus.Frozen &&
                        b.MaterialId == materialId &&
                        context.MassMaterialBatchMovements.Any(m => m.BatchId == b.Id && m.ToWarehouseId == warehouseId) &&
                        !context.MassMaterialBatchMovements.Any(m => m.BatchId == b.Id && m.FromWarehouseId == warehouseId))
            .OrderBy(b => b.ExpiryDate) // FIFO (First-In, First-Out)
            .ToListAsync();

        foreach (var batch in frozenBatches)
        {
            if (remainingQuantityToFulfill <= 0)
                break; // Stop once the required quantity is fulfilled

            // Get the available quantity in the warehouse for this batch
            var availableQuantityResult = await GetMaterialStockInWarehouseByBatch(batch.Id, warehouseId);
            if (availableQuantityResult.IsFailure)
            {
                continue;
            }

            decimal availableQuantity = availableQuantityResult.Value;
            if (availableQuantity <= 0)
                continue; // Skip batches with no stock

            // Determine how much can be taken from this batch
            decimal quantityToTake = Math.Min(availableQuantity, remainingQuantityToFulfill);

            // Add batch to the result list
            var batchDto = mapper.Map<MaterialBatchDto>(batch);
            result.Add(new BatchToSupply
            {
                Batch = batchDto,
                QuantityToTake = quantityToTake
            });

            remainingQuantityToFulfill -= quantityToTake; // Reduce the required quantity
        }

        if (remainingQuantityToFulfill > 0)
        {
            return Error.Failure("Batch.Failure", $"Not enough frozen stock available to supply {requestedQuantity}. Short by {remainingQuantityToFulfill}.");
        }

        return result;
    }
    

   public Result<List<BatchLocation>> BatchesNeededToBeConsumed(Guid materialId, Guid warehouseId, decimal quantity)
    {
        var result = new List<BatchLocation>();
        decimal remainingQuantityToFulfill = quantity;

        // Fetch batches sorted by expiry date (FIFO order)
        var batches =  context.MaterialBatches
            .Where(b => b.MaterialId == materialId &&
                        b.MassMovements.Any(m => m.ToWarehouseId == warehouseId)) // Ensure the batch is in the warehouse
            .OrderBy(b => b.ExpiryDate) // FIFO
            .Include(b => b.MassMovements)
            .ThenInclude(m => m.ToWarehouse)
            .Include(b => b.MassMovements)
            .ThenInclude(m => m.FromWarehouse)
            .AsSplitQuery()
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
                decimal quantityToConsider = Math.Min(currentLocation.QuantityAtLocation, remainingQuantityToFulfill);

                // Add batch to the result list
                result.Add(new BatchLocation
                {
                    ConsumptionLocation = mapper.Map<WarehouseDto>(currentLocation.Location),
                    Batch = mapper.Map<MaterialBatchDto>(batch),
                    QuantityToUse = quantityToConsider
                });

                remainingQuantityToFulfill -= quantityToConsider; // Reduce the required quantity
            }
        }

        if (remainingQuantityToFulfill > 0)
        {
            return Error.Failure("Batch.Failure",$"Not enough stock available to fulfill {quantity}. Short by {remainingQuantityToFulfill}.");
        }

        return result;
    }
   
    public async Task<Result<List<BatchToSupply>>> BatchesToSupplyForGivenQuantity(Guid materialId, Guid warehouseId, decimal quantity)
    {
        var result = new List<BatchToSupply>();
        decimal remainingQuantityToFulfill = quantity;

        // Fetch batches in the given warehouse, sorted by expiry date (FIFO)
        var batches = await context.MaterialBatches
            .Where(b => b.MaterialId == materialId &&
                        b.MassMovements.Any(m => m.ToWarehouseId == warehouseId)) // Only include batches in the specified warehouse
            .OrderBy(b => b.ExpiryDate) // FIFO order
            .Include(b => b.MassMovements)
            .ThenInclude(m => m.ToWarehouse)
            .Include(b => b.MassMovements)
            .ThenInclude(m => m.FromWarehouse)
            .AsSplitQuery()
            .ToListAsync();

        foreach (var batch in batches)
        {
            if (remainingQuantityToFulfill <= 0)
                break; // Stop if we've met the required quantity

            // Get the available stock for the batch in the given warehouse
            var availableQuantityResult = await GetMaterialStockInWarehouseByBatch(batch.Id, warehouseId);
            if(availableQuantityResult.IsFailure)
            {
                continue;
            }

            decimal availableQuantity = availableQuantityResult.Value;

            if (availableQuantity <= 0)
                continue; // Skip batches with no stock

            // Determine how much we can take from this batch
            decimal quantityToTake = Math.Min(availableQuantity, remainingQuantityToFulfill);

            // Map and add batch to the result list
            var batchDto = mapper.Map<MaterialBatchDto>(batch);
            result.Add(new BatchToSupply
            {
                Batch = batchDto,
                QuantityToTake = quantityToTake
            });

            remainingQuantityToFulfill -= quantityToTake; // Reduce the required quantity
        }

        if (remainingQuantityToFulfill > 0)
        {
            return Error.Failure("Batch.Failure", $"Not enough stock available to supply {quantity}. Short by {remainingQuantityToFulfill}.");
        }

        return result;
    }


    
    public async Task<Result> ConsumeMaterialAtLocation(Guid batchId, Guid locationId, decimal quantity, Guid userId)
    {
        var materialBatchEvent = new MaterialBatchEvent
        {
            BatchId = batchId,
            Quantity = quantity,
            UserId = userId,
            Type = EventType.Consumed,
            ConsumptionWarehouseId = locationId,
            ConsumedAt = DateTime.UtcNow 
        };

        // Optionally update the batch's consumed quantity
        var materialBatch = await context.MaterialBatches
            .FirstOrDefaultAsync(b => b.Id == batchId);
        
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
        var materialBatch = await context.MaterialBatches
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (materialBatch == null)
            return Error.Failure("Material.Batch", "Material batch not found.");

        materialBatch.Status = BatchStatus.Frozen;
        context.MaterialBatches.Update(materialBatch);
    
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task ReserveQuantityFromBatchForProduction(Guid batchId, Guid warehouseId, Guid productionScheduleId, Guid productId, decimal quantity)
    {
        await context.MaterialBatchReservedQuantities.AddAsync(new MaterialBatchReservedQuantity
        {
            MaterialBatchId = batchId,
            WarehouseId = warehouseId,
            ProductionScheduleId = productionScheduleId,
            ProductId = productId,
            Quantity = quantity
        });

        await context.SaveChangesAsync();
    }

    public async Task<List<MaterialBatchReservedQuantity>> GetReservedBatchesAndQuantityForProductionWarehouse(Guid materialId, Guid warehouseId, Guid productionScheduleId, Guid productId)
    {
        return 
            await context.MaterialBatchReservedQuantities
                .Include(r => r.MaterialBatch)
                .Where(r => r.MaterialBatch.MaterialId == materialId && 
                            r.WarehouseId == warehouseId && r.ProductionScheduleId == productionScheduleId && r.ProductId == productId)
                .ToListAsync();
    }

    
    public async Task<Result> ConsumeMaterialAtLocation(Material material, Guid locationId, decimal quantity, Guid userId)
    {
        var materialBatchEvents = new List<MaterialBatchEvent>();
        decimal remainingQuantityToConsume = quantity;

        foreach (var batch in material.Batches.OrderBy(b => b.ExpiryDate))
        {
            if (remainingQuantityToConsume <= 0)
                break; // Stop if we've consumed all the required quantity

            if (batch.RemainingQuantity <= 0)
                continue; // Skip batches with no remaining stock

            // Consume the minimum of what's available in the batch or the remaining needed quantity
            decimal quantityToConsumeFromThisBatch = Math.Min(batch.RemainingQuantity, remainingQuantityToConsume);

            // Create a batch event for this consumption
            var materialBatchEvent = new MaterialBatchEvent
            {
                BatchId = batch.Id,
                Quantity = quantityToConsumeFromThisBatch,
                UserId = userId,
                Type = EventType.Consumed,
                ConsumptionWarehouseId = locationId,
                ConsumedAt = DateTime.UtcNow
            };

            // Update batch quantities
            batch.ConsumedQuantity += quantityToConsumeFromThisBatch;
            remainingQuantityToConsume -= quantityToConsumeFromThisBatch;

            materialBatchEvents.Add(materialBatchEvent);
            context.MaterialBatches.Update(batch);
        }

        if (remainingQuantityToConsume > 0)
        {
            return Error.Failure("Batch.Consume", $"Not enough stock available to consume the requested quantity. Remaining: {remainingQuantityToConsume}");
        }

        // Add all batch events to the context
        await context.MaterialBatchEvents.AddRangeAsync(materialBatchEvents);

        // Save changes to the database
        await context.SaveChangesAsync();
        return Result.Success();
    }

    
    public async Task<Result<List<WarehouseStockDto>>> GetMaterialStockAcrossWarehouses(Guid materialId)
    {
        // Get all material batch movements for the given materialId, including both FromWarehouse and ToWarehouse
        var batchMovements = await context.MassMaterialBatchMovements
            .IgnoreQueryFilters()
            .Where(m => m.Batch.MaterialId == materialId)
            .Include(m => m.ToWarehouse)
            .Include(m => m.FromWarehouse)
            .ToListAsync();

        // Get all unique warehouse IDs (both from and to locations)
        var warehouseIds = batchMovements
            .SelectMany(m => new[]
            {
                m.ToWarehouseId, 
                m.FromWarehouseId
            })
            .Where(warehouseId => warehouseId.HasValue)
            .Distinct()
            .ToList();

        // List to store the WarehouseStockDto
        var warehouseStockList = new List<WarehouseStockDto>();

        // For each warehouse ID, retrieve the stock and warehouse info
        foreach (var warehouseId in warehouseIds.Where(warehouseId => warehouseId.HasValue))
        {
            var stockResult = await GetMassMaterialStockInWarehouse(materialId, warehouseId.Value);
            if (!stockResult.IsSuccess) continue;

            // Get warehouse details (you can map this from your Warehouse entity)
            var warehouse = await context.Warehouses.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
        
            if (warehouse != null)
            {
                warehouseStockList.Add(new WarehouseStockDto
                {
                    Warehouse = mapper.Map<WarehouseDto>(warehouse),
                    StockQuantity = stockResult.Value
                });
            }
        }

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

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial; 
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
            return UploadErrors.WorksheetNotFound;

        // Read headers
        var headers = new Dictionary<string, int>();
        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
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
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var categoryName = worksheet.Cells[row, headers["Category"]].Text.Trim().ToLower();
            var category = context.MaterialCategories.FirstOrDefault(m => m.Name != null && m.Name.ToLower() == categoryName);
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
                Kind = kind // Replace with your logic for Kind if needed
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

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial; 
        using var package = new ExcelPackage(new FileInfo(filePath));
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
            return UploadErrors.WorksheetNotFound;

        // Read headers
        var headers = new Dictionary<string, int>();
        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
        {
            headers[worksheet.Cells[1, col].Text.Trim()] = col;
        }

        // Validate required headers
        var requiredHeaders = new[] { "Code", "Name", "Description", "Pharmacopoeia", "Category", "MinimumStockLevel", "MaximumStockLevel" };
        foreach (var header in requiredHeaders)
        {
            if (!headers.ContainsKey(header))
                return UploadErrors.MissingRequiredHeader(header);
        }

        // Read data rows
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var categoryName = worksheet.Cells[row, headers["Category"]].Text.Trim();
            var category = await context.MaterialCategories.FirstOrDefaultAsync(m => m.Name == categoryName);

            if (category == null)
                return UploadErrors.CategoryNotFound(categoryName);

            var material = new Material
            {
                Code = worksheet.Cells[row, headers["Code"]].Text.Trim(),
                Name = worksheet.Cells[row, headers["Name"]].Text.Trim(),
                Description = worksheet.Cells[row, headers["Description"]].Text.Trim(),
                Pharmacopoeia = worksheet.Cells[row, headers["Pharmacopoeia"]].Text.Trim(),
                MaterialCategoryId = category.Id,
                MinimumStockLevel = int.TryParse(worksheet.Cells[row, headers["MinimumStockLevel"]].Text.Trim(), out var minStock) ? minStock : 0,
                MaximumStockLevel = int.TryParse(worksheet.Cells[row, headers["MaximumStockLevel"]].Text.Trim(), out var maxStock) ? maxStock : 0,
                Kind = kind
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

        var materialBatches = await context.MaterialBatches
            .Where(mb => request.MaterialBatchIds.Contains(mb.Id))
            .ToListAsync();

        if (materialBatches.Count != request.MaterialBatchIds.Count)
        {
            return Error.NotFound("MaterialBatch.NotFound", "One or more material batches not found");
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


}
