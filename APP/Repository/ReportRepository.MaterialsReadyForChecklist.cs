using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class ReportRepository
{
    public async Task<Result<IReadOnlyList<MaterialReadyForChecklistDto>>>
        GetMaterialsReadyForChecklist(
            MaterialsReadyForChecklistFilter filter,
            Guid userId,
            CancellationToken cancellationToken = default
        )
    {
        var userScope = await context.Users
            .IgnoreAutoIncludes()
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.DepartmentId,
                DepartmentType = (DepartmentType?)user.Department.Type
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (userScope is null)
            return UserErrors.NotFound(userId);

        if (!userScope.DepartmentId.HasValue || !userScope.DepartmentType.HasValue)
        {
            return Error.Validation(
                "Report.DepartmentScope",
                "The signed-in user must belong to a department to run this report."
            );
        }

        if (filter.StartDate.HasValue && filter.EndDate.HasValue
            && filter.StartDate.Value > filter.EndDate.Value)
        {
            return Error.Validation(
                "Report.DateRange",
                "Start date cannot be later than end date."
            );
        }

        var departmentIdResult = await ResolveChecklistDepartmentScope(
            filter,
            userScope.DepartmentId.Value,
            userScope.DepartmentType.Value,
            cancellationToken
        );
        if (!departmentIdResult.IsSuccess)
            return departmentIdResult.Errors;

        var departmentId = departmentIdResult.Value.DepartmentId;
        var query = context.DistributedRequisitionMaterials
            .IgnoreAutoIncludes()
            .AsNoTracking()
            .Where(material =>
                material.Status == DistributedRequisitionMaterialStatus.Pending
                && material.WarehouseArrivalLocationId.HasValue
                && material.WarehouseArrivalLocation.Warehouse.Department.Type
                    == DepartmentType.Production
                && (material.WarehouseArrivalLocation.Warehouse.Type
                    == WarehouseType.RawMaterialStorage
                    || material.WarehouseArrivalLocation.Warehouse.Type
                    == WarehouseType.PackagedStorage)
            );

        if (departmentId.HasValue)
        {
            query = query.Where(material =>
                material.WarehouseArrivalLocation.Warehouse.DepartmentId == departmentId.Value
            );
        }

        if (filter.MaterialKind.HasValue)
        {
            var warehouseType = filter.MaterialKind.Value == MaterialKind.Raw
                ? WarehouseType.RawMaterialStorage
                : WarehouseType.PackagedStorage;
            query = query.Where(material =>
                material.WarehouseArrivalLocation.Warehouse.Type == warehouseType
            );
        }

        if (filter.StartDate.HasValue)
            query = query.Where(material => material.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(material => material.CreatedAt <= filter.EndDate.Value);

        var rows = await query
            .OrderBy(material => material.WarehouseArrivalLocation.Warehouse.Department.Name)
            .ThenByDescending(material => material.CreatedAt)
            .ThenBy(material => material.Id)
            .Select(material => new MaterialReadyForChecklistDto
            {
                Id = material.Id,
                MaterialId = material.MaterialId,
                MaterialCode = material.Material.Code,
                MaterialName = material.Material.Name,
                MaterialKind = material.Material.Kind,
                ShipmentInvoiceId = material.ShipmentInvoiceId,
                ShipmentInvoiceCode = material.ShipmentInvoice.Code,
                Quantity = material.Quantity,
                UoMId = material.UoMId,
                UoMName = material.UoM.Name,
                UoMSymbol = material.UoM.Symbol,
                ArrivedAt = material.ArrivedAt,
                CreatedAt = material.CreatedAt,
                Status = material.Status,
                DepartmentId = material.WarehouseArrivalLocation.Warehouse.DepartmentId,
                DepartmentName = material.WarehouseArrivalLocation.Warehouse.Department.Name
            })
            .ToListAsync(cancellationToken);

        return rows;
    }

    private async Task<Result<ChecklistDepartmentScope>> ResolveChecklistDepartmentScope(
        MaterialsReadyForChecklistFilter filter,
        Guid userDepartmentId,
        DepartmentType userDepartmentType,
        CancellationToken cancellationToken
    )
    {
        if (userDepartmentType == DepartmentType.Production)
        {
            if (filter.DepartmentId.HasValue && filter.DepartmentId.Value != userDepartmentId)
            {
                return Error.Validation(
                    "Report.DepartmentScope",
                    "Production users can only run this report for their own department."
                );
            }

            return new ChecklistDepartmentScope(userDepartmentId);
        }

        if (!filter.DepartmentId.HasValue)
            return new ChecklistDepartmentScope(null);

        var isProductionDepartment = await context.Departments
            .IgnoreAutoIncludes()
            .AsNoTracking()
            .AnyAsync(
                department =>
                    department.Id == filter.DepartmentId.Value
                    && department.Type == DepartmentType.Production,
                cancellationToken
            );

        return isProductionDepartment
            ? new ChecklistDepartmentScope(filter.DepartmentId.Value)
            : Error.Validation(
                "Report.DepartmentScope",
                "The selected department must be an active production department."
            );
    }

    private sealed record ChecklistDepartmentScope(Guid? DepartmentId);
}
