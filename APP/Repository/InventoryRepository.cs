using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Inventory;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class InventoryRepository(ApplicationDbContext context, IMapper mapper) : IInventoryRepository
{
    public async Task<Result<Guid>> CreateInventory(CreateInventoryRequest request)
    {
        var inventory = await context.Inventories.FirstOrDefaultAsync(i => i.MaterialName == request.MaterialName);
        if (inventory != null) return Error.Validation("Inventory.Exists", "Inventory already exists");
        
        var uomId = await context.UnitOfMeasures.AnyAsync(u => u.Id == request.UnitOfMeasureId);
        if (!uomId) return Error.NotFound("UnitOfMeasure.NotFound", "Unit of measure not found");
        
        inventory = mapper.Map<Inventory>(request);
        await context.Inventories.AddAsync(inventory);
        await context.SaveChangesAsync();
        return inventory.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<InventoryDto>>>> GetInventories(int page, int pageSize,
        string searchQuery, MaterialKind? materialKind)
    {
        var query = context.Inventories.Include(i => i.Department).AsQueryable();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, i => i.Department.Name, i => i.MaterialName);
        }

        if (materialKind.HasValue)
        {
            query = query.Where(i => i.MaterialBatch.Material.Kind == materialKind.Value);
        }
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<InventoryDto>);
    }

    public async Task<Result<InventoryDto>> GetInventory(Guid id)
    {
        var inventory = await context.Inventories.FirstOrDefaultAsync(i => i.Id == id);
        return inventory is null ? 
            Error.NotFound("Inventory.NotFound", "Inventory not found") :
            mapper.Map<InventoryDto>(inventory);
    }

    public async Task<Result> UpdateInventory(Guid id, CreateInventoryRequest request)
    {
        var inventory = await context.Inventories.FirstOrDefaultAsync(i => i.Id == id);
        if (inventory == null) return Error.NotFound("Inventory.NotFound", "Inventory not found");
        
        mapper.Map(request, inventory);
        context.Inventories.Update(inventory);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteInventory(Guid id, Guid userId)
    {
        var inventory = await context.Inventories.FirstOrDefaultAsync(i => i.Id == id);
        if (inventory == null) return Error.NotFound("Inventory.NotFound", "Inventory not found");
        
        inventory.DeletedAt = DateTime.UtcNow;
        inventory.LastDeletedById = userId;
        
        context.Inventories.Update(inventory);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}