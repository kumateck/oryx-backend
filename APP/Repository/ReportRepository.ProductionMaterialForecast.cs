using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Reports;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class ReportRepository
{
    public async Task<Result<ProductionMaterialForecastDto>> GetProductionMaterialForecast(
        ProductionMaterialForecastFilter filter, Guid userId, CancellationToken cancellationToken = default)
    {
        var scope = await ForecastScope(userId, cancellationToken);
        if (scope.IsFailure) return scope.Error;
        var report = await ProductionMaterialForecasts.Build(context, scope.Value, filter?.Year ?? 0, filter?.Month ?? 0, cancellationToken);
        if (report.IsFailure) return report.Error;
        // Do not disclose the report if the grant or department changed while it was built.
        var current = await ForecastScope(userId, cancellationToken);
        if (current.IsFailure) return current.Error;
        if (current.Value != scope.Value)
            return Error.Forbidden("Forecast.DepartmentScope", "Your department changed. Reload before generating the forecast.");
        return report;
    }

    private async Task<Result<Guid>> ForecastScope(Guid userId, CancellationToken cancellationToken)
    {
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(PermissionKeys.CanViewProductionScheduleMaterialForecast))
            return Error.Forbidden("Forecast.Permission", "You do not have permission to view the production material forecast.");
        var departmentId = await context.Users.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(user => user.Id == userId && user.DeletedAt == null && !user.IsDisabled)
            .Select(user => user.DepartmentId).SingleOrDefaultAsync(cancellationToken);
        return departmentId.HasValue ? departmentId.Value
            : Error.Forbidden("Forecast.DepartmentScope", "An assigned department is required to view the forecast.");
    }
}
