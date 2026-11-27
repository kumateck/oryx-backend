using System.Reflection;
using APP.Services.Authorization;
using DOMAIN.Entities.StpRevisions;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace API.Authorization;

/// <summary>
/// Requires the key for an STP revision task in the revision's kind. An existing revision (route <c>id</c>) is judged by
/// its stored kind; a new one by the <see cref="StpRevisionKind"/> argument or the request's <c>Kind</c>. Grants are read
/// from current data just before the action, and the service checks them again before each write.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StpRevisionPermissionAttribute(StpRevisionTask task) : Attribute, IAsyncActionFilter
{
    public StpRevisionTask Task { get; } = task;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var db = http.RequestServices.GetRequiredService<ApplicationDbContext>();
        var kind = await Kind(db, context.ActionArguments);
        // Nothing names an existing revision or a kind: the action answers not found or invalid.
        if (kind is null || !Enum.IsDefined(kind.Value)) { await next(); return; }
        var key = StpRevisionPermissions.Key(Task, kind.Value);
        var grants = await CurrentPermissionSet.Load(db, actor);
        if (grants.Contains(key)) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "StpRevision.Permission", Description = $"You need {key} for this." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    private static async Task<StpRevisionKind?> Kind(ApplicationDbContext db, IDictionary<string, object> arguments)
    {
        if (arguments.TryGetValue("id", out var routeId) && routeId is Guid id)
            return await db.StpRevisions.IgnoreQueryFilters().AsNoTracking().Where(item => item.Id == id)
                .Select(item => (StpRevisionKind?)item.Kind).FirstOrDefaultAsync();
        foreach (var value in arguments.Values)
        {
            if (value is StpRevisionKind kind) return kind;
            if (value is null || value is string || value.GetType().IsPrimitive || value is Guid) continue;
            var property = value.GetType().GetProperty("Kind", BindingFlags.Public | BindingFlags.Instance);
            if (property?.PropertyType == typeof(StpRevisionKind)) return (StpRevisionKind)property.GetValue(value)!;
        }
        return null;
    }
}
