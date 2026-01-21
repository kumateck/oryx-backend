using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemStockRequisitions;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ItemStockRequisitionRepository(ApplicationDbContext context, IMapper mapper) : IItemStockRequisitionRepository
{
    public async Task<Result<Guid>> CreateItemStockRequisition(CreateItemStockRequisitionRequest request)
    {
        // Check for a duplicate requisition number
        if (await context.ItemStockRequisitions
            .AnyAsync(r => r.Number == request.Number))
        {
            return Error.Validation(
                "ItemStockRequisition.Exists",
                "Item Stock Requisition already exists");
        }

        var stockItems = request.StockItems;

        // Prevent duplicate items in the same requisition
        var duplicateItems = stockItems
            .GroupBy(i => i.ItemId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateItems.Count != 0)
        {
            return Error.Validation(
                "Items.Duplicated",
                $"Duplicate items found: {string.Join(", ", duplicateItems)}");
        }

        // Validate quantities
        var invalidQuantities = stockItems
            .Where(i => i.QuantityRequested <= 0)
            .Select(i => i.ItemId)
            .ToList();

        if (invalidQuantities.Count != 0)
        {
            return Error.Validation(
                "Items.InvalidQuantity",
                $"Quantity must be greater than zero for items: {string.Join(", ", invalidQuantities)}");
        }

        // Validate that all items exist
        var itemIds = stockItems.Select(i => i.ItemId).ToList();
        var validItemIds = await context.Items
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => i.Id)
            .ToListAsync();

        var missingItems = itemIds.Except(validItemIds).ToList();
        if (missingItems.Count != 0)
        {
            return Error.NotFound(
                "Items.NotFound",
                $"Some items were not found: {string.Join(", ", missingItems)}");
        }

        // Generate the requisition ID upfront
        var requisitionId = Guid.NewGuid();

        // Create requisition
        var requisition = new ItemStockRequisition
        {
            Id = requisitionId,
            Number = request.Number,
            Justification = request.Justification,
            RequestedById = request.RequestedById,
            DepartmentId = request.DepartmentId,
            RequisitionItems = stockItems.Select(i => new ItemStockRequisitionItem
            {
                Id = Guid.NewGuid(),
                ItemStockRequisitionId = requisitionId,
                ItemId = i.ItemId,
                QuantityRequested = i.QuantityRequested
            }).ToList()
        };
        
        await context.ItemStockRequisitions.AddAsync(requisition);
        await context.SaveChangesAsync();

        return Result.Success(requisitionId);
    }

    public async Task<Result<Paginateable<IEnumerable<ItemStockRequisitionDto>>>> GetItemStockRequisitions(int page, int pageSize, string searchQuery)
    {
        var query = context.ItemStockRequisitions.AsQueryable();

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

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<ItemStockRequisitionDto>);
    }

    public async Task<Result<ItemStockRequisitionDto>> GetItemStockRequisition(Guid id)
    {
        var dto = await context.ItemStockRequisitions
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ItemStockRequisitionDto
            {
                Id = r.Id,
                Number = r.Number,
                RequisitionDate = r.RequisitionDate,
                Justification = r.Justification,
                Status = r.Status,

                RequestedBy = new UserDto
                {
                    Id = r.RequestedBy.Id,
                    FirstName = r.RequestedBy.FirstName,
                    LastName = r.RequestedBy.LastName,
                    Email = r.RequestedBy.Email
                },

                Department = new DepartmentDto
                {
                    Id = r.Department.Id,
                    Name = r.Department.Name
                },

                RequisitionItems = r.RequisitionItems
                    .Select(ri => new ItemStockRequisitionItemDto
                    {
                        ItemStockRequisitionId = ri.ItemStockRequisitionId,
                        Id = ri.Id,
                        QuantityRequested = ri.QuantityRequested,

                        Item = new Item
                        {
                            Id = ri.Item.Id,
                            Code = ri.Item.Code,
                            Name = ri.Item.Name
                        }
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (dto is null)
        {
            return Error.NotFound(
                "ItemStockRequisition.NotFound",
                "Item stock requisition not found");
        }

        return Result.Success(dto);
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
        if (request?.QuantitiesToIssue == null || request.QuantitiesToIssue.Count == 0)
            return Error.Validation("Request.Quantities", "You must specify at least one item to issue.");

        var requisition = await context.ItemStockRequisitions
            .Include(r => r.RequisitionItems)
                .ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (requisition == null)
            return Error.NotFound("Requisition.NotFound", "Requisition not found.");

        // Track total quantities already issued
        var issuedSoFar = await context.IssueItemStockRequisitions
            .Where(iss => requisition.RequisitionItems.Select(x => x.Id).Contains(iss.ItemStockRequisitionId))
            .GroupBy(iss => iss.ItemStockRequisitionId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.QuantityIssued));

        var anyIssued = false;

        foreach (var item in requisition.RequisitionItems)
        {
            if (!request.QuantitiesToIssue.TryGetValue(item.Id, out var issueQty) || issueQty <= 0)
                continue;

            var alreadyIssued = issuedSoFar.GetValueOrDefault(item.Id, 0);
            var remainingToIssue = item.QuantityRequested - alreadyIssued;

            if (issueQty > remainingToIssue)
                return Error.Validation("Quantity.OverIssue", $"Cannot issue more than remaining quantity for item {item.Id}.");

            if (issueQty > item.Item.AvailableQuantity)
                return Error.Validation("Stock.Insufficient", $"Not enough stock for material {item.ItemId}.");

            // Deduct stock
            item.Item.AvailableQuantity -= issueQty;

            // Add Issue record
            context.IssueItemStockRequisitions.Add(new IssueItemStockRequisition
            {
                Id = Guid.NewGuid(),
                ItemStockRequisitionId = item.Id,
                QuantityIssued = issueQty
            });

            // Update issuedSoFar for status calculation
            issuedSoFar[item.Id] = alreadyIssued + issueQty;
            anyIssued = true;
            
            context.ItemTransactionLogs.Add(new ItemTransactionLog
            {
                Id = Guid.NewGuid(),
                ItemCode = item.Item.Code,            
                Date = DateTime.UtcNow,
                TransactionType = TransactionType.Issued,
                Debit = issueQty,
                Credit = 0,
                ShadowHold = null,
                TotalBalance = item.Item.AvailableQuantity
            });
        }

        if (!anyIssued)
            return Error.Validation("Request.Quantities", "No valid quantities to issue.");

        var fullyIssued = requisition.RequisitionItems.All(i =>
            issuedSoFar.GetValueOrDefault(i.Id, 0) >= i.QuantityRequested);

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
            .FirstOrDefaultAsync(r => r.Id == requisitionId && r.Status == IssueItemStockRequisitionStatus.Partial);

        if (requisition == null)
            return Error.NotFound("Requisition.NotFound", "Requisition not found.");

        if (request?.QuantitiesToIssue == null || request.QuantitiesToIssue.Count == 0)
            return Error.Validation("Request.Quantities", "You must specify at least one item to issue.");

        // Load all existing issued quantities for the requisition
        var existingIssues = await context.IssueItemStockRequisitions
            .Where(x => requisition.RequisitionItems.Select(i => i.Id).Contains(x.ItemStockRequisitionId))
            .ToDictionaryAsync(x => x.ItemStockRequisitionId, x => x.QuantityIssued);

        foreach (var item in requisition.RequisitionItems)
        {
            if (!request.QuantitiesToIssue.TryGetValue(item.Id, out var qtyNowIssued) || qtyNowIssued <= 0)
                continue;

            var alreadyIssued = existingIssues.GetValueOrDefault(item.Id, 0);
            var qtyOutstanding = item.QuantityRequested - alreadyIssued;

            if (qtyNowIssued > qtyOutstanding)
                return Error.Validation("Quantity.OverIssue", $"Cannot issue more than outstanding for item {item.Id}.");

            if (qtyNowIssued > item.Item.AvailableQuantity)
                return Error.Validation("Stock.Insufficient", $"Not enough stock for item {item.ItemId}.");

            // Deduct stock
            item.Item.AvailableQuantity -= qtyNowIssued;

            // Add or update issue record
            if (!existingIssues.TryAdd(item.Id, qtyNowIssued))
            {
                existingIssues[item.Id] += qtyNowIssued;
                var existingRecord = await context.IssueItemStockRequisitions
                    .FirstAsync(x => x.ItemStockRequisitionId == item.Id);
                existingRecord.QuantityIssued = existingIssues[item.Id];
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
            
            context.ItemTransactionLogs.Add(new ItemTransactionLog
            {
                Id = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                ItemCode = item.Item.Code,
                TransactionType = TransactionType.Issued,
                Debit = qtyNowIssued,
                Credit = 0,
                ShadowHold = null,
                TotalBalance = item.Item.AvailableQuantity
            });
        }
        
        var status = requisition.RequisitionItems.All(i => existingIssues.GetValueOrDefault(i.Id, 0) >= i.QuantityRequested);
        requisition.Status = status
            ? IssueItemStockRequisitionStatus.Completed
            : IssueItemStockRequisitionStatus.Partial;

        await context.SaveChangesAsync();
        return Result.Success();
    }
}