using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    private async Task<Result<MaterialWarehouseStockBreakdown>> WarehouseShelfTotals(Guid materialId, Guid warehouseId, bool usable)
    {
        var department = await context.Warehouses.IgnoreQueryFilters().Where(w => w.Id == warehouseId && w.DeletedAt == null)
            .Select(w => w.DepartmentId).SingleOrDefaultAsync();
        var links = await context.MaterialDepartments.IgnoreQueryFilters().Include(link => link.UoM)
            .Where(link => link.MaterialId == materialId && link.DepartmentId == department && link.DeletedAt == null).ToListAsync();
        if (links.Count > 1) return Error.Conflict("Stock.Unit", "Reconcile duplicate department material units first.");
        var rows = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Include(row => row.MaterialBatch).ThenInclude(batch => batch.UoM)
            .Where(row => row.MaterialBatch.MaterialId == materialId && row.DeletedAt == null && row.MaterialBatch.DeletedAt == null
                && row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == warehouseId).ToListAsync();
        var unit = links.SingleOrDefault()?.UoM;
        unit ??= rows.Select(row => row.MaterialBatch.UoM).DistinctBy(u => u.Id).Count() == 1 ? rows[0].MaterialBatch.UoM : null;
        if (rows.Count == 0) return new MaterialWarehouseStockBreakdown();
        if (unit is null || unit.DeletedAt.HasValue) return Error.Conflict("Stock.Unit", "Choose an active stock unit on the material's department link.");
        if (rows.Any(row => row.UoMId.HasValue && row.UoMId != row.MaterialBatch.UoMId))
            return Error.Conflict("Stock.Unit", "Reconcile inconsistent shelf and lot units first.");
        var density = links.Count == 1 ? MaterialDensity.Verified(links[0]) ?? await MaterialDensity.Load(context, materialId) : await MaterialDensity.Load(context, materialId);
        if (rows.Any(row => MaterialDensity.Convert(row.Quantity, row.MaterialBatch.UoM.Symbol, unit.Symbol, density) is null))
            return Error.Conflict("Stock.Density", "Set a verified material density before combining mass and volume stock.");
        // One rule for every screen: see StoreStock.
        var level = (await StoreStock.Measure(context, warehouseId, new Dictionary<Guid, (string?, decimal?)> { [materialId] = (unit.Symbol, density) }))
            .GetValueOrDefault(materialId, StoreStockLevel.None);
        var total = usable ? level.Free : level.OnHand;
        var expired = level.Expired;
        return new MaterialWarehouseStockBreakdown { WarehouseStock = Math.Max(0, total), ExpiredQuantity = Math.Max(0, expired),
            OnHandQuantity = level.OnHand, UnavailableQuantity = level.Unavailable, ReservedQuantity = level.Held, FreeQuantity = level.Free };
    }
}
