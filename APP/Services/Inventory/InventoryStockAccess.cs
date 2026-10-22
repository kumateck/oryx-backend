using DOMAIN.Entities.Items;
using INFRASTRUCTURE.Context;

namespace APP.Services.Inventory;

public enum InventoryStockOperation { ViewAvailable, ViewReceiving, ApproveReceipt, RejectReceipt, CreateDamage }
public static class InventoryStockAccess
{
    public static async Task<bool> Allows(ApplicationDbContext context, Guid? actor, Store store, InventoryStockOperation operation)
    {
        if (!Enum.IsDefined(store)) return false;
        var name = store switch { Store.ItStore => "It", Store.GeneralStore => "General",
            Store.EquipmentStore => "Equipment", Store.ReagentStore => "Reagent", _ => null };
        if (name == null) return false;
        var keys = await APP.Services.Authorization.CurrentPermissionSet.Load(context, actor);
        var read = operation == InventoryStockOperation.ViewAvailable ? $"CanView{name}StoreAvailableStocks" : $"CanView{name}StoreReceivingArea";
        var write = operation switch {
            InventoryStockOperation.ApproveReceipt => $"CanApprove{name}StoreReceivingAreaEntry",
            InventoryStockOperation.RejectReceipt => $"CanReject{name}StoreReceivingAreaEntry",
            InventoryStockOperation.CreateDamage => $"CanCreate{name}StoreDamageMissingItemRecord",
            _ => read
        };
        if (operation == InventoryStockOperation.CreateDamage) read = $"CanView{name}StoreAvailableStocks";
        return keys.Contains(read) && keys.Contains(write);
    }
}
