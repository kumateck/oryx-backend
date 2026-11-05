using System.Collections;
using System.Reflection;
using APP.Services.Authorization;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization;

/// <summary>
/// Requires the raw-material or packaging-material key of a task. The kinds come from what the request names, read from
/// the database: materials (<c>MaterialId</c>, <c>MaterialIds</c>), material batches (<c>MaterialBatchId</c>,
/// <c>MaterialBatchIds</c>) and distributed requisition materials (<c>DistributedRequisitionMaterialId</c>), in the body or
/// the route, plus any <see cref="MaterialKind"/> argument. Every kind involved needs its key. Grants are read from current
/// data just before the action.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MaterialKindPermissionAttribute(string rawKey, string packagingKey) : Attribute, IAsyncActionFilter
{
    public string RawKey { get; } = rawKey;
    public string PackagingKey { get; } = packagingKey;

    public string Key(MaterialKind kind) => kind == MaterialKind.Raw ? RawKey : PackagingKey;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        var kinds = await Kinds(db, context.ActionArguments);
        // Nothing names a material that exists: the action itself answers not found or invalid.
        if (kinds.Count == 0) { await next(); return; }
        var grants = await CurrentPermissionSet.Load(db, actor);
        var missing = kinds.Select(Key).Where(key => !grants.Contains(key)).Distinct().ToList();
        if (missing.Count == 0) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "Material.Permission", Description = $"You need {string.Join(" and ", missing)} for this." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    private static readonly string[] MaterialNames = ["MaterialId", "MaterialIds"];
    private static readonly string[] BatchNames = ["MaterialBatchId", "MaterialBatchIds", "BatchId"];
    private static readonly string[] DistributedNames = ["DistributedRequisitionMaterialId", "DistributedMaterialId"];

    internal static async Task<HashSet<MaterialKind>> Kinds(ApplicationDbContext db, IDictionary<string, object> arguments)
    {
        var kinds = new HashSet<MaterialKind>();
        var materials = new List<Guid>();
        var batches = new List<Guid>();
        var distributed = new List<Guid>();
        void Collect(string name, object value)
        {
            if (MaterialNames.Contains(name, StringComparer.OrdinalIgnoreCase)) materials.AddRange(Ids(value));
            else if (BatchNames.Contains(name, StringComparer.OrdinalIgnoreCase)) batches.AddRange(Ids(value));
            else if (DistributedNames.Contains(name, StringComparer.OrdinalIgnoreCase)) distributed.AddRange(Ids(value));
        }
        foreach (var (name, value) in arguments)
        {
            if (value is MaterialKind kind) { kinds.Add(kind); continue; }
            Collect(name, value);
            if (value is null || value is Guid || value is string || value.GetType().IsPrimitive || value is IEnumerable) continue;
            foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                Collect(property.Name, property.GetValue(value));
        }
        if (distributed.Count > 0)
            materials.AddRange((await db.DistributedRequisitionMaterials.IgnoreQueryFilters().AsNoTracking()
                .Where(item => distributed.Contains(item.Id) && item.MaterialId != null).Select(item => item.MaterialId).ToListAsync()).OfType<Guid>());
        if (batches.Count > 0)
            materials.AddRange(await db.MaterialBatches.IgnoreQueryFilters().AsNoTracking().Where(batch => batches.Contains(batch.Id))
                .Select(batch => batch.MaterialId).ToListAsync());
        if (materials.Count > 0)
            kinds.UnionWith(await db.Materials.IgnoreQueryFilters().AsNoTracking().Where(material => materials.Contains(material.Id))
                .Select(material => material.Kind).Distinct().ToListAsync());
        return kinds;
    }

    private static IEnumerable<Guid> Ids(object value) => value switch
    {
        Guid id when id != Guid.Empty => [id],
        IEnumerable list and not string => list.OfType<Guid>().Where(id => id != Guid.Empty),
        _ => [],
    };
}
