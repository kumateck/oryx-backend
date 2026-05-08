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

   public async Task<Result> IssueStockRequisition(
    Guid requisitionId,
    IssueStockAgainstRequisitionRequest request)
    {
        if (request?.QuantitiesToIssue == null || request.QuantitiesToIssue.Count == 0)
            return Error.Validation(
                "Request.Quantities",
                "You must specify at least one item to issue."
            );

        var requisition = await context.ItemStockRequisitions
            .Include(r => r.RequisitionItems)
                .ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(r => r.Id == requisitionId);

        if (requisition == null)
            return Error.NotFound("Requisition.NotFound", "Requisition not found.");

        if (requisition.Status == IssueItemStockRequisitionStatus.Completed)
            return Error.Validation(
                "Requisition.Completed",
                "This requisition has already been fully issued."
            );

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            //Group requisition lines by ItemId
            var requisitionItemsByItemId = requisition.RequisitionItems
                .GroupBy(x => x.ItemId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.QuantityRequested).ToList()
                );

            // Load issued quantities per requisition LINE
            var issuedSoFar = await context.IssueItemStockRequisitions
                .Where(iss =>
                    requisition.RequisitionItems
                        .Select(x => x.Id)
                        .Contains(iss.ItemStockRequisitionId))
                .GroupBy(iss => iss.ItemStockRequisitionId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Sum(x => x.QuantityIssued)
                );

            var anyIssued = false;

            // Process request items
            foreach (var requestItem in request.QuantitiesToIssue
                         .Where(requestItem => requestItem.Quantity > 0))
            {
                if (!requisitionItemsByItemId.TryGetValue(requestItem.ItemId, out var lines))
                    return Error.Validation(
                        "Request.InvalidItem",
                        $"Item {requestItem.ItemId} does not exist on this requisition."
                    );

                var remainingToIssue = requestItem.Quantity;

                // Distribute quantity across requisition lines
                foreach (var line in lines)
                {
                    if (remainingToIssue <= 0)
                        break;

                    var alreadyIssued = issuedSoFar.GetValueOrDefault(line.Id, 0);
                    var remainingOnLine = line.QuantityRequested - alreadyIssued;

                    if (remainingOnLine <= 0)
                        continue;

                    var issueQty = Math.Min(remainingOnLine, remainingToIssue);

                    if (issueQty > line.Item.AvailableQuantity)
                        return Error.Validation(
                            "Stock.Insufficient",
                            $"Not enough stock for item {requestItem.ItemId}."
                        );

                    // Deduct stock
                    line.Item.AvailableQuantity -= issueQty;

                    // Record issued quantity (LINE-LEVEL)
                    context.IssueItemStockRequisitions.Add(
                        new IssueItemStockRequisition
                        {
                            Id = Guid.NewGuid(),
                            ItemStockRequisitionId = line.ItemStockRequisitionId,
                            QuantityIssued = issueQty
                        }
                    );

                    issuedSoFar[line.Id] = alreadyIssued + issueQty;

                    // Transaction log (ISSUED = DEBIT)
                    context.ItemTransactionLogs.Add(
                        new ItemTransactionLog
                        {
                            Id = Guid.NewGuid(),
                            ItemCode = line.Item.Code,
                            Date = DateTime.UtcNow,
                            TransactionType = TransactionType.Issued,
                            Debit = issueQty,
                            Credit = 0,
                            ShadowHold = null,
                            TotalBalance = line.Item.AvailableQuantity
                        }
                    );

                    remainingToIssue -= issueQty;
                    anyIssued = true;
                }

                if (remainingToIssue > 0)
                    return Error.Validation(
                        "Quantity.OverIssue",
                        $"Requested quantity exceeds remaining requisition quantity for item {requestItem.ItemId}."
                    );
            }

            if (!anyIssued)
                return Error.Validation(
                    "Request.Quantities",
                    "No valid quantities to issue."
                );

            // Update requisition status
            var fullyIssued = requisition.RequisitionItems.All(i =>
                issuedSoFar.GetValueOrDefault(i.Id, 0) >= i.QuantityRequested
            );

            requisition.Status = fullyIssued
                ? IssueItemStockRequisitionStatus.Completed
                : IssueItemStockRequisitionStatus.Partial;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Result> IssuePartialStockRequisition(
        Guid requisitionId,
        IssueStockAgainstRequisitionRequest request)
    {
        if (request?.QuantitiesToIssue == null || request.QuantitiesToIssue.Count == 0)
            return Error.Validation(
                "Request.Quantities",
                "You must specify at least one item to issue."
            );

        var requisition = await context.ItemStockRequisitions
            .Include(r => r.RequisitionItems)
                .ThenInclude(i => i.Item)
            .FirstOrDefaultAsync(r =>
                r.Id == requisitionId &&
                r.Status == IssueItemStockRequisitionStatus.Partial);

        if (requisition == null)
            return Error.NotFound(
                "Requisition.NotFound",
                "Requisition not found or not in partial state."
            );

        // Group requisition lines by ItemId
        var requisitionItemsByItemId = requisition.RequisitionItems
            .GroupBy(x => x.ItemId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.QuantityRequested).ToList()
            );

        // Load issued quantities PER LINE
        var issuedSoFar = await context.IssueItemStockRequisitions
            .Where(x =>
                requisition.RequisitionItems
                    .Select(i => i.Id)
                    .Contains(x.ItemStockRequisitionId))
            .GroupBy(x => x.ItemStockRequisitionId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Sum(x => x.QuantityIssued)
            );

        var anyIssued = false;

        //Process request items
        foreach (var requestItem in request.QuantitiesToIssue
                     .Where(requestItem => requestItem.Quantity > 0))
        {
            if (!requisitionItemsByItemId.TryGetValue(requestItem.ItemId, out var lines))
                return Error.Validation(
                    "Request.InvalidItem",
                    $"Item {requestItem.ItemId} does not exist on this requisition."
                );

            var remainingToIssue = requestItem.Quantity;

            foreach (var line in lines)
            {
                if (remainingToIssue <= 0)
                    break;

                var alreadyIssued = issuedSoFar.GetValueOrDefault(line.Id, 0);
                var remainingOnLine = line.QuantityRequested - alreadyIssued;

                if (remainingOnLine <= 0)
                    continue;

                var issueQty = Math.Min(remainingOnLine, remainingToIssue);

                if (issueQty > line.Item.AvailableQuantity)
                    return Error.Validation(
                        "Stock.Insufficient",
                        $"Not enough stock for item {requestItem.ItemId}."
                    );

                // Deduct stock
                line.Item.AvailableQuantity -= issueQty;

                // Record issued quantity
                context.IssueItemStockRequisitions.Add(new IssueItemStockRequisition
                {
                    Id = Guid.NewGuid(),
                    ItemStockRequisitionId = line.ItemStockRequisitionId,
                    QuantityIssued = issueQty
                });

                issuedSoFar[line.Id] = alreadyIssued + issueQty;

                // Transaction log
                context.ItemTransactionLogs.Add(new ItemTransactionLog
                {
                    Id = Guid.NewGuid(),
                    Date = DateTime.UtcNow,
                    ItemCode = line.Item.Code,
                    TransactionType = TransactionType.Issued,
                    Debit = issueQty,
                    Credit = 0,
                    ShadowHold = null,
                    TotalBalance = line.Item.AvailableQuantity
                });

                remainingToIssue -= issueQty;
                anyIssued = true;
            }

            if (remainingToIssue > 0)
                return Error.Validation(
                    "Quantity.OverIssue",
                    $"Requested quantity exceeds remaining requisition quantity for item {requestItem.ItemId}."
                );
        }

        if (!anyIssued)
            return Error.Validation(
                "Request.Quantities",
                "No valid quantities to issue."
            );

        // Update requisition status
        requisition.Status = requisition.RequisitionItems.All(i =>
            issuedSoFar.GetValueOrDefault(i.Id, 0) >= i.QuantityRequested)
            ? IssueItemStockRequisitionStatus.Completed
            : IssueItemStockRequisitionStatus.Partial;

        await context.SaveChangesAsync();
        return Result.Success();
    }
}