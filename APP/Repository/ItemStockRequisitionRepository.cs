using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemStockRequisitions;
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
            if (Enum.TryParse<IssueItemStockRequisitionStatus>(searchQuery, true, out var status))
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
    
    public async Task<Result> IssueStockRequisition(Guid id, IssueStockAgainstRequisitionRequest request)
    {
        var requisition = await context.ItemStockRequisitions
            .Include(r => r.RequisitionItems)
            .ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (requisition == null)
            return Error.NotFound("Requisition.NotFound", "Requisition not found.");

        // Get total quantities already issued per requisition item
        var issuedSoFar = await context.IssueItemStockRequisitions
            .Where(iss => requisition.RequisitionItems.Select(x => x.Id).Contains(iss.ItemStockRequisitionId))
            .GroupBy(iss => iss.ItemStockRequisitionId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.QuantityIssued));

        foreach (var item in requisition.RequisitionItems)
        {
            if (!request.QuantitiesToIssue.TryGetValue(item.Id, out var issueQty))
                continue;

            if (issueQty < 0)
                return Error.Validation("Quantity.Invalid", $"Issued quantity for item {item.Id} cannot be negative.");

            var alreadyIssued = issuedSoFar.GetValueOrDefault(item.Id, 0);
            var remainingToIssue = item.QuantityRequested - alreadyIssued;

            if (issueQty > remainingToIssue)
                return Error.Validation("Quantity.OverIssue", $"Cannot issue more than remaining quantity for item {item.Id}.");

            if (issueQty > item.Item.AvailableQuantity)
                return Error.Validation("Stock.Insufficient", $"Not enough stock for material {item.ItemId}.");
        }
        
        foreach (var item in requisition.RequisitionItems)
        {
            if (!request.QuantitiesToIssue.TryGetValue(item.Id, out var issueQty) || issueQty <= 0)
                continue;

            context.IssueItemStockRequisitions.Add(new IssueItemStockRequisition
            {
                Id = Guid.NewGuid(),
                ItemStockRequisitionId = item.ItemStockRequisitionId, 
                QuantityIssued = issueQty
            });

            item.Item.AvailableQuantity -= issueQty;
        }

        // Determine status
        var fullyIssued = requisition.RequisitionItems.All(i =>
        {
            var alreadyIssued = issuedSoFar.GetValueOrDefault(i.Id, 0);
            return alreadyIssued + request.QuantitiesToIssue.GetValueOrDefault(i.Id, 0) >= i.QuantityRequested;
        });

        requisition.Status = fullyIssued
            ? IssueItemStockRequisitionStatus.Completed
            : IssueItemStockRequisitionStatus.Partial;

        await context.SaveChangesAsync();
        return Result.Success();
    }
    
   public async Task<Result> IssuePartialStockRequisition(Guid requisitionId, IssueStockAgainstRequisitionRequest request)
    {
        var requisition = await context.ItemStockRequisitions
            .Include(r => r.RequisitionItems)
            .ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(r => r.Id == requisitionId && r.Status.Equals(IssueItemStockRequisitionStatus.Partial));

        if (requisition == null)
            return Error.NotFound("Requisition.NotFound", "Requisition not found.");
        
        var issuedSoFar = await context.IssueItemStockRequisitions
            .Where(iss => requisition.RequisitionItems.Select(x => x.Id).Contains(iss.ItemStockRequisitionId))
            .GroupBy(iss => iss.ItemStockRequisitionId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.QuantityIssued));
        
        foreach (var item in requisition.RequisitionItems)
        {
            if (!request.QuantitiesToIssue.TryGetValue(item.Id, out var qtyNowIssued) || qtyNowIssued <= 0)
                continue;

            var alreadyIssued = issuedSoFar.GetValueOrDefault(item.Id, 0);
            var qtyOutstanding = item.QuantityRequested - alreadyIssued;

            if (qtyNowIssued > qtyOutstanding)
                return Error.Validation("Quantity.OverIssue", $"Cannot issue more than outstanding for item {item.Id}.");

            if (qtyNowIssued > item.Item.AvailableQuantity)
                return Error.Validation("Stock.Insufficient", $"Not enough stock for item {item.ItemId}.");
        }
        

        foreach (var item in requisition.RequisitionItems)
        {
            if (!request.QuantitiesToIssue.TryGetValue(item.Id, out var qtyNowIssued) || qtyNowIssued <= 0)
                continue;

            // Deduct stock
            item.Item.AvailableQuantity -= qtyNowIssued;

            // Either update the existing record or insert a new one
            var existingIssue = await context.IssueItemStockRequisitions
                .FirstOrDefaultAsync(x => x.ItemStockRequisitionId == item.Id);

            if (existingIssue != null)
            {
                existingIssue.QuantityIssued += qtyNowIssued;
            }
            else
            {
                context.IssueItemStockRequisitions.Add(new IssueItemStockRequisition
                {
                    Id = Guid.NewGuid(),
                    ItemStockRequisitionId = item.Id,
                    QuantityIssued = qtyNowIssued
                });
            }
        }
        
        var fullyIssued = requisition.RequisitionItems.All(i =>
        {
            var alreadyIssued = issuedSoFar.GetValueOrDefault(i.Id, 0);
            return alreadyIssued + request.QuantitiesToIssue.GetValueOrDefault(i.Id, 0) >= i.QuantityRequested;
        });

        requisition.Status = fullyIssued
            ? IssueItemStockRequisitionStatus.Completed
            : IssueItemStockRequisitionStatus.Partial;

        await context.SaveChangesAsync();

        return Result.Success();
    }
}