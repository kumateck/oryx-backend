using APP.Services.Inventory;
using APP.Services.Transactions;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class ItemRepository
{
    private async Task<Error> ValidateItem(CreateItemsRequest request, Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 3 ||
            string.IsNullOrWhiteSpace(request.Code) || !Enum.IsDefined(request.Store) ||
            !Enum.IsDefined(request.Classification) || request.MinimumLevel < 0 ||
            request.MaximumLevel < 0 || request.ReorderLevel < 0)
            return Error.Validation("Item.Invalid", "Provide a name, code, valid store/classification and non-negative whole-number stock levels.");
        request.Name = request.Name.Trim();
        request.Code = request.Code.Trim();
        var name = request.Name.ToUpper();
        var code = request.Code.ToUpper();
        var original = id.HasValue ? await context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id) : null;
        var checkCode = original == null || original.Code != request.Code;
        if (await context.Items.IgnoreQueryFilters().AnyAsync(i => i.Id != id &&
            ((checkCode && i.Code.ToUpper() == code) ||
             (i.Store == request.Store && i.DeletedAt == null && i.Name.ToUpper() == name))))
            return Error.Validation("Item.Exists", "The name is already used in this store, or the code belongs to another current/historical item. Codes must be globally unique while the ledger identifies items by code.");
        if (!await context.UnitOfMeasures.AnyAsync(u => u.Id == request.UnitOfMeasureId))
            return Error.NotFound("UnitOfMeasure.NotFound", "Unit of measure not found.");
        if (request.ItemCategoryId.HasValue && !await context.ItemCategories.AnyAsync(c => c.Id == request.ItemCategoryId))
            return Error.NotFound("ItemCategory.NotFound", "Item category not found.");
        return Error.None;
    }

    private async Task<bool> HasItemReferences(Item item) =>
        item.AvailableQuantity != 0 ||
        await context.ItemTransactionLogs.IgnoreQueryFilters().AnyAsync(r => r.ItemCode == item.Code) ||
        await context.VendorItems.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.ItemStockRequisitionItems.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.InventoryPurchaseRequisitionItems.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.Set<DOMAIN.Entities.Items.Requisitions.SourceInventoryRequisitionItem>().IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.MarketRequisitions.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.VendorQuotationItems.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.MemoItems.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.ItemGrns.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.ItemShipmentInvoiceItems.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.StockEntries.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.DamagedStocks.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.RecoverableItemReports.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id) ||
        await context.ConsumedItems.IgnoreQueryFilters().AnyAsync(r => r.ItemId == item.Id);

    public Task<Result<Guid>> CreateItem(CreateItemsRequest request) =>
        ReferenceMaintenanceTransaction.Run(context, () => CreateItemCore(request));
    private async Task<Result<Guid>> CreateItemCore(CreateItemsRequest request)
    {
        var error = await ValidateItem(request);
        if (error != Error.None) return error;
        var item = mapper.Map<Item>(request);
        await context.Items.AddAsync(item);
        await context.SaveChangesAsync();
        return item.Id;
    }

    public Task<Result> UpdateItem(Guid id, CreateItemsRequest request) =>
        ReferenceMaintenanceTransaction.Run(context, () => UpdateItemCore(id, request));
    private async Task<Result> UpdateItemCore(Guid id, CreateItemsRequest request)
    {
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return Error.NotFound("Item.NotFound", "Item not found.");
        var error = await ValidateItem(request, id);
        if (error != Error.None) return error;
        var identityChanged = item.Code != request.Code || item.Store != request.Store ||
            item.UnitOfMeasureId != request.UnitOfMeasureId || item.Classification != request.Classification;
        if (identityChanged && await HasItemReferences(item))
            return Error.Validation("Item.InUse", "A referenced item's code, store, unit and classification cannot change. Create a separate item instead.");
        mapper.Map(request, item);
        return await APP.Services.Inventory.InventoryCommit.Save(context);
    }

    public Task<Result> DeleteItem(Guid id, Guid userId) =>
        ReferenceMaintenanceTransaction.Run(context, () => DeleteItemCore(id, userId));
    private async Task<Result> DeleteItemCore(Guid id, Guid userId)
    {
        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) return Error.NotFound("Item.NotFound", "Item not found.");
        if (await HasItemReferences(item))
            return Error.Validation("Item.InUse", "Stock or historical references prevent deletion. Deactivate the item instead.");
        item.DeletedAt = DateTime.UtcNow;
        item.LastDeletedById = userId;
        item.IsActive = false;
        return await APP.Services.Inventory.InventoryCommit.Save(context);
    }

    public async Task<Result<List<ItemTransactionLogDto>>> GetItemTransactions(string itemCode, string transactionType)
    {
        var identities = await context.Items.IgnoreQueryFilters().CountAsync(i => i.Code == itemCode);
        if (identities != 1)
            return Error.Validation("Item.AmbiguousLedger", "This code does not identify one item. Its historical ledger needs reconciliation before it can be displayed.");
        var query = context.ItemTransactionLogs.Where(i => i.ItemCode == itemCode);
        if (!string.IsNullOrWhiteSpace(transactionType))
        {
            if (!Enum.TryParse<TransactionType>(transactionType, true, out var transaction) || !Enum.IsDefined(transaction))
                return Error.Validation("Item.InvalidTransactionType", "Unknown transaction type.");
            query = query.Where(i => i.TransactionType == transaction);
        }
        var transactions = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
        return Result.Success(mapper.Map<List<ItemTransactionLogDto>>(transactions));
    }
}
