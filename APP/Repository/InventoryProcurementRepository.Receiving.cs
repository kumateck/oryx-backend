using APP.Services.Inventory;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.ItemTransactionLogs;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class InventoryProcurementRepository
{
    public async Task<Result> ApproveItem(Guid stockEntryId)
    {
        var entry = await context.StockEntries.Include(e => e.Item).FirstOrDefaultAsync(e => e.Id == stockEntryId);
        if (entry == null) return Error.NotFound("StockEntry.NotFound", "Stock entry not found.");
        if (entry.Status != ApprovalStatus.Pending)
            return Error.Validation("StockEntry.State", "Only pending stock entries can be approved.");
        if (entry.Quantity <= 0 || entry.Quantity != decimal.Truncate(entry.Quantity))
            return Error.Validation("StockEntry.Quantity", "Stock receipt quantity must be a positive whole number.");
        var ledger = await InventoryLedger.Balance(context, entry.Item);
        if (ledger.IsFailure) return ledger.Error;
        var balance = ledger.Value + entry.Quantity;
        if (balance > int.MaxValue) return Error.Validation("StockEntry.Overflow", "The receipt exceeds the supported stock balance.");
        context.ItemTransactionLogs.Add(new ItemTransactionLog {
            Id = Guid.NewGuid(), ItemCode = entry.Item.Code, TransactionType = TransactionType.Purchased,
            Credit = entry.Quantity, Debit = 0, TotalBalance = balance
        });
        entry.Status = ApprovalStatus.Approved;
        entry.Item.AvailableQuantity = (int)balance;
        // One SaveChanges transaction commits the receipt, stock row and ledger together.
        // Keep entry.Quantity as the received quantity, not the cumulative balance.
        return await InventoryCommit.Save(context);
    }

    public async Task<Result> RejectItem(Guid stockEntryId)
    {
        var entry = await context.StockEntries.FirstOrDefaultAsync(e => e.Id == stockEntryId);
        if (entry == null) return Error.NotFound("StockEntry.NotFound", "Stock entry not found.");
        if (entry.Status != ApprovalStatus.Pending)
            return Error.Validation("StockEntry.State", "Only pending stock entries can be rejected.");
        entry.Status = ApprovalStatus.Rejected;
        return await InventoryCommit.Save(context);
    }
}
