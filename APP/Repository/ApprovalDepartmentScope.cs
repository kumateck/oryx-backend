using System.Linq.Expressions;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Departments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Production departments work apart. A role of the Production type (for example a production
/// manager or warehouse supervisor) is held by people in several production departments, so a
/// stage assigned to that role is for the holders in the document's own department only.
/// Stages assigned to a person by name, roles of other types, and documents that do not belong
/// to a production department are unaffected, so cross-department approvals such as
/// procurement or QA keep working.
/// </summary>
internal sealed class ApprovalDepartmentScope
{
    internal static readonly ApprovalDepartmentScope None = new(null, [], []);
    private readonly Guid? departmentId;
    private readonly Guid userId;
    private readonly List<Guid> openRoleIds;

    internal ApprovalDepartmentScope(Guid? departmentId, List<Guid> openRoleIds, List<Guid> scopedRoleIds, Guid userId = default)
    {
        this.departmentId = departmentId;
        this.openRoleIds = openRoleIds;
        this.userId = userId;
        Active = scopedRoleIds.Count > 0;
    }

    /// <summary>True when the user holds a Production role and sits in a production department.</summary>
    internal bool Active { get; }

    internal static async Task<ApprovalDepartmentScope> For(ApplicationDbContext context, Guid userId, IReadOnlyCollection<Guid> roleIds)
    {
        if (roleIds is not { Count: > 0 }) return None;
        var home = await context.Users.AsNoTracking().Where(user => user.Id == userId)
            .Select(user => new { user.DepartmentId, Type = user.Department == null ? (DepartmentType?)null : user.Department.Type })
            .FirstOrDefaultAsync();
        if (home?.DepartmentId is null || home.Type != DepartmentType.Production) return None;
        var scoped = await context.Roles.AsNoTracking()
            .Where(role => roleIds.Contains(role.Id) && role.Type == DepartmentType.Production)
            .Select(role => role.Id).ToListAsync();
        if (scoped.Count == 0) return None;
        return new ApprovalDepartmentScope(home.DepartmentId, roleIds.Except(scoped).ToList(), scoped, userId);
    }

    /// <summary>
    /// Drops a document from another production department unless the user can reach one of its
    /// active stages directly or through a role that is not department-scoped.
    /// </summary>
    internal IQueryable<T> Apply<T, TStage>(
        IQueryable<T> query,
        Expression<Func<T, IEnumerable<TStage>>> stages,
        Expression<Func<T, Department>> department)
        where TStage : ResponsibleApprovalStage
    {
        if (!Active) return query;
        var item = stages.Parameters[0];
        var ownerDepartment = Rebind(department, item);
        var home = departmentId;
        Expression<Func<Department, bool>> foreign = owner =>
            owner != null && owner.Type == DepartmentType.Production && owner.Id != home;
        var isForeign = new Replace(foreign.Parameters[0], ownerDepartment).Visit(foreign.Body);

        var user = userId;
        var open = openRoleIds;
        Expression<Func<TStage, bool>> reachable = stage =>
            stage.Status == ApprovalStatus.Pending && stage.ActivatedAt.HasValue
            && (stage.UserId == user || stage.RoleId.HasValue && open.Contains(stage.RoleId.Value));
        var stageList = Unwrap(stages.Body);
        var any = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), [typeof(TStage)], stageList, reachable);
        return query.Where(Expression.Lambda<Func<T, bool>>(Expression.OrElse(Expression.Not(isForeign), any), item));
    }

    private static Expression Rebind<T, TValue>(Expression<Func<T, TValue>> selector, ParameterExpression item) =>
        Unwrap(new Replace(selector.Parameters[0], item).Visit(selector.Body));

    private static Expression Unwrap(Expression expression) =>
        expression is UnaryExpression { NodeType: ExpressionType.Convert } convert ? convert.Operand : expression;

    private sealed class Replace(Expression from, Expression to) : ExpressionVisitor
    {
        public override Expression Visit(Expression node) => node == from ? to : base.Visit(node);
    }
}
