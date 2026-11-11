using APP.Extensions;
using APP.Services.Materials;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

internal static class MaterialStockReportQuery
{
    internal static IQueryable<MaterialStockReportRow> Rows(ApplicationDbContext context, MaterialStockReportFilter filter, Guid? departmentId, DateTime now)
    {
        var shelves = context.ShelfMaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.DeletedAt == null && item.Quantity > 0
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DeletedAt == null
                && (item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Department == null || item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Department.DeletedAt == null))
            .Select(item => new {
                item.MaterialBatchId, DepartmentId = item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId,
                DepartmentName = item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Department != null && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Department.DeletedAt == null
                    ? item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Department.Name : null,
                UoMId = item.UoMId ?? item.MaterialBatch.UoMId,
                UoM = item.UoM != null ? item.UoM.Symbol : item.MaterialBatch.UoM.Symbol, item.Quantity,
            });
        var unissued = ProductionMaterialAllocator.UnissuedReservations(context);
        var reserved = context.MaterialBatchReservedQuantities.IgnoreQueryFilters().AsNoTracking()
            .Where(item => item.DeletedAt == null && item.ReleasedAt == null && item.Quantity > 0 && !unissued.Any(hold => hold.Id == item.Id) && item.Warehouse.DeletedAt == null
                && (item.Warehouse.Department == null || item.Warehouse.Department.DeletedAt == null))
            .Select(item => new {
                item.MaterialBatchId, item.Warehouse.DepartmentId,
                DepartmentName = item.Warehouse.Department != null && item.Warehouse.Department.DeletedAt == null ? item.Warehouse.Department.Name : null,
                UoMId = item.UoM != null ? item.UoMId : item.MaterialBatch.UoMId,
                UoM = item.UoM != null ? item.UoM.Symbol : item.MaterialBatch.UoM.Symbol, item.Quantity,
            });
        var positions = shelves.Concat(reserved);
        if (departmentId.HasValue) positions = positions.Where(item => item.DepartmentId == departmentId);
        var batches = context.MaterialBatches.IgnoreQueryFilters().AsNoTracking()
            .Where(batch => batch.DeletedAt == null && batch.Material.DeletedAt == null);
        if (filter.MaterialKind.HasValue) batches = batches.Where(batch => batch.Material.Kind == filter.MaterialKind);
        batches = filter.ExpiredOnly
            ? batches.WhereMaterialDate(batch => batch, date => date.HasValue && date.Value != DateTime.MinValue && date <= now)
            : batches.WhereMaterialDate(batch => batch, date => date == null || date == DateTime.MinValue || date > now);
        var corrections = MigrationBatchReferences.ApplicableCorrections(context);
        return from position in positions join batch in batches on position.MaterialBatchId equals batch.Id
            select new MaterialStockReportRow {
                BatchId = batch.Id, DepartmentId = position.DepartmentId, DepartmentName = position.DepartmentName,
                UoMId = position.UoMId, MaterialName = batch.Material.Name, MaterialCode = batch.Material.Code,
                BatchNumber = batch.BatchNumber,
                GrnNumber = batch.Grn != null ? batch.Grn.GrnNumber : batch.MigrationGrnNumber,
                ArNumber = corrections.Where(change => change.MaterialBatchId == batch.Id)
                    .OrderByDescending(change => change.OccurredAt).ThenByDescending(change => change.Id).Select(change => change.NewArNumber).FirstOrDefault()
                    ?? context.MaterialSamplings.Where(sample => sample.MaterialBatchId == batch.Id && sample.DeletedAt == null)
                        .OrderBy(sample => sample.CreatedAt).ThenBy(sample => sample.Id).Select(sample => sample.ArNumber).FirstOrDefault(),
                ManufacturerName = batch.Checklist != null && batch.Checklist.Manufacturer != null ? batch.Checklist.Manufacturer.Name : null,
                SupplierName = batch.Checklist != null && batch.Checklist.Supplier != null ? batch.Checklist.Supplier.Name : null,
                ManufacturingDate = batch.ManufacturingDate, ExpiryDate = batch.ExpiryDate, RetestDate = batch.RetestDate,
                Quantity = position.Quantity, UoM = position.UoM,
            };
    }
}
