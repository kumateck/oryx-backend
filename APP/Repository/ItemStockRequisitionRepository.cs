using APP.Extensions;
using APP.IRepository;
using APP.Services.Inventory;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemStockRequisitions;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Store stock requisitions and the stock issued against them. Every issue is recorded against the requisition
/// line it serves, so what remains on a line is known and repeat issues can never exceed what was requested.
/// </summary>
public class ItemStockRequisitionRepository(ApplicationDbContext context) : IItemStockRequisitionRepository
{
    public async Task<Result<Guid>> CreateItemStockRequisition(CreateItemStockRequisitionRequest request, Guid actor)
    {
        if (string.IsNullOrWhiteSpace(request.Number))
            return Error.Validation("ItemStockRequisition.Number", "The requisition needs a number.");
        if (await context.ItemStockRequisitions.IgnoreQueryFilters().AnyAsync(r => r.Number == request.Number))
            return Error.Validation("ItemStockRequisition.Exists", "A stock requisition with this number already exists.");
        var lines = await ValidLines(request);
        if (lines.IsFailure) return lines.Error;
        if (!await context.Departments.AnyAsync(d => d.Id == request.DepartmentId))
            return Error.NotFound("Department.NotFound", "The requesting department was not found.");

        var requisitionId = Guid.NewGuid();
        context.ItemStockRequisitions.Add(new ItemStockRequisition
        {
            Id = requisitionId,
            Number = request.Number.Trim(),
            Justification = request.Justification,
            // The requester is the signed-in user, never a value supplied in the request.
            RequestedById = actor,
            CreatedById = actor,
            DepartmentId = request.DepartmentId,
            RequisitionItems = request.StockItems.Select(i => new ItemStockRequisitionItem
            {
                Id = Guid.NewGuid(), ItemStockRequisitionId = requisitionId, ItemId = i.ItemId, QuantityRequested = i.QuantityRequested
            }).ToList()
        });
        await context.SaveChangesAsync();
        return requisitionId;
    }

