using DOMAIN.Entities.Items;
using INFRASTRUCTURE.Context;

namespace APP.Services.Stores;

/// <summary>The store workflows that have their own per-store permission keys.</summary>
public enum StoreWorkflow { StockRequisitions, IssueStock, ReturnItems, DamageMissing, PurchaseRequisitions }

public enum StoreWorkflowAction { View, Create, Edit, Delete }

/// <summary>
/// Current-grant checks for the per-store workflow keys (for example CanCreateItStoreIssueStock).
/// Every write needs the workflow's view key as well as its action key; role names never bypass them.
/// </summary>
public static class StoreWorkflowAccess
{
    public static readonly Store[] Stores = [Store.ItStore, Store.GeneralStore, Store.EquipmentStore, Store.ReagentStore];

    private static string Name(Store store) => store switch
    {
        Store.ItStore => "It", Store.GeneralStore => "General", Store.EquipmentStore => "Equipment", Store.ReagentStore => "Reagent",
        _ => throw new ArgumentOutOfRangeException(nameof(store))
    };

    public static string? Key(Store store, StoreWorkflow workflow, StoreWorkflowAction action)
    {
        if (!Enum.IsDefined(store)) return null;
        var n = Name(store);
        return (workflow, action) switch
        {
            (StoreWorkflow.StockRequisitions, StoreWorkflowAction.View) => $"CanView{n}StoreStockRequisitions",
            (StoreWorkflow.StockRequisitions, StoreWorkflowAction.Create) => $"CanCreate{n}StoreStockRequisition",
            (StoreWorkflow.IssueStock, StoreWorkflowAction.View) => $"CanView{n}StoreIssueStocks",
            (StoreWorkflow.IssueStock, StoreWorkflowAction.Create) => $"CanCreate{n}StoreIssueStock",
            (StoreWorkflow.ReturnItems, StoreWorkflowAction.View) => $"CanView{n}StoreReturnItems",
            (StoreWorkflow.ReturnItems, StoreWorkflowAction.Create) => $"CanCreate{n}StoreReturnItem",
            (StoreWorkflow.ReturnItems, StoreWorkflowAction.Edit) => $"CanEdit{n}StoreReturnItem",
            (StoreWorkflow.ReturnItems, StoreWorkflowAction.Delete) => $"CanDelete{n}StoreReturnItem",
            (StoreWorkflow.DamageMissing, StoreWorkflowAction.View) => $"CanView{n}StoreDamageMissingItems",
            (StoreWorkflow.DamageMissing, StoreWorkflowAction.Create) => $"CanCreate{n}StoreDamageMissingItemRecord",
            (StoreWorkflow.DamageMissing, StoreWorkflowAction.Edit) => $"CanEdit{n}StoreDamageMissingItemRecord",
            (StoreWorkflow.DamageMissing, StoreWorkflowAction.Delete) => $"CanDelete{n}StoreDamageMissingItemRecord",
            (StoreWorkflow.PurchaseRequisitions, StoreWorkflowAction.View) => $"CanView{n}StorePurchaseRequisitions",
            (StoreWorkflow.PurchaseRequisitions, StoreWorkflowAction.Create) => $"CanCreate{n}StorePurchaseRequisition",
            (StoreWorkflow.PurchaseRequisitions, StoreWorkflowAction.Edit) => $"CanEdit{n}StorePurchaseRequisition",
            (StoreWorkflow.PurchaseRequisitions, StoreWorkflowAction.Delete) => $"CanDelete{n}StorePurchaseRequisition",
            _ => null
        };
    }

    public static async Task<bool> Allows(ApplicationDbContext context, Guid? actor, Store store, StoreWorkflow workflow, StoreWorkflowAction action)
    {
        var view = Key(store, workflow, StoreWorkflowAction.View);
        var key = Key(store, workflow, action);
        if (!actor.HasValue || view == null || key == null) return false;
        var keys = await Authorization.CurrentPermissionSet.Load(context, actor);
        return keys.Contains(view) && keys.Contains(key);
    }

    /// <summary>True when the actor may perform the action in every one of the stores.</summary>
    public static async Task<bool> AllowsAll(ApplicationDbContext context, Guid? actor, IEnumerable<Store> stores, StoreWorkflow workflow, StoreWorkflowAction action)
    {
        var distinct = stores.Distinct().ToList();
        if (distinct.Count == 0) return false;
        foreach (var store in distinct)
            if (!await Allows(context, actor, store, workflow, action)) return false;
        return true;
    }

    /// <summary>The stores whose records of these workflows the actor may read.</summary>
    public static async Task<HashSet<Store>> Readable(ApplicationDbContext context, Guid? actor, params StoreWorkflow[] workflows)
    {
        var keys = await Authorization.CurrentPermissionSet.Load(context, actor);
        return Stores.Where(store => workflows.Any(workflow => keys.Contains(Key(store, workflow, StoreWorkflowAction.View)!))).ToHashSet();
    }
}
