using DOMAIN.Entities.Base;
using SHARED;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

/// <summary>
/// Who carries out each step of a product's procedure, and with which instruction and analytical raw
/// data. Used when production starts and when a running batch's procedure is repaired, so both assign
/// the same people.
/// </summary>
internal static class ProcedureAssignments
{
    internal readonly record struct Assignment(Guid UserId, int Order, Guid? ArdId, OperationAction Action);

    /// <summary>The product with its procedure: steps, resources, work centres and responsible users and roles.</summary>
    internal static Task<Product> LoadProductAsync(ApplicationDbContext context, Guid productId) =>
        context.Products.AsSplitQuery()
            .Include(product => product.Routes).ThenInclude(route => route.Resources)
            .Include(product => product.Routes).ThenInclude(route => route.WorkCenters)
            .Include(product => product.Routes).ThenInclude(route => route.ResponsibleUsers).ThenInclude(user => user.ProductAnalyticalRawData)
            .Include(product => product.Routes).ThenInclude(route => route.ResponsibleRoles).ThenInclude(role => role.ProductAnalyticalRawData)
            .FirstOrDefaultAsync(product => product.Id == productId);

    internal static async Task<List<Assignment>> BuildAsync(ApplicationDbContext context, UserManager<User> userManager, Product product)
    {
        var responsibleRoles = product.Routes.SelectMany(route => route.ResponsibleRoles)
            .Select(responsible => responsible.Role)
            .Where(role => role != null && !string.IsNullOrEmpty(role.Name))
            .DistinctBy(role => role.Id)
            .ToList();
        var roleIds = responsibleRoles.Select(role => role.Id).ToList();
        var roleDepartmentsByRoleId = (await context.RoleDepartments.Where(link => roleIds.Contains(link.RoleId)).ToListAsync())
            .ToLookup(link => link.RoleId, link => link.DepartmentId);

        var roleUsersMap = new Dictionary<string, IList<User>>();
        foreach (var role in responsibleRoles)
        {
            var usersInThisRole = await userManager.GetUsersInRoleAsync(role.Name);
            if (role.Type == DepartmentType.Production)
            {
                // Production-department roles are scoped to the product's own department.
                usersInThisRole = usersInThisRole.Where(user => user.DepartmentId == product.DepartmentId).ToList();
            }
            else
            {
                // Non-production roles (e.g. QA/QC) are scoped to the department(s) the role is associated
                // with. Without an association they are not filtered, rather than silently dropping a
                // required responsible party.
                var roleDepartmentIds = roleDepartmentsByRoleId[role.Id].ToList();
                if (roleDepartmentIds.Count > 0)
                    usersInThisRole = usersInThisRole
                        .Where(user => user.DepartmentId.HasValue && roleDepartmentIds.Contains(user.DepartmentId.Value))
                        .ToList();
            }
            roleUsersMap[role.Name] = usersInThisRole;
        }

        // Directly-assigned responsible users are an explicit choice made during procedure configuration,
        // so they are not re-filtered by department, and they take precedence over a role's instruction.
        var map = new Dictionary<(Guid userId, int order), (Guid? ardId, OperationAction action)>();
        foreach (var route in product.Routes)
        foreach (var responsible in route.ResponsibleUsers)
            map[(responsible.UserId, route.Order)] = (responsible.ProductAnalyticalRawDataId, responsible.Action);

        foreach (var route in product.Routes)
        foreach (var responsible in route.ResponsibleRoles)
        {
            if (!roleUsersMap.TryGetValue(responsible.Role?.Name ?? "", out var holders)) continue;
            foreach (var user in holders)
                map.TryAdd((user.Id, route.Order), (responsible.ProductAnalyticalRawDataId, responsible.Action));
        }

        return map.Select(entry => new Assignment(entry.Key.userId, entry.Key.order, entry.Value.ardId, entry.Value.action)).ToList();
    }
}
