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
    public async Task<Result<Paginateable<IEnumerable<MaterialDto>>>> GetMaterials(int page, int pageSize, string searchQuery)
    {
        var query = context.Materials
            .AsSplitQuery()
            .Include(m => m.MaterialCategory)
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

        await context.MaterialBatches.AddRangeAsync(batches);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Get Material Batch by ID
    public async Task<Result<MaterialBatchDto>> GetMaterialBatch(Guid batchId)
    {
        var batch = await context.MaterialBatches
            .Include(b => b.Material)
            .Include(b => b.Warehouse)
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
            .Include(b => b.Warehouse)
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
            .SumAsync(b => b.RemainingQuantity);

        return totalStock;
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
