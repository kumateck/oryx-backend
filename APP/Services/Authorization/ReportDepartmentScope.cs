using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Authorization;

/// <summary>
/// The one department rule for reports. A user in a production-type department (production, and
/// warehouses set up as production) reports only on their own department: a request for another
/// department is refused, and a request for "all departments" becomes their own. Everyone else
/// (non-production, R&amp;D, no department) chooses any department, or all where a report allows it.
/// </summary>
public static class ReportDepartmentScope
{
    public sealed record Actor(Guid? DepartmentId, DepartmentType? Type, string? DepartmentName)
    {
        public bool IsRestricted => Type == DepartmentType.Production && DepartmentId.HasValue;
    }

    public const string Refusal = "Production users can only report on their own department.";

    /// <summary>The account's current department, read live rather than from a possibly stale token.</summary>
    public static Task<Actor?> Load(ApplicationDbContext context, Guid userId, CancellationToken cancellationToken = default) =>
        context.Users.IgnoreQueryFilters().AsNoTracking()
            .Where(user => user.Id == userId && user.DeletedAt == null && !user.IsDisabled)
            .Select(user => new Actor(user.DepartmentId,
                user.Department == null ? null : (DepartmentType?)user.Department.Type,
                user.Department == null ? null : user.Department.Name))
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>The department to report on: the requested one, or the actor's own when they are restricted.</summary>
    public static Result<Guid?> Resolve(Actor actor, Guid? requested)
    {
        if (!actor.IsRestricted) return Result.Success(requested);
        if (requested.HasValue && requested != Guid.Empty && requested != actor.DepartmentId)
            return Error.Forbidden("Report.DepartmentScope", Refusal);
        return Result.Success(actor.DepartmentId);
    }

    /// <summary>The same rule for reports that select a department by name.</summary>
    public static Result<string?> ResolveName(Actor actor, string? requested)
    {
        if (!actor.IsRestricted) return Result.Success(requested);
        if (!string.IsNullOrWhiteSpace(requested)
            && !string.Equals(requested.Trim(), actor.DepartmentName?.Trim(), StringComparison.OrdinalIgnoreCase))
            return Error.Forbidden("Report.DepartmentScope", Refusal);
        return Result.Success(actor.DepartmentName);
    }
}
