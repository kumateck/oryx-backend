using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>Cancels an unconsumed batch. Only issued allocations become warehouse returns; holds are released.</summary>
internal static class ProductionFullReturn
{
    internal static async Task<Result> Run(ApplicationDbContext db, Guid batchId, string reason) =>
        await ProductionMaterialTransaction.Run(db, async () =>
        {
            var actor = db.CurrentActorId;
            async Task<bool> Allowed() => actor.HasValue && (await CurrentPermissionSet.Load(db, actor))
                .Contains(PermissionKeys.CanReturnProductionMaterials);
            if (!await Allowed()) return Denied();
            reason = reason?.Trim();
            if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
                return Error.Validation("FullReturn.Reason", "Enter a cancellation reason of 1–1000 characters.");
            var department = await db.Users.Where(row => row.Id == actor && !row.IsDisabled
                && row.Department.Type == DepartmentType.Production).Select(row => row.DepartmentId).SingleOrDefaultAsync();
            var batch = await db.ProductionScheduleProducts.IgnoreAutoIncludes().Include(row => row.Product)
                .Include(row => row.ProductionSchedule).SingleOrDefaultAsync(row => row.Id == batchId);
            if (!department.HasValue || batch is null || batch.Product.DepartmentId != department) return Denied();
            var prior = await db.MaterialReturnNotes.IgnoreQueryFilters().Include(row => row.FullReturns)
                .SingleOrDefaultAsync(row => row.ProductionScheduleProductId == batchId && row.FullReturns.Any());
            if (batch.Cancelled)
                return prior is not null && batch.ReasonForCancellation == reason ? Result.Success()
                    : Error.Conflict("FullReturn.Cancelled", "This batch is already cancelled. Review its return note.");
            var runs = await db.ProductionActivities.Include(row => row.Steps).ThenInclude(row => row.Operation)
                .Where(row => row.ProductionScheduleProductId == batchId).ToListAsync();
            if (runs.Count != 1 || runs.Any(row => row.CompletedAt.HasValue || row.Status is ProductionStatus.Completed or ProductionStatus.Cancelled)
                || batch.ProductionSchedule.Status is ProductionStatus.Completed or ProductionStatus.Cancelled)
                return Blocked("The batch must have one open production run.");
            var consumption = runs[0].Steps.Where(row => row.Operation.Name.StartsWith("Product Preparation")
                || row.Operation.Name.StartsWith("Final Packing")).OrderBy(row => row.Order).FirstOrDefault();
            if (consumption is not null && runs[0].Steps.Any(row => row.Order >= consumption.Order
                && (row.StartedAt.HasValue || row.CompletedAt.HasValue || row.Status is ProductionStatus.InProgress or ProductionStatus.Completed)))
                return Blocked("The production run has reached its consumption step. Full return is only allowed before consumption.");
            var bmrs = await db.BatchManufacturingRecords.Where(row => row.ProductionScheduleProductId == batchId).ToListAsync();
            var bprs = await db.BatchPackagingRecords.Where(row => row.ProductionScheduleProductId == batchId).ToListAsync();
            if (bmrs.Any(row => row.SampledQuantity != 0 || row.Status != BatchManufacturingStatus.New)
                || await db.AnalyticalTestRequests.IgnoreQueryFilters().AnyAsync(row => row.ProductionScheduleProductId == batchId)
                || await db.FinalPackings.IgnoreQueryFilters().AnyAsync(row => row.ProductionScheduleProductId == batchId)
                || await db.FinishedGoodsTransferNotes.IgnoreQueryFilters().AnyAsync(row => row.BatchManufacturingRecord.ProductionScheduleProductId == batchId)
                || await db.MaterialReturnNotes.IgnoreQueryFilters().AnyAsync(row => row.ProductionScheduleProductId == batchId)
                || await db.ProductionExtraPackings.IgnoreQueryFilters().AnyAsync(row => row.ProductionScheduleProductId == batchId))
                return Blocked("Testing, packing, finished goods, extra material or previous return records require a separate reconciliation.");
            var allocations = await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes()
                .Include(row => row.MaterialBatch).ThenInclude(row => row.Material).Include(row => row.MaterialBatch).ThenInclude(row => row.UoM)
                .Include(row => row.UoM).Include(row => row.Warehouse)
                .Where(row => row.ProductionScheduleProductId == batchId && row.ReleasedAt == null && row.ProductionUnstartRepairId == null).ToListAsync();
            var allocationIds = allocations.Select(row => row.Id).ToList();
            if (allocations.Any(row => row.DeletedAt.HasValue || row.Quantity <= 0 || row.MaterialBatch.DeletedAt.HasValue
                || row.MaterialBatch.Material.DeletedAt.HasValue || row.Warehouse.DeletedAt.HasValue
                || row.Warehouse.Type != WarehouseType.Production || row.Warehouse.DepartmentId != department)
                || await db.MaterialBatchEvents.IgnoreQueryFilters().AnyAsync(row => row.Type == EventType.Consumed
                    && (row.ProductionScheduleProductId == batchId || row.MaterialBatchReservedQuantityId.HasValue
                        && allocationIds.Contains(row.MaterialBatchReservedQuantityId.Value))))
                return Blocked("Consumed, deleted or invalid allocations prevent a full return. Reconcile consumption before cancelling.");
            var unissued = ProductionMaterialAllocator.UnissuedReservations(db);
            var holdIds = await unissued.Where(row => row.ProductionScheduleProductId == batchId).Select(row => row.Id).ToListAsync();
            var issued = allocations.Where(row => !holdIds.Contains(row.Id)).ToList();
            if (issued.Count == 0) return Blocked("No issued material was found. Use the unstart workflow to release unissued reservations.");
            var requisitions = await db.Requisitions.Include(row => row.Items).Where(row => row.ProductionScheduleProductId == batchId
                && row.RequisitionType == RequisitionType.Stock).ToListAsync();
            var requisitionIds = requisitions.Select(row => row.Id).ToList();
            var cards = await db.BinCardInformation.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                .Where(row => row.DeletedAt == null && row.RequisitionId.HasValue && requisitionIds.Contains(row.RequisitionId.Value))
                .Select(row => new { row.RequisitionId, row.MaterialBatchId, row.QuantityIssued, row.QuantityReceived, row.WarehouseId,
                    Unit = row.UoMId.HasValue ? row.UoM.Symbol : row.MaterialBatch.UoM.Symbol }).ToListAsync();
            var stores = await db.Warehouses.Where(row => row.DepartmentId == department
                && (row.Type == WarehouseType.RawMaterialStorage || row.Type == WarehouseType.PackagedStorage)).ToListAsync();
            var returns = new List<MaterialReturnNoteFullReturn>();
            foreach (var group in issued.GroupBy(row => row.MaterialBatchId))
            {
                var lot = group.First().MaterialBatch;
                var density = group.First().DensityGramsPerMilliliter ?? await MaterialDensity.Load(db, lot.MaterialId);
                decimal? Convert(decimal quantity, string unit) => MaterialDensity.Convert(quantity, unit, lot.UoM.Symbol, density);
                var quantity = group.Select(row => Convert(row.Quantity, row.UoM.Symbol)).ToList();
                var evidence = cards.Where(row => row.MaterialBatchId == lot.Id).ToList();
                var net = evidence.Select(row => Convert(row.QuantityIssued - row.QuantityReceived, row.Unit)).ToList();
                var targetType = lot.Material.Kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage;
                var destinations = stores.Where(row => row.Type == targetType).ToList();
                if (destinations.Count != 1 || quantity.Any(row => row is null) || net.Any(row => row is null)
                    || quantity.Sum() != net.Sum() || evidence.Any(row => row.WarehouseId != destinations[0].Id))
                    return Blocked($"{lot.Material.Code}: issued allocations, bin-card quantities and return warehouse do not reconcile.");
                foreach (var floorGroup in group.GroupBy(row => row.WarehouseId))
                {
                    var floor = floorGroup.Key;
                    var movements = await db.MassMaterialBatchMovements.IgnoreQueryFilters().AsNoTracking().Where(row => row.DeletedAt == null
                        && row.BatchId == lot.Id && (row.FromWarehouseId == floor || row.ToWarehouseId == floor)).ToListAsync();
                    var consumed = await db.MaterialBatchEvents.IgnoreQueryFilters().Where(row => row.DeletedAt == null
                        && row.BatchId == lot.Id && row.ConsumptionWarehouseId == floor && row.Type == EventType.Consumed).SumAsync(row => row.Quantity);
                    var physical = movements.Sum(row => (row.ToWarehouseId == floor ? row.Quantity : 0) - (row.FromWarehouseId == floor ? row.Quantity : 0)) - consumed;
                    var allIssued = await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
                        .Where(row => row.MaterialBatchId == lot.Id && row.WarehouseId == floor && row.DeletedAt == null && row.ReleasedAt == null
                            && !unissued.Any(hold => hold.Id == row.Id)).Select(row => new { row.Quantity, Unit = row.UoM.Symbol }).ToListAsync();
                    var pending = await db.MaterialReturnNoteFullReturns.IgnoreQueryFilters().IgnoreAutoIncludes().Where(row => row.DeletedAt == null && !row.Returned
                        && row.MaterialReturnNote.DeletedAt == null && row.MaterialBatchReservedQuantity.MaterialBatchId == lot.Id
                        && row.MaterialBatchReservedQuantity.WarehouseId == floor)
                        .Select(row => new { row.MaterialBatchReservedQuantity.Quantity, Unit = row.MaterialBatchReservedQuantity.UoM.Symbol }).ToListAsync();
                    var outstanding = allIssued.Concat(pending).Select(row => Convert(row.Quantity, row.Unit)).ToList();
                    if (outstanding.Any(row => row is null) || physical != outstanding.Sum())
                        return Blocked($"{lot.Material.Code}: floor movements, consumption and outstanding issued stock disagree.");
                }
                returns.AddRange(group.Select(row => new MaterialReturnNoteFullReturn { MaterialBatchReservedQuantityId = row.Id,
                    DestinationWarehouseId = destinations[0].Id, SourceWarehouseLocationShelfId = row.WarehouseLocationShelfId }));
            }
            // Every issue must be represented. An ambiguous issued/pending duplicate is not an unissued hold.
            if (cards.Any(card => card.QuantityIssued != card.QuantityReceived
                && !issued.Any(row => row.MaterialBatchId == card.MaterialBatchId)))
                return Blocked("An issue has no matching active allocation. Reconcile it before returning stock.");
            if (!await Allowed()) return Denied();
            var now = DateTime.UtcNow;
            var note = new MaterialReturnNote { ProductionScheduleProductId = batchId, BatchNumber = batch.BatchNumber ?? bmrs.FirstOrDefault()?.BatchNumber,
                ReturnDate = now, Status = MaterialReturnStatus.Pending, FullReturns = returns };
            db.MaterialReturnNotes.Add(note);
            foreach (var row in allocations)
            {
                if (holdIds.Contains(row.Id)) row.ReleasedAt = now;
                row.DeletedAt = now; row.LastDeletedById = actor;
            }
            foreach (var req in requisitions.Where(row => !cards.Any(card => card.RequisitionId == row.Id && card.QuantityIssued > 0)))
            {
                req.DeletedAt = now; req.LastDeletedById = actor;
                foreach (var line in req.Items) { line.DeletedAt = now; line.LastDeletedById = actor; }
            }
            batch.Cancelled = true; batch.ReasonForCancellation = reason;
            foreach (var run in runs)
            {
                run.Status = ProductionStatus.Cancelled;
                foreach (var step in run.Steps.Where(row => row.Status != ProductionStatus.Completed)) step.Status = ProductionStatus.Cancelled;
                db.ProductionActivityLogs.Add(new() { ProductionActivityId = run.Id, UserId = actor, Timestamp = now,
                    Message = $"Full return: batch cancelled; {returns.Count} issued allocation(s) awaiting warehouse receipt; {holdIds.Count} unissued hold(s) released. Reason: {reason}" });
            }
            foreach (var row in bmrs) { row.DeletedAt = now; row.LastDeletedById = actor; }
            foreach (var row in bprs) { row.DeletedAt = now; row.LastDeletedById = actor; }
            await db.SaveChangesAsync();
            return Result.Success();
        });

    private static Error Denied() => Error.Forbidden("FullReturn.Permission", "Your current department and permissions do not allow this return.");
    private static Error Blocked(string reason) => Error.Conflict("FullReturn.Reconciliation", reason);
}
