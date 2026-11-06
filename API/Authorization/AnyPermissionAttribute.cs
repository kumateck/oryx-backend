using APP.Services.Authorization;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Authorization;

/// <summary>
/// For a read that several pages show (a dashboard figure that also appears on a report page): the key of any one of
/// those pages is enough. Each key still opens exactly one page; this only lists the pages that share the data. Not for
/// writes, and not as a substitute for a task's own key. Grants are read from current data just before the action.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AnyPermissionAttribute(params string[] keys) : Attribute, IAsyncActionFilter
{
    public IReadOnlyList<string> Keys { get; } = keys;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Items["Sub"] as string, out var actor)) { context.Result = new UnauthorizedResult(); return; }
        var grants = await CurrentPermissionSet.Load(http.RequestServices.GetRequiredService<ApplicationDbContext>(), actor);
        if (Keys.Any(grants.Contains)) { await next(); return; }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden, Title = "Forbidden", Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
            Extensions = { ["errors"] = new[] { new { Code = "Report.Permission", Description = $"You need one of {string.Join(", ", Keys)} to see this." } } },
        }) { StatusCode = StatusCodes.Status403Forbidden };
    }
}
