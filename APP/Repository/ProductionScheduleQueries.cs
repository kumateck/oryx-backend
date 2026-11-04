using DOMAIN.Entities.ProductionSchedules;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal sealed record ScopedProductionSchedules(IQueryable<ProductionSchedule> Query, Guid? DepartmentId);

internal static class ProductionScheduleQueries
{
    internal static async Task<Result<ScopedProductionSchedules>> ForActor(ApplicationDbContext context, Guid userId)
    {
        // The current account department is authoritative; role order and stale session claims cannot widen a branch.
        var actor = await context.Users.AsNoTracking().Where(user => user.Id == userId && user.DeletedAt == null && !user.IsDisabled)
            .Select(user => new { user.DepartmentId, Type = (DepartmentType?)user.Department.Type }).SingleOrDefaultAsync();
        if (actor is null) return Error.NotFound("User.NotFound", "User not found");
        if (actor.DepartmentId is null || actor.Type is null)
            return Error.Forbidden("User.Department", "Select an associated department before viewing production schedules.");
        Guid? department = actor.Type == DepartmentType.Production ? actor.DepartmentId : null;
        var query = context.ProductionSchedules.IgnoreQueryFilters().AsNoTracking().AsSplitQuery()
            .Where(schedule => schedule.DeletedAt == null && schedule.Products.Any(item => item.Product.DeletedAt == null
                && (!department.HasValue || item.Product.DepartmentId == department)))
            .Where(schedule => !department.HasValue || schedule.DepartmentId == null || schedule.DepartmentId == department)
            .Include(schedule => schedule.Products.Where(item => item.Product.DeletedAt == null
                && (!department.HasValue || item.Product.DepartmentId == department))).ThenInclude(item => item.Product)
            .Include(schedule => schedule.Products.Where(item => item.Product.DeletedAt == null
                && (!department.HasValue || item.Product.DepartmentId == department))).ThenInclude(item => item.ProductionActivity)
            .Include(schedule => schedule.Products.Where(item => item.Product.DeletedAt == null
                && (!department.HasValue || item.Product.DepartmentId == department))).ThenInclude(item => item.ProductPacking).ThenInclude(packing => packing.BasePackingUoM)
            .Include(schedule => schedule.Products.Where(item => item.Product.DeletedAt == null
                && (!department.HasValue || item.Product.DepartmentId == department))).ThenInclude(item => item.ProductPacking).ThenInclude(packing => packing.PackingLists).ThenInclude(line => line.Uom);
        return new ScopedProductionSchedules(query, department);
    }
}
