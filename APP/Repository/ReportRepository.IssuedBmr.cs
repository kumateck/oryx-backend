using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Products.Production;
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
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(PermissionKeys.CanViewBmrIssuedReport))
            return Error.Forbidden("Report.Permission", "BMR issued report access is required.");
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
            .Select(IssuedRow).ToListAsync(cancellationToken);
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(PermissionKeys.CanViewBmrIssuedReport))
            return Error.Forbidden("Report.Permission", "BMR issued report access is required.");
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

    /// <summary>One issued BMR as both issued-BMR reports show it.</summary>
    private static readonly System.Linq.Expressions.Expression<Func<BatchManufacturingRecord, IssuedBmrReportRowDto>> IssuedRow = item =>
        new IssuedBmrReportRowDto
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
            };

    public async Task<Result<IssuedBmrByProductReportDto>> GetIssuedBmrByProductReport(
        IssuedBmrByProductFilter filter, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(PermissionKeys.CanViewBmrIssuedByProductReport))
            return Error.Forbidden("Report.Permission", "BMR issued by product report access is required.");
        if (!filter.ProductId.HasValue || filter.ProductId == Guid.Empty || filter.DepartmentId == Guid.Empty
            || filter.StartDate.HasValue != filter.EndDate.HasValue || filter.StartDate > filter.EndDate || filter.EndDate == DateOnly.MaxValue)
            return Error.Validation("Report.Scope", "Choose a product, and either both issue dates or neither.");
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
        // Archived products and departments still have issued history.
        var product = await context.Products.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.Id == filter.ProductId)
            .Select(item => new { item.Id, item.Code, item.Name, item.DepartmentId, DepartmentName = item.Department.Name })
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null || (departmentId.HasValue && product.DepartmentId != departmentId))
            return Error.NotFound("Report.Product", "The product was not found in your department.");
        // IssuedDate is the issuance evidence; completed, released and archived BMRs stay in the list.
        var query = context.BatchManufacturingRecords.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.IssuedDate.HasValue && item.ProductionScheduleProduct.ProductId == product.Id);
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            var start = filter.StartDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endExclusive = filter.EndDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(item => item.IssuedDate >= start && item.IssuedDate < endExclusive);
        }
        var rows = await query.OrderBy(item => item.IssuedDate).ThenBy(item => item.Id).Select(IssuedRow).ToListAsync(cancellationToken);
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(PermissionKeys.CanViewBmrIssuedByProductReport))
            return Error.Forbidden("Report.Permission", "BMR issued by product report access is required.");
        // Do not disclose data under an assignment that changed while the report loaded.
        var current = await context.Users.IgnoreAutoIncludes().AsNoTracking()
            .Where(user => user.Id == userId && !user.IsDisabled)
            .Select(user => new { user.DepartmentId, Type = (DepartmentType?)user.Department.Type })
            .SingleOrDefaultAsync(cancellationToken);
        if (current is null || current.DepartmentId != actor.DepartmentId || current.Type != actor.Type)
            return Error.Forbidden("Report.DepartmentScope", "Your department changed. Reload before generating the report.");
        cancellationToken.ThrowIfCancellationRequested();
        return new IssuedBmrByProductReportDto { ProductId = product.Id, ProductCode = product.Code, ProductName = product.Name,
            StartDate = filter.StartDate, EndDate = filter.EndDate, DepartmentId = product.DepartmentId, DepartmentName = product.DepartmentName,
            GeneratedAt = DateTime.UtcNow, TotalIssued = rows.Count, Rows = rows };
    }
}
