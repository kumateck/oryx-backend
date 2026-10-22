using APP.IRepository;
using APP.Services.Inventory;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.RecoverableItemsReports;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Returns of recoverable items to their store. A return is made against the requisition line the stock was issued on,
/// can never exceed what was issued less what was already returned, and puts the stock back in one atomic posting.
/// </summary>
public class RecoverableItemReportRepository(ApplicationDbContext context) : IRecoverableItemReportRepository
{
    public async Task<Result<Guid>> CreateRecoverableItemReport(CreateRecoverableItemReportRequest request, Guid actor)
    {
        if (request.Quantity <= 0) return Error.Validation("Invalid.Quantity", "Quantity must be greater than 0.");
        if (request.ItemStockRequisitionItemId is not { } lineId)
            return Error.Validation("Return.Line", "Choose the stock requisition item being returned.");
        var line = await context.ItemStockRequisitionItems.Include(l => l.Item)
            .FirstOrDefaultAsync(l => l.Id == lineId && context.ItemStockRequisitions.Any(r => r.Id == l.ItemStockRequisitionId));
        if (line is null) return Error.NotFound("Return.Line", "The stock requisition item was not found.");
        if (line.ItemId != request.ItemId) return Error.Validation("Return.Item", "The item does not match the stock requisition item.");
        var item = line.Item;
        if (item.Classification == InventoryClassification.NonRecoverable) return Error.Validation("Item.NotRecoverable", "Item is not recoverable.");

        var issued = await context.IssueItemStockRequisitions.Where(x => x.ItemStockRequisitionItemId == lineId).SumAsync(x => (int?)x.QuantityIssued) ?? 0;
        var returned = await context.RecoverableItemReports.Where(x => x.ItemStockRequisitionItemId == lineId).SumAsync(x => (int?)x.Quantity) ?? 0;
        if (request.Quantity > issued - returned)
            return Error.Validation("Return.Exceeds", $"Only {Math.Max(0, issued - returned)} issued on this requisition remain to be returned.");
        var ledger = await InventoryLedger.Balance(context, item);
        if (ledger.IsFailure) return ledger.Error;

        item.AvailableQuantity += request.Quantity;
        var report = new RecoverableItemReport
        {
            Id = Guid.NewGuid(), ItemId = item.Id, Quantity = request.Quantity, Reason = request.Reason, ItemStockRequisitionItemId = lineId, CreatedById = actor
        };
        context.RecoverableItemReports.Add(report);
        context.ItemTransactionLogs.Add(new ItemTransactionLog
        {
            Id = Guid.NewGuid(), ItemCode = item.Code, TransactionType = TransactionType.Returned, Credit = request.Quantity, Debit = 0,
            TotalBalance = item.AvailableQuantity, CreatedById = actor
        });
        // Stock, report and ledger are one atomic SaveChanges, rejected if the item's stock changed meanwhile.
        var commit = await InventoryCommit.Save(context);
        return commit.IsFailure ? commit.Error : report.Id;
    }

    public async Task<Result<List<RecoverableItemReportDto>>> GetRecoverableItemReport(IReadOnlyCollection<Store> readableStores) =>
        await context.RecoverableItemReports.AsNoTracking()
            .Where(r => readableStores.Contains(r.Item.Store))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RecoverableItemReportDto
            {
                Id = r.Id, CreatedAt = r.CreatedAt, Quantity = r.Quantity, Reason = r.Reason, Store = r.Item.Store,
                ItemStockRequisitionItemId = r.ItemStockRequisitionItemId,
                ItemStockRequisitionId = r.ItemStockRequisitionItem == null ? null : r.ItemStockRequisitionItem.ItemStockRequisitionId,
                RequisitionNumber = r.ItemStockRequisitionItem == null ? null : r.ItemStockRequisitionItem.ItemStockRequisition.Number,
                CreatedBy = r.CreatedBy == null ? null : new UserDto { Id = r.CreatedBy.Id, FirstName = r.CreatedBy.FirstName, LastName = r.CreatedBy.LastName, Email = r.CreatedBy.Email },
                Item = new ItemDto
                {
                    Id = r.Item.Id, Name = r.Item.Name, Code = r.Item.Code, Store = r.Item.Store, Classification = r.Item.Classification,
                    AvailableQuantity = r.Item.AvailableQuantity, IsActive = r.Item.IsActive,
                    UnitOfMeasure = r.Item.UnitOfMeasure == null ? null : new UnitOfMeasureDto { Id = r.Item.UnitOfMeasure.Id, Name = r.Item.UnitOfMeasure.Name, Symbol = r.Item.UnitOfMeasure.Symbol }
                }
            }).ToListAsync();
}
