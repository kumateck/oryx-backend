using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemStockRequisitions;
using DOMAIN.Entities.LeaveRequests;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ItemStockRequisitionRepository(ApplicationDbContext context, IMapper mapper) : IItemStockRequisitionRepository
{
    public async Task<Result<Guid>> CreateItemStockRequisition(CreateItemStockRequisitionRequest request)
    {
        var existingItemStockReq =
            await context.ItemStockRequisitions.FirstOrDefaultAsync(nps => nps.Number == request.Number);

        if (existingItemStockReq != null)
            return Error.Validation("ItemStockRequisition.Exists", "Item Stock Requisition already exists");
        
        var validStockItems = await context.Items
            .Where(s => request.StockItems.Select(si => si.ItemId).Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var missingIds = request.StockItems.Select(s => s.ItemId).Except(validStockItems).ToList();
        if (missingIds.Count != 0)
            return Error.NotFound("Items.NotFound", $"Some items not found: {string.Join(", ", missingIds)}");
        
        var invalidQuantities = request.StockItems
            .Where(i => i.QuantityRequested <= 0)
            .Select(i => i.ItemId)
            .ToList();

        if (invalidQuantities.Count != 0)
        {
            return Error.Validation(
                "Items.InvalidQuantity",
                $"Quantity requested must be greater than zero for items: {string.Join(", ", invalidQuantities)}"
            );
        }

        var itemStockReq = mapper.Map<ItemStockRequisition>(request);
        await context.ItemStockRequisitions.AddAsync(itemStockReq);
        await context.SaveChangesAsync();

        var itemsToAdd = request.StockItems.Select(s => new ItemStockRequisitionItem
        {
            ItemStockRequisitionId = itemStockReq.Id,
            ItemId = s.ItemId,
            QuantityRequested = s.QuantityRequested
        }).ToList();

        await context.ItemStockRequisitionItems.AddRangeAsync(itemsToAdd);
        await context.SaveChangesAsync();
        
        return itemStockReq.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<ItemStockRequisitionDto>>>> GetItemStockRequisitions(int page, int pageSize, string searchQuery)
    {
        var query =  context.ItemStockRequisitions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Number,
                q => q.Department.Name);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<LeaveStatus>(searchQuery, true, out var status))
            {
                query = query.Where(q => q.Status == status);
            }
        }

        return await PaginationHelper.GetPaginatedResultAsync(query,page, pageSize, mapper.Map<ItemStockRequisitionDto>);
    }

    public async Task<Result<ItemStockRequisitionDto>> GetItemStockRequisition(Guid id)
    {
        var itemStockReq = await context.ItemStockRequisitions
            .Include(u => u.CreatedBy)
            .FirstOrDefaultAsync(isr => isr.Id == id);
        return itemStockReq is null ? 
            Error.NotFound("ItemStockRequisition.NotFound", "Item stock requisition not found") 
            : mapper.Map<ItemStockRequisitionDto>(itemStockReq);
    }

    public async Task<Result> UpdateItemStockRequisition(Guid id, CreateItemStockRequisitionRequest request)
    {
        var itemStockReq = await context.ItemStockRequisitions
            .Include(r => r.RequisitionItems)
            .FirstOrDefaultAsync(isr => isr.Id == id);

        if (itemStockReq == null)
            return Error.NotFound("ItemStockRequisition.NotFound", "Item stock requisition not found");
        
        var validStockItems = await context.Items
            .Where(s => request.StockItems.Select(si => si.ItemId).Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var missingIds = request.StockItems.Select(s => s.ItemId).Except(validStockItems).ToList();
        if (missingIds.Count != 0)
            return Error.NotFound("Items.NotFound", $"Some items not found: {string.Join(", ", missingIds)}");

        var invalidQuantities = request.StockItems
            .Where(i => i.QuantityRequested <= 0)
            .Select(i => i.ItemId)
            .ToList();

        if (invalidQuantities.Count != 0)
        {
            return Error.Validation(
                "Items.InvalidQuantity",
                $"Quantity requested must be greater than zero for items: {string.Join(", ", invalidQuantities)}"
            );
        }
        
        mapper.Map(request, itemStockReq);
        
        var requestItemIds = request.StockItems.Select(s => s.ItemId).ToList();
        
        var itemsToRemove = itemStockReq.RequisitionItems
            .Where(i => !requestItemIds.Contains(i.ItemId))
            .ToList();
        context.ItemStockRequisitionItems.RemoveRange(itemsToRemove);
        
        foreach (var stockItem in request.StockItems)
        {
            var existingItem = itemStockReq.RequisitionItems.FirstOrDefault(i => i.ItemId == stockItem.ItemId);
            if (existingItem != null)
            {
                existingItem.QuantityRequested = stockItem.QuantityRequested;
            }
            else
            {
                itemStockReq.RequisitionItems.Add(new ItemStockRequisitionItem
                {
                    ItemStockRequisitionId = itemStockReq.Id,
                    ItemId = stockItem.ItemId,
                    QuantityRequested = stockItem.QuantityRequested
                });
            }
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteItemStockRequisition(Guid id, Guid userId)
    {
        var itemStockReq = await context.ItemStockRequisitions.FirstOrDefaultAsync(isr => isr.Id == id);
        if (itemStockReq == null) return Error.NotFound("ItemStockRequisition.NotFound", "Item stock requisition not found");
        
        itemStockReq.DeletedAt = DateTime.UtcNow;
        itemStockReq.LastDeletedById = userId;
        
        context.ItemStockRequisitions.Update(itemStockReq);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}