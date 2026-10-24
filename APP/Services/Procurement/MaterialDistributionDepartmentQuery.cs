using DOMAIN.Entities.Departments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Procurement;

public static class MaterialDistributionDepartmentQuery
{
    /// <summary>
    /// Load only the requesting department and the warehouse/location fields exposed by
    /// DepartmentDto. Requisition.CreatedBy auto-includes a second department hierarchy;
    /// joining both trees multiplied rows until the distribution read timed out.
    /// </summary>
    public static IQueryable<Department> ForRequisition(ApplicationDbContext context, Guid requisitionId)
        => context.Departments
            .IgnoreAutoIncludes()
            .AsNoTracking()
            .Where(department => context.Requisitions.Any(requisition =>
                requisition.Id == requisitionId && requisition.DepartmentId == department.Id))
            .Include(department => department.Warehouses)
                .ThenInclude(warehouse => warehouse.Locations)
            .AsSplitQuery()
            .TagWith("Material distribution: requesting department and warehouse locations");
}
