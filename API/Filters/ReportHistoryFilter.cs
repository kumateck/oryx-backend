using System.Globalization;
using APP.Services.Authorization;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Reporting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using SHARED.Services.Identity;

namespace API.Filters;

public sealed class ReportHistoryFilter(ApplicationDbContext database, ICurrentUserService user) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var metadata = (context.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo
            .GetCustomAttributes(typeof(ReportHistoryAttribute), true).OfType<ReportHistoryAttribute>().SingleOrDefault();
        if (!context.HttpContext.Request.Query.TryGetValue("asOfDate", out var value) || string.IsNullOrWhiteSpace(value))
        {
            if (metadata?.RequireForCurrent == true)
            {
                var current = await CurrentPermissionSet.Load(database,user.UserId);
                if (!metadata.Permissions.Any(current.Contains)) { context.Result = new StatusCodeResult(403); return; }
                var result = await next();
                var refreshed = await CurrentPermissionSet.Load(database,user.UserId);
                if (!metadata.Permissions.Any(refreshed.Contains)) result.Result = new StatusCodeResult(403);
            }
            else await next();
            return;
        }

        if (metadata is null || !HttpMethods.IsGet(context.HttpContext.Request.Method))
        { context.Result = Problem(400, "This operation does not support historical reporting."); return; }
        // Authorize against live grants before creating any temporal views. No cached role/admin bypass.
        var permissions = await CurrentPermissionSet.Load(database, user.UserId);
        if (!metadata.Permissions.Any(permissions.Contains)) { context.Result = new StatusCodeResult(403); return; }
        if (value.Count != 1 || !DateTimeOffset.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var requested))
        { context.Result = Problem(400, "Choose a valid historical date."); return; }
        DateTime cutoff;
        try { cutoff = ReportHistoryScope.Cutoff(requested, DateTime.UtcNow); }
        catch (ArgumentException error) { context.Result = Problem(400, error.Message); return; }
        try
        {
            ActionExecutedContext executed;
            await using (await ReportHistoryScope.Open(database, cutoff, context.HttpContext.RequestAborted))
            {
                context.HttpContext.Response.Headers["X-Oryx-Report-As-Of"] = cutoff.ToString("O");
                executed = await next();
            }
            if (executed.Exception is ReportHistoryUnavailableException unavailable)
            { executed.ExceptionHandled = true; executed.Result = Problem(409, unavailable.Message, unavailable.AvailableFrom); return; }
            // A revoked permission while the snapshot was read also prevents disclosure of the result.
            var refreshed = await CurrentPermissionSet.Load(database, user.UserId);
            if (!metadata.Permissions.Any(refreshed.Contains)) executed.Result = new StatusCodeResult(403);
        }
        catch (ReportHistoryUnavailableException error) { context.Result = Problem(409, error.Message, error.AvailableFrom); }
    }
    private static ObjectResult Problem(int status, string detail, DateTime? availableFrom = null)
    {
        var problem = new ProblemDetails { Status = status, Title = status == 409 ? "Historical report unavailable" : "Invalid report date", Detail = detail };
        if (availableFrom.HasValue) problem.Extensions["availableFrom"] = availableFrom.Value;
        return new ObjectResult(problem) { StatusCode = status };
    }
}
