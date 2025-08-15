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
        if (item == null)
            return Error.NotFound("Item.Invalid", "Invalid item");
        
        if (request.QuantityDamaged <= 0)
            return Error.Validation("Quantity.Invalid", "Quantity damaged must be greater than 0");

        if (item.AvailableQuantity < request.QuantityDamaged)
            return Error.Validation("Quantity.ExceedsStock", "Damaged quantity exceeds available stock");

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            item.AvailableQuantity -= request.QuantityDamaged;
            context.Items.Update(item);
            
            var stock = mapper.Map<DamagedStock>(request);
            stock.Id = Guid.NewGuid();
            stock.CreatedAt = DateTime.UtcNow;

            await context.DamagedStocks.AddAsync(stock);
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return stock.Id;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Error.Failure("DamagedStock.CreateFailed", ex.Message);
        }
    }

    public async Task<Result<Paginateable<IEnumerable<DamagedStockDto>>>> GetDamagedStocks(int page, int pageSize, string searchQuery)
    {
        var query = context.DamagedStocks
            .Include(d => d.Item)
            .AsQueryable();
        
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
        var stocks = await context.DamagedStocks
            .Include(d => d.Item)
            .FirstOrDefaultAsync(ds => ds.Id == id);
        return stocks is null ? 
            Error.NotFound("DamagedStock.NotFound", "Damaged stock not found") : 
            mapper.Map<DamagedStockDto>(stocks, 
                opts => { opts.Items[AppConstants.ModelType] = nameof(DamagedStock);});
        
    }

    public async Task<Result> UpdateDamagedStocks(Guid id, CreateDamagedStockRequest request)
    {
        var damagedStock = await context.DamagedStocks
            .Include(d => d.Item)
            .FirstOrDefaultAsync(ds => ds.Id == id);

        if (damagedStock == null)
            return Error.NotFound("DamagedStock.NotFound", "Damaged stock not found");

        var currentQtyDamaged = damagedStock.QuantityDamaged;
        var newQtyDamaged = request.QuantityDamaged;
        
        var difference = newQtyDamaged - currentQtyDamaged;

        switch (difference)
        {
            case < 0:
                damagedStock.Item.AvailableQuantity += Math.Abs(difference);
                break;
            case > 0 when damagedStock.Item.AvailableQuantity < difference:
                return Error.Validation("DamagedStock.Invalid", "Not enough stock to increase damage quantity.");
            case > 0:
                damagedStock.Item.AvailableQuantity -= difference;
                break;
        }
        
        mapper.Map(request, damagedStock);

        context.DamagedStocks.Update(damagedStock);
        context.Items.Update(damagedStock.Item);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteDamagedStocks(Guid id, Guid userId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        var damagedStock = await context.DamagedStocks
            .Include(ds => ds.Item)
            .FirstOrDefaultAsync(ds => ds.Id == id);

        if (damagedStock == null)
            return Error.NotFound("DamagedStock.NotFound", "Damaged stock not found");
        
        damagedStock.Item.AvailableQuantity += damagedStock.QuantityDamaged;
            
        damagedStock.LastDeletedById = userId;
        damagedStock.DeletedAt = DateTime.UtcNow;

        context.DamagedStocks.Update(damagedStock);
        await context.SaveChangesAsync();

        await transaction.CommitAsync();
        return Result.Success();
    }
}