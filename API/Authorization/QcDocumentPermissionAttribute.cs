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
/// Requires the key for a QC document task. For material documents the raw or packaging key is chosen from the stored
/// document's material (route <c>id</c>, or the STP an ARD is written against) and from every material the request adds,
/// removes or names, and each kind involved needs its key. Product documents need their single key. Grants are read from
/// current data just before the action.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class QcDocumentPermissionAttribute(QcDocument document, QcTask task) : Attribute, IAsyncActionFilter
{
    public QcDocument Document { get; } = document;
    public QcTask Task { get; } = task;

    private static readonly string[] MaterialIdProperties = ["MaterialId", "MaterialIds", "MaterialIdsToAdd", "MaterialIdsToRemove"];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        List<string> needed;
        if (QcDocumentPermissions.IsMaterialDocument(Document))
        {
            var kinds = await Kinds(db, context.ActionArguments);
            // Nothing names a document or material that exists: the action itself answers not found or invalid.
            if (kinds.Count == 0) { await next(); return; }
            needed = kinds.Select(kind => QcDocumentPermissions.MaterialKey(Document, Task, kind)).ToList();
        }
        else needed = [QcDocumentPermissions.ProductKey(Document, Task)];
        var grants = await CurrentPermissionSet.Load(db, actor);
        var missing = needed.Where(key => key is null || !grants.Contains(key)).Distinct().ToList();
        if (missing.Count == 0) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "QcDocument.Permission", Description = $"You need {string.Join(" and ", missing)} for this change." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    private async Task<HashSet<MaterialKind>> Kinds(ApplicationDbContext db, IDictionary<string, object> arguments)
    {
        var kinds = new HashSet<MaterialKind>();
        var materials = new List<Guid>();
        var stps = new List<Guid>();
        foreach (var value in arguments.Values)
        {
            if (value is MaterialKind kind) { kinds.Add(kind); continue; }
            if (value is null || value is Guid || value is string || value.GetType().IsPrimitive) continue;
            foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (MaterialIdProperties.Contains(property.Name)) materials.AddRange(Ids(property.GetValue(value)));
                else if (property.Name == "StpId") stps.AddRange(Ids(property.GetValue(value)));
            }
        }
        if (arguments.TryGetValue("id", out var routeId) && routeId is Guid id)
        {
            switch (Document)
            {
                case QcDocument.MaterialStp: stps.Add(id); break;
                case QcDocument.MaterialSpecification:
                    materials.AddRange(await db.MaterialSpecifications.IgnoreQueryFilters().AsNoTracking().Where(spec => spec.Id == id)
                        .Select(spec => spec.MaterialId).ToListAsync());
                    break;
                case QcDocument.MaterialArd:
                    stps.AddRange(await db.MaterialAnalyticalRawData.IgnoreQueryFilters().AsNoTracking().Where(ard => ard.Id == id)
                        .Select(ard => ard.StpId).ToListAsync());
                    break;
            }
        }
        if (stps.Count > 0)
            materials.AddRange(await db.MaterialStandardTestProcedures.IgnoreQueryFilters().AsNoTracking().Where(stp => stps.Contains(stp.Id))
                .Select(stp => stp.MaterialId).ToListAsync());
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
