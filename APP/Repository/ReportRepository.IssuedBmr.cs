using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class ReportRepository
{
    public async Task<Result<IssuedBmrReportDto>> GetIssuedBmrReport(
        IssuedBmrReportFilter filter, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(PermissionKeys.CanViewQcAndQaReport))
            return Error.Forbidden("Report.Permission", "QC and QA report access is required.");
        if (!filter.StartDate.HasValue || !filter.EndDate.HasValue || filter.StartDate > filter.EndDate
            || filter.EndDate == DateOnly.MaxValue || filter.DepartmentId == Guid.Empty)
            return Error.Validation("Report.Scope", "Choose a valid inclusive issue-date range and department.");
        var actor = await context.Users.IgnoreAutoIncludes().AsNoTracking()
            .Where(user => user.Id == userId && !user.IsDisabled)
            .Select(user => new { user.DepartmentId, Type = (DepartmentType?)user.Department.Type })
            .SingleOrDefaultAsync(cancellationToken);
        if (actor?.Type is null)
            return Error.Forbidden("Report.DepartmentScope", "An assigned department is required.");
        var departmentId = filter.DepartmentId;
        if (actor.Type == DepartmentType.Production)
        {
            if (departmentId.HasValue && departmentId != actor.DepartmentId)
                return Error.Forbidden("Report.DepartmentScope", "Production users can only report on their own department.");
            departmentId = actor.DepartmentId;
        }
        // Archived departments still have issued history; validate identity without dropping it.
        var department = departmentId.HasValue ? await context.Departments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.Id == departmentId && item.Type == DepartmentType.Production)
            .Select(item => new { item.Id, item.Name }).SingleOrDefaultAsync(cancellationToken) : null;
        if (departmentId.HasValue && department is null)
            return Error.NotFound("Report.Department", "The production department was not found.");
        var start = filter.StartDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusive = filter.EndDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        // IssuedDate is the issuance evidence. Completion, QC approval and later archival
        // must not remove issued BMRs or turn pending BMRs into issued ones.
        var query = context.BatchManufacturingRecords.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.IssuedDate.HasValue && item.IssuedDate >= start && item.IssuedDate < endExclusive);
        if (departmentId.HasValue)
            query = query.Where(item => item.ProductionScheduleProduct.Product.DepartmentId == departmentId);
        var rows = await query.OrderBy(item => item.IssuedDate).ThenBy(item => item.Id)
            .Select(item => new IssuedBmrReportRowDto
            {
                Id = item.Id, ScheduleProductId = item.ProductionScheduleProductId,
                ProductId = item.ProductionScheduleProduct.ProductId,
                ProductCode = item.ProductionScheduleProduct.Product.Code,
                ProductName = item.ProductionScheduleProduct.Product.Name,
                DepartmentId = item.ProductionScheduleProduct.Product.DepartmentId,
                DepartmentName = item.ProductionScheduleProduct.Product.Department.Name,
                ScheduleCode = item.ProductionScheduleProduct.ProductionSchedule.Code,
                BatchNumber = item.BatchNumber, BatchQuantity = item.BatchQuantity,
                BatchUnit = item.ProductionScheduleProduct.Product.BaseUoM.Symbol,
                PackingStyle = item.ProductionScheduleProduct.ProductPacking.Name,
                ManufacturingDate = item.ManufacturingDate, ExpiryDate = item.ExpiryDate,
                IssuedAt = item.IssuedDate.Value, IssuedById = item.IssuedById,
                IssuedByName = item.IssuedBy == null ? null :
                    ((item.IssuedBy.FirstName ?? "") + " " + (item.IssuedBy.LastName ?? "")).Trim() == ""
                        ? item.IssuedBy.UserName : ((item.IssuedBy.FirstName ?? "") + " " + (item.IssuedBy.LastName ?? "")).Trim(),
                IsArchived = item.DeletedAt.HasValue || item.ProductionScheduleProduct.Product.DeletedAt.HasValue
                    || item.ProductionScheduleProduct.Product.Department.DeletedAt.HasValue
                    || item.ProductionScheduleProduct.ProductionSchedule.DeletedAt.HasValue
                    || item.ProductionActivityStep.DeletedAt.HasValue,
                IsCancelled = item.ProductionScheduleProduct.Cancelled
            }).ToListAsync(cancellationToken);
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(PermissionKeys.CanViewQcAndQaReport))
            return Error.Forbidden("Report.Permission", "QC and QA report access is required.");
        // Do not disclose data under an assignment that changed while the report loaded.
        var current = await context.Users.IgnoreAutoIncludes().AsNoTracking()
            .Where(user => user.Id == userId && !user.IsDisabled)
            .Select(user => new { user.DepartmentId, Type = (DepartmentType?)user.Department.Type })
            .SingleOrDefaultAsync(cancellationToken);
        if (current is null || current.DepartmentId != actor.DepartmentId || current.Type != actor.Type)
            return Error.Forbidden("Report.DepartmentScope", "Your department changed. Reload before generating the report.");
        cancellationToken.ThrowIfCancellationRequested();
        return new IssuedBmrReportDto { StartDate = filter.StartDate.Value, EndDate = filter.EndDate.Value,
            DepartmentId = departmentId, DepartmentName = department?.Name, GeneratedAt = DateTime.UtcNow,
            TotalIssued = rows.Count, Rows = rows };
    }
}
