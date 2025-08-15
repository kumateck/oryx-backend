using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.DamagedStocks;
using DOMAIN.Entities.Items;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class DamagedStocksRepository(ApplicationDbContext context, IMapper mapper) : IDamagedStocksRepository
{
    public async Task<Result<Guid>> CreateDamagedStocks(CreateDamagedStockRequest request)
    {
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null) return Error.NotFound("Item.Invalid", "Invalid item");

        item.AvailableQuantity -= request.QuantityDamaged;

        context.Items.Update(item);
        await context.SaveChangesAsync();

        var stock = mapper.Map<DamagedStock>(request);
       
       await context.DamagedStocks.AddAsync(stock);
       await context.SaveChangesAsync();
       return stock.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<DamagedStockDto>>>> GetDamagedStocks(int page, int pageSize, string searchQuery)
    {
        var query = context.DamagedStocks.AsQueryable();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Item.Name);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<Store>(searchQuery, true, out var damagedStore))
            {
                query = query.Where(q => q.Item.Store == damagedStore);
            }
        }
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,  entity => mapper.Map<DamagedStockDto>(entity, opts =>
            opts.Items[AppConstants.ModelType] = nameof(DamagedStockDto)));
    }

    public async Task<Result<DamagedStockDto>> GetDamagedStock(Guid id)
    {
        var stocks = await context.DamagedStocks.FirstOrDefaultAsync(ds => ds.Id == id);
        return stocks is null ? 
            Error.NotFound("DamagedStock.NotFound", "Damaged stock not found") : 
            mapper.Map<DamagedStockDto>(stocks, 
                opts => { opts.Items[AppConstants.ModelType] = nameof(DamagedStock);});
        
    }

    public async Task<Result> UpdateDamagedStocks(Guid id, CreateDamagedStockRequest request)
    {
        var damagedStock = await context.DamagedStocks.FirstOrDefaultAsync(ds => ds.Id == id);
        if (damagedStock == null) return Error.NotFound("DamagedStock.NotFound", "Damaged stock not found");
        
        mapper.Map(request, damagedStock);
        context.DamagedStocks.Update(damagedStock);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteDamagedStocks(Guid id, Guid userId)
    {
        var damagedStock = await context.DamagedStocks.FirstOrDefaultAsync(ds => ds.Id == id);
        if (damagedStock == null) return Error.NotFound("DamagedStock.NotFound", "Damaged stock not found");
        
        damagedStock.LastDeletedById = userId;
        damagedStock.DeletedAt = DateTime.UtcNow;
        
        context.DamagedStocks.Update(damagedStock);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}