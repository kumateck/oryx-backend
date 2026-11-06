using System.Reflection;
using APP.Services.Authorization;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using SHARED;
using SHARED.Services.Identity;

namespace API.Filters;

/// <summary>Applies the report department rule to every endpoint marked <see cref="ReportDepartmentScopeAttribute"/>.</summary>
public sealed class ReportDepartmentScopeFilter(ApplicationDbContext database, ICurrentUserService user) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = (context.ActionDescriptor as ControllerActionDescriptor)?.MethodInfo;
        if (method?.GetCustomAttribute<ReportDepartmentScopeAttribute>(true) is null || user.UserId is not Guid userId)
        { await next(); return; }
        var actor = await ReportDepartmentScope.Load(database, userId, context.HttpContext.RequestAborted);
        if (actor is null || !actor.IsRestricted) { await next(); return; }
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            // An omitted optional parameter is absent from the arguments; it still means "all departments".
            context.ActionArguments.TryGetValue(parameter.Name, out var value);
            var error = Scope(actor, parameter.Name, parameter.ParameterType, ref value);
            if (error is not null) { context.Result = Refused(error); return; }
            if (value is not null) context.ActionArguments[parameter.Name] = value;
        }
        await next();
    }

    private static Error? Scope(ReportDepartmentScope.Actor actor, string name, Type type, ref object? value)
    {
        if (IsDepartmentId(name, type))
        {
            var resolved = ReportDepartmentScope.Resolve(actor, value as Guid?);
            if (resolved.IsFailure) return resolved.Error;
            value = resolved.Value;
            return null;
        }
        if (string.Equals(name, "departmentName", StringComparison.OrdinalIgnoreCase) && type == typeof(string))
        {
            var resolved = ReportDepartmentScope.ResolveName(actor, value as string);
            if (resolved.IsFailure) return resolved.Error;
            value = resolved.Value;
            return null;
        }
        var property = type.IsClass && type != typeof(string)
            ? type.GetProperty("DepartmentId", BindingFlags.Public | BindingFlags.Instance) : null;
        if (property is null || !property.CanWrite || !IsDepartmentId(property.Name, property.PropertyType)) return null;
        value ??= type.GetConstructor(Type.EmptyTypes)?.Invoke(null);
        if (value is null) return null;
        var scoped = ReportDepartmentScope.Resolve(actor, property.GetValue(value) as Guid?);
        if (scoped.IsFailure) return scoped.Error;
        property.SetValue(value, scoped.Value);
        return null;
    }

    private static bool IsDepartmentId(string name, Type type) =>
        string.Equals(name, "departmentId", StringComparison.OrdinalIgnoreCase) && (type == typeof(Guid?) || type == typeof(Guid));

    private static ObjectResult Refused(Error error) => new(new ProblemDetails
    { Status = 403, Title = "Department not allowed", Detail = error.Description }) { StatusCode = 403 };
}
