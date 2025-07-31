using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ItemRepository(ApplicationDbContext context, IMapper mapper) : IItemRepository
{
    public async Task<Result<Guid>> CreateItem(CreateItemRequest request)
    {
        var item = await context.Items.FirstOrDefaultAsync(i => i.Code == request.Name);
        if (item != null) return Error.Validation("Item.Exists", "Item already exists for this department");
        
        item = mapper.Map<Item>(request);
        await context.Items.AddAsync(item);
        await context.SaveChangesAsync();
        return item.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ItemDto>>>> GetItems(int page, int pageSize,
        string searchQuery)
    {
        var query = context.Items.Include(i => i.Department).AsQueryable();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, i => i.Department.Name, i => i.MaterialName);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ItemDto>);
    }

    public async Task<Result<ItemDto>> GetItem(Guid id)
    {
        var inventory = await context.Items.FirstOrDefaultAsync(i => i.Id == id);
        return inventory is null ? 
            Error.NotFound("Inventory.NotFound", "Inventory not found") :
            mapper.Map<ItemDto>(inventory);
    }
    

    public async Task<Result> UpdateItem(Guid id, CreateItemRequest request)
    {
        var inventory = await context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (inventory == null) return Error.NotFound("Inventory.NotFound", "Inventory not found");
        
        mapper.Map(request, inventory);
        context.Items.Update(inventory);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteItem(Guid id, Guid userId)
    {
        var inventory = await context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (inventory == null) return Error.NotFound("Inventory.NotFound", "Inventory not found");
        
        inventory.DeletedAt = DateTime.UtcNow;
        inventory.LastDeletedById = userId;
        inventory.IsActive = false;
        
        context.Items.Update(inventory);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}