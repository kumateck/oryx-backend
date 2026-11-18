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
        var density = await MaterialDensity.Load(context, materialId);
        decimal total = 0, expired = 0;
        foreach (var row in rows)
        {
            if (row.UoMId.HasValue && row.UoMId != row.MaterialBatch.UoMId)
                return Error.Conflict("Stock.Unit", "Reconcile inconsistent shelf and lot units first.");
            var value = MaterialDensity.Convert(row.Quantity, row.MaterialBatch.UoM.Symbol, unit.Symbol, density);
            if (!value.HasValue) return Error.Conflict("Stock.Density", "Set a verified material density before combining mass and volume stock.");
            var invalid = MaterialValidity.IsExpired(row.MaterialBatch, DateTime.UtcNow)
                || MaterialValidity.IsExpiredDate(row.MaterialBatch.RetestDate, DateTime.UtcNow);
            if (!usable || !invalid && row.MaterialBatch.Status == BatchStatus.Available) total += value.Value;
            if (invalid) expired += value.Value;
        }
        if (usable)
        {
            var ids = rows.Select(row => row.MaterialBatchId).ToList();
            var held = await ProductionMaterialAllocator.UnissuedReservations(context).Include(r => r.UoM)
                .Where(r => ids.Contains(r.MaterialBatchId)).ToListAsync();
            foreach (var reservation in held)
            {
                var quantity = MaterialDensity.Convert(reservation.Quantity, reservation.UoM.Symbol, unit.Symbol, density);
                if (!quantity.HasValue) return Error.Conflict("Stock.Density", "Reservation units cannot be converted to this warehouse's unit.");
                total -= quantity.Value;
            }
        }
        return new MaterialWarehouseStockBreakdown { WarehouseStock = Math.Max(0, total), ExpiredQuantity = Math.Max(0, expired) };
    }
}
