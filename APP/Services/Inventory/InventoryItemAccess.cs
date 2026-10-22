using APP.Utils;
using DOMAIN.Entities.Items;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Inventory;

public static class InventoryItemAccess
{
    public static async Task<bool> Allows(ApplicationDbContext context, Guid? actor, Store store,
        string action, bool stockRead = false, bool receivingRead = true)
    {
        if (!actor.HasValue || !Enum.IsDefined(store)) return false;
        var name = store switch { Store.ItStore => "It", Store.GeneralStore => "General",
            Store.EquipmentStore => "Equipment", Store.ReagentStore => "Reagent", _ => null };
        if (name == null || action is not ("ViewItemsIn" or "CreateNewItemIn" or "EditItemIn" or "DeleteItemFrom")) return false;
        var keys = await APP.Services.Authorization.CurrentPermissionSet.Load(context, actor);
        bool Has(string key) => keys.Contains(key);
        var view = Has($"CanViewItemsIn{name}Store");
        if (action == "ViewItemsIn") return view || (stockRead && (Has($"CanView{name}StoreAvailableStocks") || (receivingRead && Has($"CanView{name}StoreReceivingArea"))));
        return view && Has($"Can{action}{name}Store");
    }
}
