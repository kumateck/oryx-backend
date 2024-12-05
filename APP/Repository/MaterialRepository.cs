using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;

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
            var initialLocationId = request.FirstOrDefault(r => r.MaterialId == batch.MaterialId)?.InitialLocationId;
        
            if (initialLocationId.HasValue)
            {
                var movement = new MaterialBatchMovement
                {
                    BatchId = batch.Id,
                    FromLocationId = Guid.Empty,  // Assuming it's coming from a "default" location (new batch creation)
                    ToLocationId = initialLocationId.Value,
                    Quantity = batch.TotalQuantity, // All material moved to the initial location
                    MovedAt = DateTime.UtcNow,
                    MovedById = userId,
                    MovementType = MovementType.ToWarehouse
                };

                // Add the movement entry to the context
                await context.MaterialBatchMovements.AddAsync(movement);
            }
        }

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
            .Include(b => b.Events).ThenInclude(m => m.ConsumedLocation)
            .Include(b => b.Movements).ThenInclude(m => m.FromLocation)
            .Include(b => b.Movements).ThenInclude(m => m.ToLocation)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        return batch is null
            ? MaterialErrors.NotFound(batchId)
            : mapper.Map<MaterialBatchDto>(batch);
    }

    // Get paginated list of Material Batches
    public async Task<Result<Paginateable<IEnumerable<MaterialBatchDto>>>> GetMaterialBatches(int page, int pageSize, string searchQuery)
    {
        var query = context.MaterialBatches
            .Include(b => b.Material)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, b => b.Material.Name);  // Searching by material name
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialBatchDto>
        );
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
    
    public async Task<Result<int>> CheckStockLevel(Guid materialId)
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

    public async Task<Result> MoveMaterialBatch(Guid batchId, Guid fromLocationId, Guid toLocationId, int quantity, Guid userId)
    {
        var batch = await context.MaterialBatches
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch is null)
        {
            return MaterialErrors.NotFound(batchId);
        }

        // Get the current stock at the source location using the existing method
        var currentStockAtFromLocation = await GetMaterialStockInWarehouse(batch.MaterialId, fromLocationId);

        if (currentStockAtFromLocation.Value < quantity)
        {
            return MaterialErrors.InsufficientStock; // Not enough stock in source location to move
        }

        // Proceed with the movement - updating the batch (no need to adjust ConsumedQuantity here)
        var movement = new MaterialBatchMovement
        {
            BatchId = batchId,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
            Quantity = quantity,
            MovedAt = DateTime.UtcNow,
            MovedById = userId,
            MovementType = MovementType.BetweenLocations // Regular movement
        };

        // Add the movement entry to the context
        await context.MaterialBatchMovements.AddAsync(movement);

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

    
    public async Task<Result<int>> GetMaterialStockInWarehouse(Guid materialId, Guid warehouseId)
    {
        // Sum of quantities moved to this location (incoming batches)
        var batchesInLocation = await context.MaterialBatchMovements
            .Include(m => m.Batch)
            .Include(m => m.ToLocation)
            .Where(m => m.Batch.MaterialId == materialId
                        && m.ToLocation.WarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);
    
        // Sum of quantities moved out of this location (outgoing batches)
        var batchesMovedOut = await context.MaterialBatchMovements
            .Include(m => m.Batch)
            .Include(m => m.FromLocation)
            .Where(m => m.Batch.MaterialId == materialId
                        && m.FromLocation != null && m.FromLocation.WarehouseId == warehouseId)
            .SumAsync(m => m.Quantity);
    
        // Sum of the consumed quantities at this location for the given material
        var batchesConsumedAtLocation = await context.MaterialBatchEvents
            .Include(m => m.Batch)
            .Include(m => m.ConsumedLocation)
            .Where(e => e.Batch.MaterialId == materialId
                        && e.ConsumedLocation != null 
                        && e.ConsumedLocation.WarehouseId == warehouseId
                        && e.Type == EventType.Consumed)
            .SumAsync(e => e.Quantity);

        // Calculate the total available quantity for the material in this location
        var totalQuantityInLocation = batchesInLocation - batchesMovedOut - batchesConsumedAtLocation;

        return totalQuantityInLocation;
    }
    
    public async Task<Result> ConsumeMaterialAtLocation(Guid batchId, Guid locationId, int quantity, Guid userId)
    {
        var materialBatchEvent = new MaterialBatchEvent
        {
            BatchId = batchId,
            Quantity = quantity,
            UserId = userId,
            Type = EventType.Consumed,
            ConsumedLocationId = locationId,
            ConsumedAt = DateTime.UtcNow 
        };

        // Optionally update the batch's consumed quantity
        var materialBatch = await context.MaterialBatches
            .FirstOrDefaultAsync(b => b.Id == batchId);
        if (materialBatch != null)
        {
            materialBatch.ConsumedQuantity += quantity;
            context.MaterialBatches.Update(materialBatch);
        }

        // Add the event to the context
        await context.MaterialBatchEvents.AddAsync(materialBatchEvent);

        // Save changes to the database
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<List<WarehouseStockDto>>> GetMaterialStockAcrossWarehouses(Guid materialId)
    {
        // Get all material batch movements for the given materialId, including both FromLocation and ToLocation
        var batchMovements = await context.MaterialBatchMovements
            .Where(m => m.Batch.MaterialId == materialId)
            .Include(m => m.ToLocation)
            .Include(m => m.FromLocation)
            .ToListAsync();

        // Get all unique warehouse IDs (both from and to locations)
        var warehouseIds = batchMovements
            .SelectMany(m => new[]
            {
                m.ToLocation.WarehouseId, 
                m.FromLocation?.WarehouseId
            })
            .Where(warehouseId => warehouseId.HasValue)
            .Distinct()
            .ToList();

        // List to store the WarehouseStockDto
        var warehouseStockList = new List<WarehouseStockDto>();

        // For each warehouse ID, retrieve the stock and warehouse info
        foreach (var warehouseId in warehouseIds.Where(warehouseId => warehouseId.HasValue))
        {
            var stockResult = await GetMaterialStockInWarehouse(materialId, warehouseId.Value);
            if (!stockResult.IsSuccess) continue;

            // Get warehouse details (you can map this from your Warehouse entity)
            var warehouse = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId.Value);
        
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




    // ************* Check if Requisition Can Be Fulfilled *************

    // Checks if the requisition can be fulfilled with the current stock level
    /*public async Task<Result<bool>> CanFulfillRequisition(Guid materialId, Guid requisitionId)
    {
        var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
        if (material == null)
        {
            return MaterialErrors.NotFound(materialId);
        }

        var requisition = await context.Requisitions.Include(requisition => requisition.Items).FirstOrDefaultAsync(r => r.Id == requisitionId);

        if (requisition is null)
        {
            return RequisitionErrors.NotFound(requisitionId);
        }

        // Get the total available stock for the material in the warehouse
        var totalAvailableStock = await context.MaterialBatches
            .Where(b => b.MaterialId == materialId && b.Status == BatchStatus.Available)
            .SumAsync(b => b.RemainingQuantity);

        // Calculate the remaining stock after fulfilling the requisition
        var remainingStockAfterRequisition = totalAvailableStock - requisition.Items.Where(i => i.MaterialId == ).Sum(i => i.Quantity);

        // Check if the requested quantity can be fulfilled AND ensure the remaining stock doesn't drop below the minimum stock level
        // Requisition can be fulfilled without violating the minimum stock level
        // Not enough stock to fulfill requisition without going below minimum stock
        return remainingStockAfterRequisition >= material.MinimumStockLevel;
    }*/
}