    public async Task<Result<Paginateable<IEnumerable<ItemStockRequisitionDto>>>> GetItemStockRequisitions(int page, int pageSize, string searchQuery,
        IReadOnlyCollection<Store> readableStores, Store? store, IssueItemStockRequisitionStatus? status)
    {
        var query = context.ItemStockRequisitions.AsNoTracking()
            // A requisition is shown only when every store it draws on is readable.
            .Where(r => r.RequisitionItems.Any() && r.RequisitionItems.All(i => readableStores.Contains(i.Item.Store)));
        if (store.HasValue) query = query.Where(r => r.RequisitionItems.Any(i => i.Item.Store == store));
        if (status.HasValue) query = query.Where(r => r.Status == status);
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<IssueItemStockRequisitionStatus>(searchQuery, true, out var searched) && !status.HasValue)
                query = query.Where(r => r.Status == searched);
            else query = query.WhereSearch(searchQuery, r => r.Number, r => r.Department.Name);
        }
        query = query.OrderByDescending(r => r.CreatedAt);
        var total = await query.CountAsync();
        var rows = await Project(query.Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync();
        return PaginationHelper.Page(rows, page, pageSize, total);
    }

    public async Task<Result<ItemStockRequisitionDto>> GetItemStockRequisition(Guid id)
    {
        var dto = await Project(context.ItemStockRequisitions.AsNoTracking().Where(r => r.Id == id)).FirstOrDefaultAsync();
        return dto is null ? Error.NotFound("ItemStockRequisition.NotFound", "Item stock requisition not found") : dto;
    }

    public async Task<List<Store>> StoresOf(Guid id) =>
        await context.ItemStockRequisitionItems.Where(i => i.ItemStockRequisitionId == id && context.ItemStockRequisitions.Any(r => r.Id == id))
            .Select(i => i.Item.Store).Distinct().ToListAsync();

    public async Task<Result> UpdateItemStockRequisition(Guid id, CreateItemStockRequisitionRequest request)
    {
        var requisition = await context.ItemStockRequisitions.Include(r => r.RequisitionItems).FirstOrDefaultAsync(r => r.Id == id);
        if (requisition == null) return Error.NotFound("ItemStockRequisition.NotFound", "Item stock requisition not found");
        if (requisition.Status != IssueItemStockRequisitionStatus.Pending || await context.IssueItemStockRequisitions.AnyAsync(x => x.ItemStockRequisitionId == id))
            return Error.Validation("ItemStockRequisition.Issued", "Stock has been issued against this requisition, so it can no longer be changed.");
        var lines = await ValidLines(request);
        if (lines.IsFailure) return lines.Error;
        if (!await context.Departments.AnyAsync(d => d.Id == request.DepartmentId))
            return Error.NotFound("Department.NotFound", "The requesting department was not found.");

        // The number and the requester stay as created; only the department, justification and lines change.
        requisition.DepartmentId = request.DepartmentId;
        requisition.Justification = request.Justification;
        var wanted = request.StockItems.ToDictionary(i => i.ItemId, i => i.QuantityRequested);
        context.ItemStockRequisitionItems.RemoveRange(requisition.RequisitionItems.Where(i => !wanted.ContainsKey(i.ItemId)).ToList());
        foreach (var (itemId, quantity) in wanted)
        {
            var line = requisition.RequisitionItems.FirstOrDefault(i => i.ItemId == itemId);
            if (line != null) line.QuantityRequested = quantity;
            else context.ItemStockRequisitionItems.Add(new ItemStockRequisitionItem { Id = Guid.NewGuid(), ItemStockRequisitionId = id, ItemId = itemId, QuantityRequested = quantity });
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteItemStockRequisition(Guid id, Guid userId)
    {
        var requisition = await context.ItemStockRequisitions.FirstOrDefaultAsync(r => r.Id == id);
        if (requisition == null) return Error.NotFound("ItemStockRequisition.NotFound", "Item stock requisition not found");
        if (requisition.Status != IssueItemStockRequisitionStatus.Pending || await context.IssueItemStockRequisitions.AnyAsync(x => x.ItemStockRequisitionId == id))
            return Error.Validation("ItemStockRequisition.Issued", "Stock has been issued against this requisition, so it cannot be deleted.");
        requisition.DeletedAt = DateTime.UtcNow;
        requisition.LastDeletedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// Issues stock against the requisition's lines. Each quantity must fit within what remains on its line and within
    /// current, ledger-reconciled stock. Stock, issue records, ledger entries and status are saved together, and a
    /// concurrent change to any item's stock rejects the whole issue.
    /// </summary>
    public async Task<Result> IssueStock(Guid id, IssueStockAgainstRequisitionRequest request, Guid actor)
    {
        var quantities = (request?.QuantitiesToIssue ?? []).Where(q => q.Quantity != 0).ToList();
        if (quantities.Count == 0) return Error.Validation("Request.Quantities", "Enter a quantity to issue for at least one item.");
        if (quantities.Any(q => q.Quantity < 0)) return Error.Validation("Request.Quantities", "Quantities to issue cannot be negative.");
        if (quantities.GroupBy(q => q.ItemId).Any(g => g.Count() > 1)) return Error.Validation("Request.Quantities", "Each item can appear only once in an issue.");

        var requisition = await context.ItemStockRequisitions.Include(r => r.RequisitionItems).ThenInclude(i => i.Item).FirstOrDefaultAsync(r => r.Id == id);
        if (requisition == null) return Error.NotFound("Requisition.NotFound", "Requisition not found.");
        if (requisition.Status == IssueItemStockRequisitionStatus.Completed)
            return Error.Validation("Requisition.Completed", "This requisition has already been fully issued.");
        var history = await context.IssueItemStockRequisitions.Where(x => x.ItemStockRequisitionId == id).ToListAsync();
        if (history.Any(x => x.ItemStockRequisitionItemId == null))
            return Error.Validation("Requisition.Unattributed", "Earlier issues on this requisition were recorded without their item, so what remains cannot be worked out. Reconcile it before issuing more.");
        var issued = history.GroupBy(x => x.ItemStockRequisitionItemId!.Value).ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityIssued));

        foreach (var quantity in quantities)
        {
            var line = requisition.RequisitionItems.FirstOrDefault(i => i.ItemId == quantity.ItemId);
            if (line == null) return Error.Validation("Request.InvalidItem", "An item to issue is not on this requisition.");
            var remaining = line.QuantityRequested - issued.GetValueOrDefault(line.Id);
            if (quantity.Quantity > remaining)
                return Error.Validation("Quantity.OverIssue", $"Only {remaining} of {line.Item.Name} remain to be issued on this requisition.");
            if (quantity.Quantity > line.Item.AvailableQuantity)
                return Error.Validation("Stock.Insufficient", $"Only {line.Item.AvailableQuantity} of {line.Item.Name} are in stock.");
            var ledger = await InventoryLedger.Balance(context, line.Item);
            if (ledger.IsFailure) return ledger.Error;

            line.Item.AvailableQuantity -= quantity.Quantity;
            context.IssueItemStockRequisitions.Add(new IssueItemStockRequisition
            {
                Id = Guid.NewGuid(), ItemStockRequisitionId = id, ItemStockRequisitionItemId = line.Id, QuantityIssued = quantity.Quantity, CreatedById = actor
            });
            context.ItemTransactionLogs.Add(new ItemTransactionLog
            {
                Id = Guid.NewGuid(), ItemCode = line.Item.Code, Date = DateTime.UtcNow, TransactionType = TransactionType.Issued,
                Debit = quantity.Quantity, Credit = 0, TotalBalance = line.Item.AvailableQuantity, CreatedById = actor
            });
            issued[line.Id] = issued.GetValueOrDefault(line.Id) + quantity.Quantity;
        }

        requisition.Status = requisition.RequisitionItems.All(i => issued.GetValueOrDefault(i.Id) >= i.QuantityRequested)
            ? IssueItemStockRequisitionStatus.Completed : IssueItemStockRequisitionStatus.Partial;
        requisition.LastUpdatedById = actor;
        return await InventoryCommit.Save(context);
    }

    private async Task<Result> ValidLines(CreateItemStockRequisitionRequest request)
    {
        var lines = request.StockItems ?? [];
        if (lines.Count == 0) return Error.Validation("Items.Required", "Add at least one item.");
        if (lines.GroupBy(i => i.ItemId).Any(g => g.Count() > 1)) return Error.Validation("Items.Duplicated", "Each item can appear only once.");
        if (lines.Any(i => i.QuantityRequested <= 0)) return Error.Validation("Items.InvalidQuantity", "Every quantity requested must be more than zero.");
        var ids = lines.Select(i => i.ItemId).ToList();
        var items = await context.Items.Where(i => ids.Contains(i.Id)).Select(i => new { i.Id, i.Store, i.IsActive }).ToListAsync();
        if (items.Count != ids.Count) return Error.NotFound("Items.NotFound", "Some items were not found.");
        if (items.Any(i => !i.IsActive)) return Error.Validation("Items.Inactive", "Inactive items cannot be requested.");
        if (items.Select(i => i.Store).Distinct().Count() != 1) return Error.Validation("Items.Stores", "A requisition draws on one store. Raise a separate requisition for each store.");
        return Result.Success();
    }

    private IQueryable<ItemStockRequisitionDto> Project(IQueryable<ItemStockRequisition> query) => query.Select(r => new ItemStockRequisitionDto
    {
        Id = r.Id,
        CreatedAt = r.CreatedAt,
        Number = r.Number,
        RequisitionDate = r.RequisitionDate,
        Justification = r.Justification,
        Status = r.Status,
        Store = r.RequisitionItems.Select(i => (Store?)i.Item.Store).FirstOrDefault(),
        IssueHistoryAttributed = !context.IssueItemStockRequisitions.Any(x => x.ItemStockRequisitionId == r.Id && x.ItemStockRequisitionItemId == null),
        RequestedBy = new UserDto { Id = r.RequestedBy.Id, FirstName = r.RequestedBy.FirstName, LastName = r.RequestedBy.LastName, Email = r.RequestedBy.Email },
        Department = new DepartmentDto { Id = r.Department.Id, Name = r.Department.Name },
        RequisitionItems = r.RequisitionItems.Select(ri => new ItemStockRequisitionItemDto
        {
            Id = ri.Id,
            ItemStockRequisitionId = ri.ItemStockRequisitionId,
            ItemId = ri.ItemId,
            QuantityRequested = ri.QuantityRequested,
            QuantityIssued = context.IssueItemStockRequisitions.Where(x => x.ItemStockRequisitionItemId == ri.Id).Sum(x => (int?)x.QuantityIssued) ?? 0,
            Item = new Item
            {
                Id = ri.Item.Id, Code = ri.Item.Code, Name = ri.Item.Name, Store = ri.Item.Store, IsActive = ri.Item.IsActive,
                AvailableQuantity = ri.Item.AvailableQuantity, Classification = ri.Item.Classification, UnitOfMeasureId = ri.Item.UnitOfMeasureId,
                UnitOfMeasure = new UnitOfMeasure { Id = ri.Item.UnitOfMeasure.Id, Name = ri.Item.UnitOfMeasure.Name, Symbol = ri.Item.UnitOfMeasure.Symbol }
            }
        }).ToList()
    });
}
