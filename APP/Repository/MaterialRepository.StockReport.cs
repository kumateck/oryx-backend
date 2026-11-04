using APP.Extensions;
using DOMAIN.Entities.Materials.Batch;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    public async Task<Result<MaterialStockReportDto>> GetMaterialStockReport(MaterialStockReportFilter filter, Guid userId)
    {
        var account = await ExpiryDepartment(userId);
        if (account.IsFailure) return account.Error;
        if (account.Value.HasValue && filter.DepartmentId.HasValue && account.Value != filter.DepartmentId)
            return Error.Forbidden("StockReport.Department", "Your account can only report stock in its assigned department.");
        if (filter.MaterialKind.HasValue && !Enum.IsDefined(filter.MaterialKind.Value))
            return Error.Validation("StockReport.Kind", "Choose a valid material kind.");
        var departmentId = account.Value ?? filter.DepartmentId;
        var department = departmentId.HasValue ? await context.Departments.IgnoreQueryFilters().AsNoTracking()
            .Where(value => value.Id == departmentId && value.DeletedAt == null)
            .Select(value => new { value.Name }).SingleOrDefaultAsync() : null;
        if (departmentId.HasValue && department == null)
            return Error.Forbidden("StockReport.Department", "The selected stock department is unavailable.");
        var now = DateTime.UtcNow;
        // UNION ALL is executed with the metadata join as one SQL statement: a position
        // moving from a shelf to a reservation cannot be counted twice between requests.
        var positions = await MaterialStockReportQuery.Rows(context, filter, departmentId, now).ToListAsync();
        var rows = positions.GroupBy(row => new { row.BatchId, row.DepartmentId, row.UoMId })
            .Select(group => { var row = group.First(); row.Quantity = group.Sum(value => value.Quantity); return row; })
            .OrderBy(row => row.MaterialName).ThenBy(row => row.MaterialCode).ThenBy(row => row.BatchNumber)
            .ThenBy(row => row.DepartmentName).ThenBy(row => row.BatchId).ThenBy(row => row.UoMId).ToList();
        return Result.Success(new MaterialStockReportDto { GeneratedAt = now, DepartmentId = departmentId,
            DepartmentName = department?.Name, MaterialKind = filter.MaterialKind, ExpiredOnly = filter.ExpiredOnly, Rows = rows });
    }
}
