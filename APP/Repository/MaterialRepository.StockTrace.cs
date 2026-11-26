using System.Data;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    public async Task<Result<MaterialStockTraceDto>> GetMaterialStockTrace(Guid materialId, MaterialKind kind,
        Guid departmentId, DateTime? since, Guid userId, bool forPrint = false)
    {
        var key = forPrint
            ? kind == MaterialKind.Raw ? PermissionKeys.CanPrintRawMaterialStockTrace : PermissionKeys.CanPrintPackagingMaterialStockTrace
            : kind == MaterialKind.Raw ? PermissionKeys.CanViewRawMaterialStockTrace : PermissionKeys.CanViewPackagingMaterialStockTrace;
        var actor = await context.Users.AsNoTracking().Where(user => user.Id == userId && !user.IsDisabled)
            .Select(user => new { user.DepartmentId }).SingleOrDefaultAsync();
        if (actor?.DepartmentId != departmentId || departmentId == Guid.Empty)
            return Error.Forbidden("StockTrace.Department", "Read stock in your own department.");
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(key))
            return Error.Forbidden("StockTrace.Permission", "You do not have permission to trace this material kind.");
        var material = await context.Materials.AsNoTracking().SingleOrDefaultAsync(row => row.Id == materialId && row.DeletedAt == null);
        if (material is null) return Error.NotFound("StockTrace.Material", "Material not found.");
        if (material.Kind != kind) return Error.Forbidden("StockTrace.Kind", "Use the stock trace permission for the material's stored kind.");
        var now = DateTime.UtcNow;
        var start = since.HasValue ? DateTime.SpecifyKind(since.Value.Date, DateTimeKind.Utc) : new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        if (start > now) return Error.Validation("StockTrace.Period", "The period must start today or earlier.");
        await using var snapshot = context.Database.IsRelational() && context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead) : null;
        var result = await MaterialStockTraceQuery.Load(context, material, departmentId, start, now);
        if (!(await CurrentPermissionSet.Load(context, userId)).Contains(key)
            || !await context.Users.AnyAsync(user => user.Id == userId && !user.IsDisabled && user.DepartmentId == departmentId))
            return Error.Forbidden("StockTrace.Permission", "Your stock trace access or department changed. Reload your access.");
        if (snapshot is not null) await snapshot.CommitAsync();
        return result;
    }
}
