using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ItemGrns;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ItemGrnRepository(ApplicationDbContext context, IMapper mapper) : IItemGrnRepository
{
    public async Task<Result<Guid>> CreateItemGrn(CreateItemGrnRequest request)
    {
        var item = await context.Items.AnyAsync(i => i.Id == request.ItemId);
        if  (!item) return Error.NotFound("Item.NotFound", "Item not found");
        
        var supplier = await context.Suppliers.AnyAsync(i => i.Id == request.SupplierId);
        if  (!supplier) return Error.NotFound("Supplier.NotFound", "Supplier not found");
        
        var  itemGrn = mapper.Map<ItemGrn>(request);
        await context.ItemGrns.AddAsync(itemGrn);
        await context.SaveChangesAsync();
        return itemGrn.Id;
    }

    public async Task<Result<ItemGrnDto>> GetItemGrn(Guid id)
    {
        var itemGrn = await context.ItemGrns.FirstOrDefaultAsync(x => x.Id == id);
        return itemGrn is null ? Error.NotFound("ItemGrn.NotFound","Item Grn not found")
            : Result.Success(mapper.Map<ItemGrnDto>(itemGrn));
    }

    public async Task<Result<Paginateable<IEnumerable<ItemGrnDto>>>> GetItemGrns(int page, int pageSize, string searchQuery)
    {
        var query = context.ItemGrns.AsQueryable();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.InvoiceNumber,
                q=> q.Supplier.Name, q=> q.Item.Code, q=> q.Item.Name);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ItemGrnDto>);
    }

    public async Task<Result> UpdateItemGrn(Guid id, CreateItemGrnRequest request)
    {
        var itemGrn = await context.ItemGrns.FirstOrDefaultAsync(i => i.Id == id);
        if (itemGrn == null) return Error.NotFound("ItemGrn.NotFound", "Item Grn not found");

        mapper.Map(request, itemGrn);
        context.ItemGrns.Update(itemGrn);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteItemGrn(Guid id, Guid userId)
    {
        var itemGrn = await context.ItemGrns.FirstOrDefaultAsync(i => i.Id == id);
        if (itemGrn == null) return Error.NotFound("ItemGrn.NotFound", "Item Grn not found");
        
        itemGrn.DeletedAt = DateTime.UtcNow;
        itemGrn.LastDeletedById = userId;
        
        context.ItemGrns.Update(itemGrn);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}