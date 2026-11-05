using APP.Services.Authorization;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders.Request;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization;

/// <summary>
/// Requires the foreign or local key of a purchase-order task. The variant is the stored order's supplier type (route
/// <c>purchaseOrderId</c>), or for a new order the type of the supplier it names; never a request flag. Grants are read
/// from current data just before the action.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PurchaseOrderPermissionAttribute(string foreignKey, string localKey) : Attribute, IAsyncActionFilter
{
    public string ForeignKey { get; } = foreignKey;
    public string LocalKey { get; } = localKey;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        SupplierType? type = null;
        if (context.ActionArguments.TryGetValue("purchaseOrderId", out var id) && id is Guid orderId)
            type = await db.PurchaseOrders.IgnoreQueryFilters().AsNoTracking().Where(order => order.Id == orderId)
                .Select(order => (SupplierType?)order.Supplier.Type).FirstOrDefaultAsync();
        else if (context.ActionArguments.Values.OfType<CreatePurchaseOrderRequest>().FirstOrDefault() is { } request)
            type = await db.Suppliers.IgnoreQueryFilters().AsNoTracking().Where(supplier => supplier.Id == request.SupplierId)
                .Select(supplier => (SupplierType?)supplier.Type).FirstOrDefaultAsync();
        // No such order or supplier: the action itself answers not found or invalid.
        if (type is null) { await next(); return; }
        var key = type == SupplierType.Foreign ? ForeignKey : LocalKey;
        if ((await CurrentPermissionSet.Load(db, actor)).Contains(key)) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "PurchaseOrder.Permission", Description = $"You need {key} for this." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }
}
