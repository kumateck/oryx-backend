using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Items;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ItemRepository(ApplicationDbContext context, IMapper mapper) : IItemRepository
{
    public async Task<Result<Guid>> CreateItem(CreateItemsRequest request)
    {
        var item = await context.Items.FirstOrDefaultAsync(i => i.Code == request.Code || i.Name == request.Name);;
        if (item != null) return Error.Validation("Item.Exists", "Item already exists for this department");
        
        item = mapper.Map<Item>(request);
        await context.Items.AddAsync(item);
        await context.SaveChangesAsync();
        return item.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ItemDto>>>> GetItems(int page, int pageSize,
        string searchQuery, Store? store)
    {
        var query = context.Items
            .Include(i => i.UnitOfMeasure)
            .AsQueryable();
        
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, i => i.Name,
                i => i.Code);
        }

        if (store.HasValue)
        {
            query = query.Where(i => i.Store == store.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<Store>(searchQuery, true, out var itemStore))
            {
                query = query.Where(q => q.Store == itemStore);
            }
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query,
            page,
            pageSize,
            entity => mapper.Map<ItemDto>(entity, opts =>
            opts.Items[AppConstants.ModelType] = nameof(Item)));
    }

    public async Task<Result<ItemDto>> GetItem(Guid id)
    {
        var item = await context.Items
            .Include(i => i.UnitOfMeasure)
            .FirstOrDefaultAsync(i => i.Id == id);
        return item is null ? 
            Error.NotFound("Item.NotFound", "Item not found") :
            mapper.Map<ItemDto>(item,
                opts => opts.Items[AppConstants.ModelType] = nameof(Item));
    }
    

    public async Task<Result> UpdateItem(Guid id, CreateItemsRequest request)
    {
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return Error.NotFound("Item.NotFound", "Item not found");
        
        mapper.Map(request, item);
        context.Items.Update(item);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteItem(Guid id, Guid userId)
    {
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return Error.NotFound("Item.NotFound", "Item not found");
        
        item.DeletedAt = DateTime.UtcNow;
        item.LastDeletedById = userId;
        item.IsActive = false;
        
        context.Items.Update(item);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}