using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization;

public enum CollectionTask { Create, Edit, Delete }

/// <summary>
/// Requires the key of a collection (reference list) task for the collection named by the route <c>itemType</c>, the
/// same keys the collection settings pages check. Material categories use the raw or packaging key: from the request
/// when creating, from the stored category otherwise. Collection types no page manages share one key, granted to nobody
/// until assigned. Grants are read from current data just before the action.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CollectionPermissionAttribute(CollectionTask task) : Attribute, IAsyncActionFilter
{
    public CollectionTask Task { get; } = task;

    /// <summary>The managed collection types and their create, edit and delete keys.</summary>
    public static readonly IReadOnlyDictionary<string, (string Create, string Edit, string Delete)> Keys = new Dictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase)
    {
        ["ProductCategory"] = ("CanCreateProductCategory", "CanEditProductCategory", "CanDeleteProductCategory"),
        ["ItemCategory"] = ("CanCreateItemCategory", "CanEditItemCategory", "CanDeleteItemCategory"),
        ["QcEquipmentCategory"] = ("CanCreateEquipmentCategory", "CanEditEquipmentCategory", "CanDeleteEquipmentCategory"),
        ["Resource"] = ("CanCreateResource", "CanEditResource", "CanDeleteResource"),
        ["Operation"] = ("CanCreateOperation", "CanEditOperation", "CanDeleteOperation"),
        ["WorkCenter"] = ("CanCreateWorkCenter", "CanEditWorkCenter", "CanDeleteWorkCenter"),
        ["Site"] = ("CanCreateSite", "CanEditWorkSite", "CanDeleteSite"),
        ["MaterialType"] = ("CanCreateMaterialType", "CanEditMaterialType", "CanDeleteMaterialType"),
        ["UnitOfMeasure"] = ("CanCreateUnitOfMeasure", "CanEditUnitOfMeasure", "CanDeleteUnitOfMeasure"),
        ["PackageStyle"] = ("CanCreatePackageStyle", "CanEditPackageStyle", "CanDeletePackageStyle"),
        ["ProductState"] = ("CanCreateProductState", "CanEditProductState", "CanDeleteProductState"),
        ["Country"] = ("CanCreateCountries", "CanEditCountries", "CanDeleteCountries"),
        ["ShiftCategory"] = ("CanCreateShiftCategory", "CanEditShiftCategory", "CanDeleteShiftCategory"),
        ["TermsOfPayment"] = ("CanCreatePaymentTerm", "CanEditPaymentTerm", "CanDeletePaymentTerm"),
        ["DeliveryMode"] = ("CanCreateDeliveryMode", "CanEditDeliveryMode", "CanDeleteDeliveryMode"),
        ["Charge"] = ("CanCreateCharges", "CanEditCharges", "CanDeleteCharges"),
    };

    private static readonly (string Create, string Edit, string Delete) Raw = ("CanCreateRawCategory", "CanEditRawCategory", "CanDeleteRawCategory");
    private static readonly (string Create, string Edit, string Delete) Packaging = ("CanCreatePackageCategory", "CanEditPackageCategory", "CanDeletePackageCategory");

    /// <summary>Every key this task can ask for.</summary>
    public IEnumerable<string> AllKeys => Keys.Values.Append(Raw).Append(Packaging).Select(Pick).Append(PermissionKeys.CanManageOtherCollectionTypes).Distinct();

    private string Pick((string Create, string Edit, string Delete) keys) => Task switch
    {
        CollectionTask.Create => keys.Create, CollectionTask.Edit => keys.Edit, _ => keys.Delete,
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        var needed = await Needed(db, context.ActionArguments);
        var grants = await CurrentPermissionSet.Load(db, actor);
        var missing = needed.Where(key => !grants.Contains(key)).ToList();
        if (missing.Count == 0) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "Collection.Permission", Description = $"You need {string.Join(" and ", missing)} for this." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    public async Task<List<string>> Needed(ApplicationDbContext db, IDictionary<string, object?> arguments)
    {
        var type = arguments.TryGetValue("itemType", out var value) ? value as string : null;
        if (string.Equals(type, nameof(MaterialCategory), StringComparison.OrdinalIgnoreCase))
        {
            var kinds = new HashSet<MaterialKind>();
            if (Task != CollectionTask.Create && arguments.TryGetValue("itemId", out var id) && id is Guid itemId)
                kinds.UnionWith(await db.MaterialCategories.IgnoreQueryFilters().AsNoTracking().Where(item => item.Id == itemId).Select(item => item.MaterialKind).ToListAsync());
            if (arguments.Values.OfType<CreateItemRequest>().FirstOrDefault()?.MaterialKind is { } requested) kinds.Add(requested);
            // A category with no kind named needs both keys.
            if (kinds.Count == 0) kinds.UnionWith([MaterialKind.Raw, MaterialKind.Package]);
            return kinds.Select(kind => Pick(kind == MaterialKind.Raw ? Raw : Packaging)).Distinct().ToList();
        }
        return [type is not null && Keys.TryGetValue(type, out var keys) ? Pick(keys) : PermissionKeys.CanManageOtherCollectionTypes];
    }
}
