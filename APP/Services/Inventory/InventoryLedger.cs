using DOMAIN.Entities.Items;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Inventory;

public static class InventoryLedger
{
    public static async Task<Result<decimal>> Balance(ApplicationDbContext context, Item item)
    {
        if (!item.IsActive || item.AvailableQuantity < 0)
            return Error.Validation("Inventory.Inactive", "The item must be active with a non-negative balance.");
        if (await context.Items.IgnoreQueryFilters().CountAsync(i => i.Code == item.Code) != 1)
            return Error.Validation("Inventory.AmbiguousCode", "The item code is shared by historical identities. Reconcile its ledger before posting stock.");
        var latest = await context.ItemTransactionLogs.Where(t => t.ItemCode == item.Code)
            .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).FirstOrDefaultAsync();
        var balance = latest?.TotalBalance ?? 0;
        if (balance != item.AvailableQuantity)
            return Error.Validation("Inventory.Unreconciled", "The ledger and available balance disagree. Reconcile them before posting stock.");
        return balance;
    }
}
