using APP.Services.Authorization;
using DOMAIN.Entities.BillOfMaterials.Request;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Routes;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Authorization;

/// <summary>
/// Requires the product permission for one task: the section and verb, in the context the request names
/// (<c>?context=qa</c> for the QA product pages, otherwise Production). For a bulk <see cref="ProductVerb.Save"/> the
/// submitted rows are compared with the stored ones and every kind of change needs its own key. Grants are read from
/// current data just before the action runs.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ProductPermissionAttribute(ProductSection section, ProductVerb verb) : Attribute, IAsyncActionFilter
{
    public ProductSection Section { get; } = section;
    public ProductVerb Verb { get; } = verb;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var productContext = ProductPermissions.Parse(http.Request.Query["context"]);
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        var verbs = Verb == ProductVerb.Save ? await Changes(db, context.ActionArguments) : [Verb];
        var grants = await CurrentPermissionSet.Load(db, actor);
        var needed = verbs.Select(item => (Verb: item, Key: ProductPermissions.Key(productContext, Section, item))).ToList();
        var refused = needed.Where(item => item.Key is null || !grants.Contains(item.Key)).ToList();
        if (refused.Count == 0) { await next(); return; }
        var detail = refused.Any(item => item.Key is null)
            ? $"{productContext} cannot {string.Join(" or ", refused.Select(item => item.Verb.ToString().ToLowerInvariant()))} the product {Section}."
            : $"You need {string.Join(" and ", refused.Select(item => item.Key))} for this change.";
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "Product.Permission", Description = detail } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    private async Task<HashSet<ProductVerb>> Changes(ApplicationDbContext db, IDictionary<string, object> arguments)
    {
        var productId = arguments.TryGetValue("productId", out var id) && id is Guid guid ? guid : Guid.Empty;
        var rows = arguments.Values;
        return Section switch
        {
            ProductSection.Bom when rows.OfType<CreateBillOfMaterialRequest>().FirstOrDefault() is { } bom => await ProductSectionChanges.Bom(db, bom),
            ProductSection.PackingMaterials when rows.OfType<List<CreateProductPackageRequest>>().FirstOrDefault() is { } packages =>
                await ProductSectionChanges.PackingMaterials(db, productId, packages),
            ProductSection.Procedure when rows.OfType<List<CreateRouteRequest>>().FirstOrDefault() is { } routes =>
                await ProductSectionChanges.Procedure(db, productId, routes),
            ProductSection.PackingStyles when rows.OfType<List<CreateProductPacking>>().FirstOrDefault() is { } styles =>
                await ProductSectionChanges.PackingStyles(db, productId, styles),
            // An unreadable body is refused by model binding; to be safe it needs every kind of change.
            _ => [ProductVerb.Add, ProductVerb.Edit, ProductVerb.Delete],
        };
    }
}
