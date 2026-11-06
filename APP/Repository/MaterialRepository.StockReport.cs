using APP.Services.Authorization;
using APP.Extensions;
using DOMAIN.Entities.Materials.Batch;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    public async Task<Result<MaterialStockReportDto>> GetMaterialStockReport(MaterialStockReportFilter filter, Guid userId)
    {
        // Production users report on their own department; other users choose any department or all.
        var actor = await ReportDepartmentScope.Load(context, userId);
        if (actor is null) return Error.Forbidden("Expiry.AccountUnavailable", "An active account is required to read expiry records.");
        var scope = ReportDepartmentScope.Resolve(actor, filter.DepartmentId);
        if (scope.IsFailure) return scope.Error;
        if (filter.MaterialKind.HasValue && !Enum.IsDefined(filter.MaterialKind.Value))
            return Error.Validation("StockReport.Kind", "Choose a valid material kind.");
        var departmentId = scope.Value;
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
