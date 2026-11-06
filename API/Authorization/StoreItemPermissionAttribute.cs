using APP.Services.Authorization;
using APP.Services.Stores;
using DOMAIN.Entities.Items;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization;

/// <summary>
/// Requires a per-store workflow key (with that workflow's view key) for the store of the item involved: the item a
/// request names (<c>ItemId</c>), or the item of a stored damaged-stock record (route <c>id</c>). Grants are read from
/// current data just before the action.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StoreItemPermissionAttribute(StoreWorkflow workflow, StoreWorkflowAction action) : Attribute, IAsyncActionFilter
{
    public StoreWorkflow Workflow { get; } = workflow;
    public StoreWorkflowAction Action { get; } = action;

    /// <summary>False when the task is offered outside the workflow's own list (damage is reported from available stock), so only the task key is needed.</summary>
    public bool NeedsView { get; init; } = true;

    public IEnumerable<string> AllKeys => StoreWorkflowAccess.Stores
        .SelectMany(store => new[] { NeedsView ? StoreWorkflowAccess.Key(store, Workflow, StoreWorkflowAction.View) : null, StoreWorkflowAccess.Key(store, Workflow, Action) })
        .OfType<string>().Distinct();

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        var stores = await Stores(db, context.ActionArguments);
        // Nothing names an item that exists: the action itself answers not found or invalid.
        if (stores.Count == 0) { await next(); return; }
        var grants = await CurrentPermissionSet.Load(db, actor);
        var needed = stores.SelectMany(store => new[] { NeedsView ? StoreWorkflowAccess.Key(store, Workflow, StoreWorkflowAction.View) : null, StoreWorkflowAccess.Key(store, Workflow, Action) });
        if (needed.All(key => key is not null && grants.Contains(key))) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "Store.Permission", Description = "You do not have this permission for the item's store." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    public static async Task<HashSet<Store>> Stores(ApplicationDbContext db, IDictionary<string, object?> arguments)
    {
        var items = new List<Guid>();
        foreach (var value in arguments.Values)
            if (value?.GetType().GetProperty("ItemId")?.GetValue(value) is Guid itemId) items.Add(itemId);
        if (arguments.TryGetValue("id", out var id) && id is Guid recordId)
            items.AddRange(await db.DamagedStocks.IgnoreQueryFilters().AsNoTracking().Where(item => item.Id == recordId).Select(item => item.ItemId).ToListAsync());
        if (items.Count == 0) return [];
        return (await db.Items.IgnoreQueryFilters().AsNoTracking().Where(item => items.Contains(item.Id)).Select(item => item.Store).Distinct().ToListAsync()).ToHashSet();
    }
}
