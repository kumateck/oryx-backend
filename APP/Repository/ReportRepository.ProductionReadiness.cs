using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class ReportRepository
{
    public async Task<Result<ProductionReadinessReportDto>> GetProductionReadinessReport(
        ProductionReadinessFilter filter, Guid userId, CancellationToken cancellationToken = default)
    {
        var permissions = await CurrentPermissionSet.Load(context, userId);
        if (!permissions.Contains(PermissionKeys.CanViewQcAndQaReport))
            return Error.Forbidden("Report.Permission", "QC and QA report access is required.");
        if (!filter.DepartmentId.HasValue || filter.DepartmentId == Guid.Empty
            || !filter.StartDate.HasValue || !filter.EndDate.HasValue
            || filter.StartDate > filter.EndDate || filter.EndDate == DateOnly.MaxValue)
            return Error.Validation("Report.Scope", "Choose a production department and a valid inclusive date range.");

        var actor = await context.Users.IgnoreAutoIncludes().AsNoTracking()
            .Where(user => user.Id == userId && !user.IsDisabled)
            .Select(user => new { user.DepartmentId, Type = (DepartmentType?)user.Department.Type })
            .SingleOrDefaultAsync(cancellationToken);
        if (actor is null || actor.Type is null)
            return Error.Forbidden("Report.DepartmentScope", "An assigned department is required.");
        if (actor.Type == DepartmentType.Production && actor.DepartmentId != filter.DepartmentId)
            return Error.Forbidden("Report.DepartmentScope", "Production users can only report on their own department.");

        var department = await context.Departments.IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.Id == filter.DepartmentId && item.Type == DepartmentType.Production)
            .Select(item => new { item.Id, item.Name }).SingleOrDefaultAsync(cancellationToken);
        if (department is null)
            return Error.NotFound("Report.Department", "The production department was not found.");

        var start = filter.StartDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusive = filter.EndDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        // One projected SQL statement reads the plan, latest prices and all checks from one database
        // snapshot. Do not fan out to the product/specification endpoints or load their entity graphs.
        var source = await context.ProductionScheduleProducts.IgnoreAutoIncludes().AsNoTracking()
            .Where(item => !item.Cancelled && !item.ProductionSchedule.DeletedAt.HasValue
                && item.Product.DepartmentId == department.Id
                && item.ProductionSchedule.ScheduledStartTime < endExclusive
                && item.ProductionSchedule.ScheduledEndTime >= start)
            .Select(item => new ProductionReadinessSource
            {
                Id = item.Id, ProductId = item.ProductId,
                ProductName = item.Product.Name ?? "Unnamed product", ProductCode = item.Product.Code ?? "",
                MarketId = item.MarketTypeId, MarketName = item.MarketType.Name,
                PackingId = item.ProductPackingId, PackingName = item.ProductPacking.Name,
                PackingProductId = (Guid?)item.ProductPacking.ProductId,
                BatchSize = item.BatchSize,
                ScheduleStart = item.ProductionSchedule.ScheduledStartTime,
                ScheduleEnd = item.ProductionSchedule.ScheduledEndTime,
                ExpectedYield = (decimal?)item.ProductPacking.ExpectedYield,
                PacksPerShipper = (int?)item.ProductPacking.PackPerShipper,
                UnitPrice = item.Product.Prices.OrderByDescending(price => price.Date)
                    .Select(price => (decimal?)price.Price).FirstOrDefault(),
                PriceUnit = item.Product.Prices.OrderByDescending(price => price.Date)
                    .Select(price => price.PriceUoM).FirstOrDefault(),
                Specification = context.ProductSpecifications.Any(spec => spec.ProductId == item.ProductId),
                Stp = context.ProductStandardTestProcedures.Any(stp => stp.ProductId == item.ProductId),
                Ard = context.ProductAnalyticalRawData.Any(ard =>
                    ard.ProductStandardTestProcedure.ProductId == item.ProductId
                    && !ard.ProductStandardTestProcedure.DeletedAt.HasValue),
                Procedure = item.Product.Routes.Any(),
                Bom = item.Product.BillOfMaterials.Where(bom => bom.IsActive && !bom.BillOfMaterial.DeletedAt.HasValue)
                    .OrderByDescending(bom => bom.EffectiveDate).ThenBy(bom => bom.Id)
                    .Select(bom => bom.BillOfMaterial.Items.Any()).FirstOrDefault(),
                Packaging = item.Product.Packages.Any(),
                PackingStyle = item.Product.Packings.Any()
            }).ToListAsync(cancellationToken);

        if (source.Any(item => item.BatchSize is not (BatchSize.Full or BatchSize.Half)
            || item.ScheduleEnd < item.ScheduleStart
            || (item.PackingProductId.HasValue && item.PackingProductId != item.ProductId)))
            return Error.Conflict("Report.InvalidPlan", "A planned batch has an invalid size, schedule range or packing ownership. Correct it before generating the report.");

        // Recheck live grants after reading; a revoked report must not be disclosed.
        var refreshed = await CurrentPermissionSet.Load(context, userId);
        if (!refreshed.Contains(PermissionKeys.CanViewQcAndQaReport))
            return Error.Forbidden("Report.Permission", "QC and QA report access is required.");
        cancellationToken.ThrowIfCancellationRequested();
        return ProductionReadinessReportBuilder.Build(department.Id, department.Name,
            filter.StartDate.Value, filter.EndDate.Value, source);
    }
}
