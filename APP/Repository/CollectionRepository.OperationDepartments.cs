using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Departments;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// An operation belongs to at most one production department (<c>Operation.DepartmentId</c>); procedure pickers and the
/// production activity board list a department's operations from it. Linking and unlinking are separate tasks, and a
/// linked operation must be unlinked before it can be linked elsewhere, so a move needs both keys.
/// </summary>
public partial class CollectionRepository
{
    public async Task<Result> LinkOperationDepartment(Guid operationId, Guid departmentId, Guid userId)
    {
        var operation = await context.Operations.FirstOrDefaultAsync(item => item.Id == operationId);
        if (operation is null) return Error.NotFound("Operation.NotFound", "The operation was not found.");

        var department = await context.Departments.AsNoTracking()
            .Where(item => item.Id == departmentId && item.DeletedAt == null)
            .Select(item => new { item.Id, item.Name, item.Type })
            .FirstOrDefaultAsync();
        if (department is null) return Error.NotFound("Department.NotFound", "The department was not found.");
        if (department.Type != DepartmentType.Production)
            return Error.Validation("Operation.Department", "Operations can only be linked to a production department.");

        if (operation.DepartmentId == department.Id) return Result.Success();
        if (operation.DepartmentId.HasValue)
        {
            var current = await context.Departments.IgnoreQueryFilters().AsNoTracking()
                .Where(item => item.Id == operation.DepartmentId).Select(item => item.Name).FirstOrDefaultAsync();
            return Error.Conflict("Operation.Department",
                $"“{operation.Name}” is already linked to {current ?? "another department"}. Unlink it first.");
        }

        var name = operation.Name.Trim().ToLower();
        var duplicate = await context.Operations.AnyAsync(item =>
            item.Id != operation.Id && item.DepartmentId == department.Id && item.Name.Trim().ToLower() == name);
        if (duplicate)
            return Error.Conflict("Operation.Department", $"{department.Name} already has an operation named “{operation.Name}”.");

        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(PermissionKeys.CanLinkOperationDepartment))
            return Error.Forbidden("Operation.Permission", "You need CanLinkOperationDepartment for this.");

        operation.DepartmentId = department.Id;
        operation.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UnlinkOperationDepartment(Guid operationId, Guid userId)
    {
        var operation = await context.Operations.FirstOrDefaultAsync(item => item.Id == operationId);
        if (operation is null) return Error.NotFound("Operation.NotFound", "The operation was not found.");
        if (!operation.DepartmentId.HasValue) return Result.Success();

        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(PermissionKeys.CanUnlinkOperationDepartment))
            return Error.Forbidden("Operation.Permission", "You need CanUnlinkOperationDepartment for this.");

        operation.DepartmentId = null;
        operation.LastUpdatedById = userId;
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
