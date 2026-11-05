using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
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
                && (!department.HasValue || item.Product.DepartmentId == department))).ThenInclude(item => item.ProductPacking).ThenInclude(packing => packing.BasePackingUoM)
            .Include(schedule => schedule.Products.Where(item => item.Product.DeletedAt == null
                && (!department.HasValue || item.Product.DepartmentId == department))).ThenInclude(item => item.ProductPacking).ThenInclude(packing => packing.PackingLists).ThenInclude(line => line.Uom);
        return new ScopedProductionSchedules(query, department);
    }

    /// <summary>
    /// Sets each batch's production run to its live run, or none. The schedule query ignores query filters,
    /// so including the run there would also bring back runs archived by an unstart repair and show the
    /// batch as still in production.
    /// </summary>
    internal static async Task AttachLiveActivities(ApplicationDbContext context, IReadOnlyCollection<ProductionSchedule> schedules)
    {
        var batches = schedules.SelectMany(schedule => schedule.Products).ToList();
        var ids = batches.Select(batch => batch.Id).Distinct().ToList();
        // The query filter hides archived runs; a unique index allows one live run per batch.
        var live = await context.ProductionActivities.AsNoTracking().IgnoreAutoIncludes()
            .Where(activity => ids.Contains(activity.ProductionScheduleProductId))
            .Select(activity => new { activity.Id, activity.ProductionScheduleProductId })
            .ToDictionaryAsync(activity => activity.ProductionScheduleProductId, activity => activity.Id);
        foreach (var batch in batches)
            batch.ProductionActivity = live.TryGetValue(batch.Id, out var id)
                ? new ProductionActivity { Id = id, ProductionScheduleProductId = batch.Id }
                : null;
    }
}
