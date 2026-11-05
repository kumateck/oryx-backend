using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal sealed record ProductionMaterialAllocation(Guid BatchId, Guid ShelfId, Guid UnitId, decimal Quantity);
/// <param name="ShortageDetails">One sentence per short material saying what was counted and why stock was not usable.</param>
internal sealed record ProductionMaterialAllocationPlan(List<ProductionMaterialAllocation> Allocations,
    Dictionary<Guid, decimal> Shortages, string EvidenceHash, List<string> Blockers, List<string> ShortageDetails);

internal static class ProductionMaterialAllocator
{
    internal static IQueryable<MaterialBatchReservedQuantity> UnissuedReservations(ApplicationDbContext context) =>
        context.MaterialBatchReservedQuantities.IgnoreQueryFilters().Where(item => item.DeletedAt == null && item.ReleasedAt == null
            && (!context.RequisitionItems.IgnoreQueryFilters().Any(line => line.Requisition.ProductionScheduleProductId == item.ProductionScheduleProductId
                && line.Requisition.RequisitionType == DOMAIN.Entities.Requisitions.RequisitionType.Stock
                && line.MaterialId == item.MaterialBatch.MaterialId && (line.Status == DOMAIN.Entities.Requisitions.RequestStatus.Completed
                    || line.Requisition.Status == DOMAIN.Entities.Requisitions.RequestStatus.Completed
                    || context.StockRequisitionIssues.IgnoreQueryFilters().Any(issue => issue.RequisitionId == line.RequisitionId)))
                // Ambiguous legacy duplicates with an outstanding stock document still hold stock.
                || context.RequisitionItems.IgnoreQueryFilters().Any(line => line.Requisition.ProductionScheduleProductId == item.ProductionScheduleProductId
                    && line.Requisition.RequisitionType == DOMAIN.Entities.Requisitions.RequisitionType.Stock
                    && line.MaterialId == item.MaterialBatch.MaterialId && line.DeletedAt == null && line.Requisition.DeletedAt == null
                    && line.Status != DOMAIN.Entities.Requisitions.RequestStatus.Completed && line.Requisition.Status != DOMAIN.Entities.Requisitions.RequestStatus.Completed
                    && !context.StockRequisitionIssues.IgnoreQueryFilters().Any(issue => issue.RequisitionId == line.RequisitionId))));

