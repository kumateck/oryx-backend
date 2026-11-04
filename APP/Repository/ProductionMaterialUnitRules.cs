using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions.Request;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class ProductionMaterialUnitRules
{
    internal static async Task<Result> ValidateStock(ApplicationDbContext context, Guid batchId, List<CreateRequisitionItemRequest> items)
    {
        var ids = await context.Materials.IgnoreQueryFilters().Where(material => material.Kind == MaterialKind.Raw
            && items.Select(item => item.MaterialId).Contains(material.Id)).Select(material => material.Id).ToListAsync();
        if (ids.Count == 0) return Result.Success();
        var batch = await context.ProductionScheduleProducts.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(batch => batch.Id == batchId).Select(batch => new { batch.Product.DepartmentId, batch.ProductId }).SingleOrDefaultAsync();
        if (batch?.DepartmentId is null)
            return Error.Conflict("Requisition.MaterialUnit", "The batch needs a production department before its material units can be verified.");
        var links = await context.MaterialDepartments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(link => link.UoM).Where(link => ids.Contains(link.MaterialId) && link.DepartmentId == batch.DepartmentId
                && link.DeletedAt == null).ToListAsync();
        var snapshot = await context.ProductionMaterialConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(configuration => configuration.ProductionScheduleProductId == batchId && configuration.Kind == MaterialKind.Raw);
        var requirements = snapshot is null ? [] : ProductionMaterialSources.Requirements(snapshot.RequirementsJson);
        var reserved = await context.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(row => row.ProductionScheduleProductId == batchId && ids.Contains(row.MaterialBatch.MaterialId)
                && row.DeletedAt == null && row.ReleasedAt == null)
            .Select(row => new { row.MaterialBatch.MaterialId, row.UoMId, row.Quantity }).ToListAsync();
        var bom = await context.ProductBillOfMaterials.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(bom => bom.ProductId == batch.ProductId && bom.IsActive && bom.DeletedAt == null)
            .OrderByDescending(bom => bom.EffectiveDate).Select(bom => bom.BillOfMaterialId).FirstOrDefaultAsync();
        var definitions = await context.BillOfMaterialItems.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(item => item.BillOfMaterialId == bom && ids.Contains(item.MaterialId) && item.DeletedAt == null)
            .Select(item => new { item.MaterialId, Unit = item.BaseUoM.Symbol }).ToListAsync();
        foreach (var materialId in ids)
        {
            var matches = links.Where(link => link.MaterialId == materialId).ToList();
            if (matches.Count != 1 || matches[0].UoM is null || matches[0].UoM.DeletedAt.HasValue)
                return Error.Conflict("Requisition.MaterialUnit", "Set one valid unit on the department material linkage before repairing the BOM and requesting stock.");
            var unitId = matches[0].UoMId;
            if (items.Any(item => item.MaterialId == materialId && item.UomId != unitId)
                || requirements.Any(item => item.MaterialId == materialId && item.UnitId != unitId)
                || reserved.Any(item => item.MaterialId == materialId && item.UoMId != unitId)
                || snapshot is null && definitions.Any(item => item.MaterialId == materialId
                    && UomConverter.Convert(1, item.Unit, matches[0].UoM.Symbol) is null)
                || snapshot is null && reserved.Any(row => row.MaterialId == materialId)
                    && items.Any(item => item.MaterialId == materialId && item.Quantity != reserved.Where(row => row.MaterialId == materialId).Sum(row => row.Quantity)))
                return Error.Conflict("Requisition.BomRepairRequired", "The batch's raw-material units no longer match the department material linkage. Save the corrected product BOM, apply Repair BOM on this production schedule, then reload before requesting or issuing stock.");
        }
        return Result.Success();
    }
}
