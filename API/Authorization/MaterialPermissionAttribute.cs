using APP.Services.Authorization;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization;

/// <summary>
/// Requires the raw or packaging key for a material task. The kinds come from the stored materials the request names
/// (route <c>materialId</c>, department links), plus any kind the request sets (a new material, an upload, or an edit
/// that changes the kind), and every kind involved needs its key. Grants are read from current data just before the action.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MaterialPermissionAttribute(MaterialTask task) : Attribute, IAsyncActionFilter
{
    public MaterialTask Task { get; } = task;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        var kinds = await Kinds(db, context.ActionArguments);
        // Nothing names a material that exists: the action itself answers not found or invalid.
        if (kinds.Count == 0) { await next(); return; }
        var grants = await CurrentPermissionSet.Load(db, actor);
        var missing = kinds.Select(kind => MaterialPermissions.Key(Task, kind)).Where(key => key is null || !grants.Contains(key)).Distinct().ToList();
        if (missing.Count == 0) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "Material.Permission", Description = $"You need {string.Join(" and ", missing)} for this change." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    private static async Task<HashSet<MaterialKind>> Kinds(ApplicationDbContext db, IDictionary<string, object> arguments)
    {
        var kinds = new HashSet<MaterialKind>();
        var ids = new List<Guid>();
        foreach (var value in arguments.Values)
            switch (value)
            {
                case MaterialKind kind: kinds.Add(kind); break;
                case DOMAIN.Entities.Materials.CreateMaterialRequest request: kinds.Add(request.Kind); break;
                case List<CreateMaterialDepartment> links: ids.AddRange(links.Select(link => link.MaterialId)); break;
            }
        if (arguments.TryGetValue("materialId", out var id) && id is Guid materialId) ids.Add(materialId);
        if (ids.Count > 0)
            kinds.UnionWith(await db.Materials.IgnoreQueryFilters().AsNoTracking().Where(material => ids.Contains(material.Id))
                .Select(material => material.Kind).Distinct().ToListAsync());
        return kinds;
    }
}