    internal static async Task<ProductionMaterialAllocationPlan> Plan(ApplicationDbContext context, ProductionMaterialSource source,
        List<MaterialBatchReservedQuantity> existing)
    {
        var ids = source.Requirements.Select(item => item.MaterialId).ToList();
        var shelves = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.MaterialBatch).ThenInclude(item => item.UoM)
            .Include(item => item.MaterialBatch).ThenInclude(item => item.Material)
            .Where(item => ids.Contains(item.MaterialBatch.MaterialId) && item.DeletedAt == null && item.MaterialBatch.DeletedAt == null
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == source.StorageId
                && item.WarehouseLocationShelf.DeletedAt == null && item.WarehouseLocationShelf.WarehouseLocationRack.DeletedAt == null
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.DeletedAt == null)
            .ToListAsync();
        var shelfIds = shelves.Select(item => item.WarehouseLocationShelfId).ToList();
        // Issued reservations still exist, but their stock has already left this storage shelf.
        // Subtract only allocations which have not been issued; count all other batches' holds.
        var others = await UnissuedReservations(context).IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.UoM).Include(item => item.MaterialBatch).Include(item => item.ProductionScheduleProduct)
            .Where(item => item.ProductionScheduleProductId != source.Batch.Id
                && item.WarehouseLocationShelfId.HasValue && shelfIds.Contains(item.WarehouseLocationShelfId.Value))
            .ToListAsync();
        var allocations = new List<ProductionMaterialAllocation>();
        var shortages = new Dictionary<Guid, decimal>();
        var blockers = new List<string>();
        var shortfalls = new List<(ProductionMaterialRequirement Requirement, decimal Remaining)>();
        foreach (var requirement in source.Requirements)
        {
            var remaining = requirement.Quantity;
            var candidates = shelves.Where(item => item.MaterialBatch.MaterialId == requirement.MaterialId
                    && item.Quantity > 0 && item.MaterialBatch.Status == BatchStatus.Available
                    && !MaterialValidity.IsExpired(item.MaterialBatch, DateTime.UtcNow))
                .OrderByDescending(item => existing.Any(reservation => reservation.MaterialBatchId == item.MaterialBatchId
                    && reservation.WarehouseLocationShelfId == item.WarehouseLocationShelfId))
                .ThenBy(item => item.MaterialBatch.ReturnDate == null).ThenBy(item => item.MaterialBatch.ReturnDate)
                .ThenBy(item => MaterialValidity.EffectiveDate(item.MaterialBatch)).ThenBy(item => item.Id).ToList();
            foreach (var shelf in candidates)
            {
                if (remaining <= 0) break;
                if (shelf.UoMId != shelf.MaterialBatch.UoMId || shelf.MaterialBatch.UoMId != requirement.UnitId)
                { blockers.Add($"{requirement.Name}: shelf, batch and department stock units must agree. Reconcile the stock units first."); continue; }
                var available = UomConverter.Convert(shelf.Quantity, shelf.MaterialBatch.UoM?.Symbol, requirement.Unit);
                var held = others.Where(item => item.MaterialBatchId == shelf.MaterialBatchId && item.WarehouseLocationShelfId == shelf.WarehouseLocationShelfId)
                    .Select(item => UomConverter.Convert(item.Quantity, item.UoM?.Symbol, requirement.Unit)).ToList();
                if (!available.HasValue || held.Any(quantity => !quantity.HasValue))
                { blockers.Add($"{requirement.Name}: existing stock/reservation units cannot be converted safely."); continue; }
                var quantity = Math.Min(remaining, Math.Max(0, available.Value - held.Sum(value => value.GetValueOrDefault())));
                if (quantity <= 0) continue;
                var physical = UomConverter.Convert(quantity, requirement.Unit, shelf.MaterialBatch.UoM.Symbol);
                if (!physical.HasValue) continue;
                allocations.Add(new ProductionMaterialAllocation(shelf.MaterialBatchId, shelf.WarehouseLocationShelfId, shelf.MaterialBatch.UoMId, physical.Value));
                remaining -= quantity;
            }
            shortages[requirement.MaterialId] = remaining;
            if (remaining > 0) shortfalls.Add((requirement, remaining));
        }
        var evidence = ProductionMaterialSources.Hash(new
        {
            Shelves = shelves.OrderBy(item => item.Id).Select(item => new { item.Id, item.MaterialBatchId, item.WarehouseLocationShelfId,
                item.UoMId, item.Quantity, item.MaterialBatch.Status, item.MaterialBatch.ExpiryDate, item.MaterialBatch.RetestDate, item.MaterialBatch.Material.Kind, item.MaterialBatch.ReturnDate, item.MaterialBatch.UoM.Symbol }),
            Others = others.OrderBy(item => item.Id).Select(item => new { item.Id, item.MaterialBatchId, item.WarehouseLocationShelfId, item.UoMId, item.Quantity, item.UoM.Symbol })
        });
        var details = new List<string>();
        if (shortfalls.Count > 0)
        {
            // Only to explain a refusal: where else the short materials are on shelves (other warehouses).
            var shortIds = shortfalls.Select(item => item.Requirement.MaterialId).ToList();
            var elsewhere = await context.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(item => shortIds.Contains(item.MaterialBatch.MaterialId) && item.Quantity > 0
                    && item.DeletedAt == null && item.MaterialBatch.DeletedAt == null && item.WarehouseLocationShelf.DeletedAt == null
                    && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId != source.StorageId)
                .Select(item => new StockElsewhere(item.MaterialBatch.MaterialId,
                    item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Name, item.Quantity, item.MaterialBatch.UoM.Symbol))
                .ToListAsync();
            details.AddRange(shortfalls.Select(item => ShortageDetail(item.Requirement, item.Remaining, shelves, others,
                elsewhere.Where(row => row.MaterialId == item.Requirement.MaterialId).ToList())));
        }
        return new ProductionMaterialAllocationPlan(allocations, shortages, evidence, blockers.Distinct().ToList(), details);
    }

    /// <summary>
    /// Explains a shortage in the requirement's unit: usable stock found, stock held by other batches' unissued
    /// reservations (with their batch numbers), and stock on shelves that is not approved or not valid.
    /// </summary>
    private sealed record StockElsewhere(Guid MaterialId, string Warehouse, decimal Quantity, string Unit);

    private static string ShortageDetail(ProductionMaterialRequirement requirement, decimal remaining,
        List<DOMAIN.Entities.Warehouses.ShelfMaterialBatch> shelves, List<MaterialBatchReservedQuantity> others, List<StockElsewhere> elsewhere)
    {
        decimal Sum(IEnumerable<DOMAIN.Entities.Warehouses.ShelfMaterialBatch> rows) => rows
            .Select(item => UomConverter.Convert(item.Quantity, item.MaterialBatch.UoM?.Symbol, requirement.Unit) ?? 0).Sum();
        var now = DateTime.UtcNow;
        var own = shelves.Where(item => item.MaterialBatch.MaterialId == requirement.MaterialId && item.Quantity > 0).ToList();
        var notApproved = own.Where(item => item.MaterialBatch.Status != BatchStatus.Available).ToList();
        var notValid = own.Where(item => item.MaterialBatch.Status == BatchStatus.Available && MaterialValidity.IsExpired(item.MaterialBatch, now)).ToList();
        var usable = own.Except(notApproved).Except(notValid).ToList();
        var holds = others.Where(hold => usable.Any(shelf => shelf.MaterialBatchId == hold.MaterialBatchId
            && shelf.WarehouseLocationShelfId == hold.WarehouseLocationShelfId)).ToList();
        var held = holds.Select(item => UomConverter.Convert(item.Quantity, item.UoM?.Symbol, requirement.Unit) ?? 0).Sum();
        var holders = holds.Select(item => item.ProductionScheduleProduct?.BatchNumber).Where(number => !string.IsNullOrWhiteSpace(number))
            .Distinct().Order().Take(5).ToList();
        string Amount(decimal value) => $"{value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)} {requirement.Unit}";
        var parts = new List<string>
        {
            $"{requirement.Name}: needs {Amount(requirement.Quantity)}, {Amount(Math.Max(0, requirement.Quantity - remaining))} available to reserve in the department's storage warehouse (short by {Amount(remaining)})"
        };
        if (held > 0) parts.Add($"{Amount(held)} is held by other batches' unissued reservations" + (holders.Count > 0 ? $" ({string.Join(", ", holders)})" : ""));
        if (notValid.Count > 0) parts.Add($"{Amount(Sum(notValid))} is expired or past its retest date");
        if (notApproved.Count > 0) parts.Add($"{Amount(Sum(notApproved))} is not yet approved for use");
        if (own.Count == 0) parts.Add("no stock of this material is on the storage warehouse's shelves");
        var located = elsewhere.GroupBy(row => string.IsNullOrWhiteSpace(row.Warehouse) ? "an unnamed warehouse" : row.Warehouse)
            .Select(group => (Warehouse: group.Key, Quantity: group.Sum(row => UomConverter.Convert(row.Quantity, row.Unit, requirement.Unit) ?? 0)))
            .Where(item => item.Quantity > 0).OrderByDescending(item => item.Quantity).Take(3).ToList();
        if (located.Count > 0)
            parts.Add("shelved elsewhere: " + string.Join(", ", located.Select(item => $"{Amount(item.Quantity)} in {item.Warehouse}"))
                + " (transfer it to this department's storage warehouse to use it)");
        else if (own.Count == 0) parts.Add("none is shelved in any other warehouse either; check that received stock has been put on shelves");
        return string.Join("; ", parts) + ".";
    }

    internal static void Apply(ApplicationDbContext context, ProductionMaterialSource source,
        List<MaterialBatchReservedQuantity> existing, List<ProductionMaterialAllocation> desired)
    {
        var pending = desired.ToDictionary(item => (item.BatchId, item.ShelfId, item.UnitId));
        foreach (var row in existing)
        {
            var key = (row.MaterialBatchId, row.WarehouseLocationShelfId.GetValueOrDefault(), row.UoMId);
            if (pending.Remove(key, out var target)) row.Quantity = target.Quantity;
            else row.ReleasedAt = DateTime.UtcNow;
        }
        foreach (var row in pending.Values)
            context.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity
            {
                ProductionScheduleProductId = source.Batch.Id, WarehouseId = source.ProductionId,
                MaterialBatchId = row.BatchId, WarehouseLocationShelfId = row.ShelfId, UoMId = row.UnitId, Quantity = row.Quantity
            });
    }
}
