using System.Data;
using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Reports.Warehouse;
using DOMAIN.Entities.Materials.Batch;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class MaterialRepository
{
    public async Task<Result<List<ReservedMaterialReportDto>>> GetReservedQuantitiesForMaterial(Guid materialId, Guid? departmentId)
    {
        await using var snapshot = context.Database.IsRelational() && context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead) : null;
        var unissued = ProductionMaterialAllocator.UnissuedReservations(context).IgnoreAutoIncludes()
            .Where(row => row.MaterialBatch.MaterialId == materialId);
        if (departmentId.HasValue)
            unissued = unissued.Where(row => row.WarehouseLocationShelfId.HasValue
                ? row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId == departmentId
                : row.Warehouse.DepartmentId == departmentId);
        var source = await unissued.AsNoTracking().SelectMaterialDate(row => row.MaterialBatch, (row, date) => new
        {
            row.MaterialBatchId,
            DepartmentId = row.WarehouseLocationShelfId.HasValue
                ? row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId : row.Warehouse.DepartmentId,
            Row = new ReservedMaterialReportDto
            {
                RackName = row.WarehouseLocationShelf == null ? null : row.WarehouseLocationShelf.WarehouseLocationRack.Name,
                ShelfName = row.WarehouseLocationShelf == null ? null : row.WarehouseLocationShelf.Name ?? row.WarehouseLocationShelf.Code,
                MaterialName = row.MaterialBatch.Material.Name, MaterialCode = row.MaterialBatch.Material.Code,
                ProductName = row.ProductionScheduleProduct.Product.Name, ProductCode = row.ProductionScheduleProduct.Product.Code,
                ReservedQuantity = row.Quantity, UomSymbol = row.UoM.Symbol,
                WarehouseName = row.Warehouse.Name, DepartmentName = row.Warehouse.Department.Name,
                DateTime = row.CreatedAt, Schedule = row.ProductionScheduleProduct.ProductionSchedule.Code,
                ProductBatchNumber = context.BatchManufacturingRecords.Where(record => record.ProductionScheduleProductId == row.ProductionScheduleProductId)
                    .Select(record => record.BatchNumber).FirstOrDefault() ?? row.ProductionScheduleProduct.BatchNumber,
                ArNumber = context.MaterialSamplings.Where(sample => sample.MaterialBatchId == row.MaterialBatchId)
                    .OrderByDescending(sample => sample.CreatedAt).Select(sample => sample.ArNumber).FirstOrDefault(),
                ManufacturingDate = row.MaterialBatch.ManufacturingDate, ExpiryDate = row.MaterialBatch.ExpiryDate,
                EffectiveExpiryDate = date, MaterialBatchNumber = row.MaterialBatch.BatchNumber,
            }
        }).ToListAsync();
        var ids = source.Select(row => row.MaterialBatchId).Distinct().ToList();
        var shelves = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(row => ids.Contains(row.MaterialBatchId) && row.DeletedAt == null && row.MaterialBatch.DeletedAt == null
                && (!departmentId.HasValue || row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId == departmentId))
            .Select(row => new { row.MaterialBatchId, row.Quantity, Unit = row.MaterialBatch.UoM.Symbol,
                DepartmentId = row.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId }).ToListAsync();
        var links = await context.MaterialDepartments.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(row => row.MaterialId == materialId && row.DeletedAt == null && row.UoM.DeletedAt == null)
            .Select(row => new { row.DepartmentId, Unit = row.UoM.Symbol }).ToListAsync();
        var density = await MaterialDensity.Load(context, materialId);
        foreach (var item in source)
        {
            var units = links.Where(row => row.DepartmentId == item.DepartmentId).Select(row => row.Unit).Distinct().ToList();
            if (units.Count > 1) return Error.Conflict("ReservedStock.Unit", "Reconcile duplicate department stock units before viewing reservations.");
            var target = units.SingleOrDefault() ?? item.Row.UomSymbol;
            var held = source.Where(row => row.MaterialBatchId == item.MaterialBatchId && row.DepartmentId == item.DepartmentId)
                .Select(row => MaterialDensity.Convert(row.Row.ReservedQuantity, row.Row.UomSymbol, target, density)).ToList();
            var physical = shelves.Where(row => row.MaterialBatchId == item.MaterialBatchId && row.DepartmentId == item.DepartmentId)
                .Select(row => MaterialDensity.Convert(row.Quantity, row.Unit, target, density)).ToList();
            var quantity = MaterialDensity.Convert(item.Row.ReservedQuantity, item.Row.UomSymbol, target, density);
            if (quantity is null || held.Any(row => row is null) || physical.Any(row => row is null))
                return Error.Conflict("ReservedStock.Density", "Record a verified material density to reconcile mass and volume stock units.");
            // Do not read an unrestricted PostgreSQL numeric SUM into System.Decimal: full-precision density
            // fractions can exceed its scale even though every quantity and the rounded total fit.
            // Aggregate the individual decimal quantities in the same unit, using the stock-screen rule.
            item.Row.BalanceQuantity = physical.Sum(row => row!.Value) - held.Sum(row => row!.Value);
        }
        // Convert only after every balance is measured; changing source rows early would mix units in later sums.
        foreach (var item in source)
        {
            var target = links.Where(row => row.DepartmentId == item.DepartmentId).Select(row => row.Unit).Distinct().SingleOrDefault() ?? item.Row.UomSymbol;
            item.Row.ReservedQuantity = MaterialDensity.Convert(item.Row.ReservedQuantity, item.Row.UomSymbol, target, density)!.Value;
            item.Row.UomSymbol = target;
        }
        if (snapshot is not null) await snapshot.CommitAsync();
        return source.Select(row => row.Row).ToList();
    }
}
