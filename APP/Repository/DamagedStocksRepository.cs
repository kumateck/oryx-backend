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
    public async Task<Result<Guid>> CreateDamagedStocks(CreateDamagedStockRequest request, Guid userId)
    {
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null)
            return Error.NotFound("Item.Invalid", "Invalid item");

        if (request.QuantityDamaged <= 0)
            return Error.Validation("Quantity.Invalid", "Quantity damaged must be greater than 0");

        if (item.AvailableQuantity < request.QuantityDamaged)
            return Error.Validation("Quantity.ExceedsStock", "Damaged quantity exceeds available stock");

        if (item.HasBatch)
        {
            if (request.Batches == null || request.Batches.Count == 0)
                return Error.Validation("Batches.Required", "Batch details are required for this item.");

            var totalBatchQty = request.Batches.Sum(b => b.Quantity);
            if (totalBatchQty != request.QuantityDamaged)
                return Error.Validation("Batches.QuantityMismatch", "Total batch quantity must equal damaged quantity.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Reduce available quantity
            item.AvailableQuantity -= request.QuantityDamaged;
            context.Items.Update(item);

            // Map damaged stock
            var stock = mapper.Map<DamagedStock>(request);
            stock.Id = Guid.NewGuid();
            stock.CreatedAt = DateTime.UtcNow;

            // Add batches if applicable
            if (item.HasBatch && request.Batches.Count != 0)
            {
                stock.Batches = request.Batches.Select(b => new DamagedStockBatch
                {
                    Id = Guid.NewGuid(),
                    BatchNumber = b.BatchNumber,
                    Quantity = b.Quantity,
                    CreatedAt = DateTime.UtcNow
                }).ToList();
            }

            await context.DamagedStocks.AddAsync(stock);
            
            var log = new DamagedStocksLog
            {
                Id = Guid.NewGuid(),
                DamagedStockId = stock.Id,
                UserId = userId,
                TimeStamp = DateTime.UtcNow
            };
            await context.DamagedStocksLogs.AddAsync(log);

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
            .AsSplitQuery()
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
            .AsSplitQuery()
            .Include(d => d.Item)
            .FirstOrDefaultAsync(ds => ds.Id == id);
        return stocks is null ? 
            Error.NotFound("DamagedStock.NotFound", "Damaged stock not found") : 
            mapper.Map<DamagedStockDto>(stocks, 
                opts => { opts.Items[AppConstants.ModelType] = nameof(DamagedStock);});
        
    }

    public async Task<Result> UpdateDamagedStocks(Guid id, CreateDamagedStockRequest request, Guid userId)
    {
        var damagedStock = await context.DamagedStocks
            .AsSplitQuery()
            .Include(d => d.Item)
            .Include(d => d.Batches)
            .FirstOrDefaultAsync(ds => ds.Id == id);

        if (damagedStock == null)
            return Error.NotFound("DamagedStock.NotFound", "Damaged stock not found");

        var currentQtyDamaged = damagedStock.QuantityDamaged;
        var newQtyDamaged = request.QuantityDamaged;
        var difference = newQtyDamaged - currentQtyDamaged;

        // Adjust item stock
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

        // Update basic damaged stock fields
        mapper.Map(request, damagedStock);

        // Handle batch items
        if (damagedStock.Item.HasBatch)
        {
            if (request.Batches == null || request.Batches.Count == 0)
                return Error.Validation("Batches.Required", "Batch details are required for this item.");

            var totalBatchQty = request.Batches.Sum(b => b.Quantity);
            if (totalBatchQty != newQtyDamaged)
                return Error.Validation("Batches.QuantityMismatch", "Total batch quantity must equal damaged quantity.");

            // Remove old batch records
            context.DamagedStockBatch.RemoveRange(damagedStock.Batches);

            // Add new batch records
            damagedStock.Batches = request.Batches.Select(b => new DamagedStockBatch
            {
                Id = Guid.NewGuid(),
                BatchNumber = b.BatchNumber,
                Quantity = b.Quantity,
                CreatedAt = DateTime.UtcNow
            }).ToList();
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Update damaged stock and item
            context.DamagedStocks.Update(damagedStock);
            context.Items.Update(damagedStock.Item);
            
            var log = new DamagedStocksLog
            {
                Id = Guid.NewGuid(),
                DamagedStockId = damagedStock.Id,
                UserId = userId,
                TimeStamp = DateTime.UtcNow
            };
            await context.DamagedStocksLogs.AddAsync(log);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Error.Failure("DamagedStock.UpdateFailed", ex.Message);
        }
    }

    public async Task<Result> DeleteDamagedStocks(Guid id, Guid userId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var damagedStock = await context.DamagedStocks
                .Include(ds => ds.Item)
                .Include(ds => ds.Batches)
                .FirstOrDefaultAsync(ds => ds.Id == id);

            if (damagedStock == null)
                return Error.NotFound("DamagedStock.NotFound", "Damaged stock not found");
            if (damagedStock.Item.HasBatch && damagedStock.Batches.Count != 0)
            {
                
                var totalBatchQty = damagedStock.Batches.Sum(b => b.Quantity);
                damagedStock.Item.AvailableQuantity += totalBatchQty;
                context.DamagedStockBatch.RemoveRange(damagedStock.Batches);
            }
            else
            {
                damagedStock.Item.AvailableQuantity += damagedStock.QuantityDamaged;
            }

            context.Items.Update(damagedStock.Item);
            
            damagedStock.LastDeletedById = userId;
            damagedStock.DeletedAt = DateTime.UtcNow;
            context.DamagedStocks.Update(damagedStock);
            
            var log = new DamagedStocksLog
            {
                Id = Guid.NewGuid(),
                DamagedStockId = damagedStock.Id,
                UserId = userId,
                TimeStamp = DateTime.UtcNow
            };
            await context.DamagedStocksLogs.AddAsync(log);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Error.Failure("DamagedStock.DeleteFailed", ex.Message);
        }
    }
}