using APP.Services.Authorization;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization;

/// <summary>
/// Requires the foreign or local key of a sourcing task for every supplier the request involves. The supplier types come
/// from stored records: the supplier (route <c>supplierId</c>), a supplier quotation (route <c>supplierQuotationId</c>)
/// or the suppliers a list of <see cref="ProcessQuotation"/> names; never a request flag. Grants are read from current
/// data just before the action.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SupplierTypePermissionAttribute(string foreignKey, string localKey) : Attribute, IAsyncActionFilter
{
    public string ForeignKey { get; } = foreignKey;
    public string LocalKey { get; } = localKey;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        var types = await Types(db, context.ActionArguments);
        // Nothing names a supplier that exists: the action itself answers not found or invalid.
        if (types.Count == 0) { await next(); return; }
        var grants = await CurrentPermissionSet.Load(db, actor);
        var missing = types.Select(type => type == SupplierType.Foreign ? ForeignKey : LocalKey).Where(key => !grants.Contains(key)).Distinct().ToList();
        if (missing.Count == 0) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "Supplier.Permission", Description = $"You need {string.Join(" and ", missing)} for this." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    public static async Task<HashSet<SupplierType>> Types(ApplicationDbContext db, IDictionary<string, object?> arguments)
    {
        var suppliers = new List<Guid>();
        if (arguments.TryGetValue("supplierId", out var supplier) && supplier is Guid supplierId) suppliers.Add(supplierId);
        if (arguments.Values.OfType<IEnumerable<ProcessQuotation>>().FirstOrDefault() is { } quotations)
            suppliers.AddRange(quotations.Select(quotation => quotation.SupplierId));
        if (arguments.TryGetValue("supplierQuotationId", out var quotation) && quotation is Guid quotationId)
            suppliers.AddRange(await db.SupplierQuotations.IgnoreQueryFilters().AsNoTracking().Where(item => item.Id == quotationId)
                .Select(item => item.SupplierId).ToListAsync());
        if (suppliers.Count == 0) return [];
        return (await db.Suppliers.IgnoreQueryFilters().AsNoTracking().Where(item => suppliers.Contains(item.Id))
            .Select(item => item.Type).Distinct().ToListAsync()).ToHashSet();
    }
}
