using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Production;

public sealed partial class IssuedMaterialRepairService
{
    private async Task<Result<Plan>> BuildPlan(Guid batchId, IssuedMaterialRepairKind kind)
    {
        var batch = await db.ProductionScheduleProducts.IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.Product).Include(item => item.ProductionSchedule).SingleOrDefaultAsync(item => item.Id == batchId);
        if (batch is null) return Error.NotFound("Repair.Batch", "The production batch is no longer available.");
        var materialKind = kind == IssuedMaterialRepairKind.Bom ? MaterialKind.Raw : MaterialKind.Package;
        var preview = new IssuedMaterialRepairPreviewDto { BatchId = batch.Id, BatchNumber = batch.BatchNumber ?? "", Kind = kind };
        var blockers = preview.Blockers;
        var activities = await db.ProductionActivities.IgnoreAutoIncludes().Where(item => item.ProductionScheduleProductId == batchId)
            .Include(item => item.Steps).ThenInclude(item => item.Operation).ToListAsync();
        if (batch.Cancelled || batch.ProductionSchedule.Status is ProductionStatus.Cancelled or ProductionStatus.Completed
            || activities.Count != 1 || activities.Any(item => item.CompletedAt.HasValue || item.Status is ProductionStatus.Completed or ProductionStatus.Cancelled))
            blockers.Add("The batch must have one open production run and an active schedule.");
        var consumptionName = kind == IssuedMaterialRepairKind.Bom ? "Product Preparation" : "Final Packing";
        if (activities.SelectMany(item => item.Steps).Any(step => step.Operation.Name != null && step.Operation.Name.StartsWith(consumptionName)
            && (step.StartedAt.HasValue || step.CompletedAt.HasValue || step.Status != ProductionStatus.New)))
            blockers.Add("The material consumption step has started. A reviewed consumption correction is required.");
        if (await db.MaterialReturnNotes.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == batchId)
            || await db.ProductionExtraPackings.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == batchId)
            || await db.FinalPackings.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == batchId)
            || await db.FinishedGoodsTransferNotes.IgnoreQueryFilters().AnyAsync(item => item.BatchManufacturingRecord.ProductionScheduleProductId == batchId))
            blockers.Add("This batch has returns, extra-material requests or finalized records. Reconcile those records before a stock repair.");

        var requisitions = await db.Requisitions.IgnoreAutoIncludes().Where(item => item.ProductionScheduleProductId == batchId
            && item.RequisitionType == RequisitionType.Stock && item.Items.Any(line => line.Material.Kind == materialKind))
            .Include(item => item.Items).ThenInclude(item => item.Material).ToListAsync();
        if (requisitions.Count != 1) return Error.Conflict("Repair.Requisition", "Exactly one stock requisition of this material kind is required.");
        var requisition = requisitions[0];
        preview.RequisitionCode = requisition.Code;
        if (!requisition.Approved || requisition.Status != RequestStatus.Completed || requisition.Items.Any(item => item.Status != RequestStatus.Completed))
            blockers.Add("The requisition must be approved and fully issued. Use the existing pre-issue workflow otherwise.");
        if (requisition.Items.Any(item => item.Material.Kind != materialKind || item.DeletedAt.HasValue || item.Material.IsUnlimited))
            blockers.Add("Mixed, deleted or unlimited material lines cannot be repaired automatically.");

        var targets = new List<(Guid MaterialId, decimal Quantity, Guid? Unit)>();
        if (kind == IssuedMaterialRepairKind.Packaging)
        {
            var packages = await db.ProductPackages.IgnoreAutoIncludes().AsNoTracking().Where(item => item.ProductId == batch.ProductId
                && (item.ProductPackingId == batch.ProductPackingId || item.ProductPackingId == null)).OrderBy(item => item.Id).ToListAsync();
            targets.AddRange(packages.Select(item => (item.MaterialId,
                (batch.BatchSize == BatchSize.Full ? item.PrescribedQuantity : item.PrescribedQuantity / 2) + item.Loose, (Guid?)null)));
        }
        else
        {
            var boms = await db.ProductBillOfMaterials.IgnoreAutoIncludes().AsNoTracking().Where(item => item.ProductId == batch.ProductId
                && item.IsActive).OrderByDescending(item => item.EffectiveDate).ThenByDescending(item => item.Version).Take(1)
                .Include(item => item.BillOfMaterial).ThenInclude(item => item.Items).ToListAsync();
            if (boms.Count != 1) blockers.Add("Exactly one active product BOM is required.");
            else targets.AddRange(boms[0].BillOfMaterial.Items.Select(item => (item.MaterialId,
                batch.BatchSize == BatchSize.Full ? item.PrescribedQuantity : item.PrescribedQuantity / 2, item.BaseUoMId)));
        }
        var ids = requisition.Items.Select(item => item.MaterialId).Order().ToList();
        if (ids.Distinct().Count() != ids.Count || targets.Select(item => item.MaterialId).Distinct().Count() != targets.Count
            || !ids.SequenceEqual(targets.Select(item => item.MaterialId).Order()))
            blockers.Add("The corrected master must contain the same materials, once each. Material replacement or substitution requires separate reconciliation.");
        if (targets.Any(item => item.Quantity <= 0)) blockers.Add("Every corrected master quantity must be positive.");
        var links = await db.MaterialDepartments.IgnoreAutoIncludes().AsNoTracking().Include(item => item.UoM)
            .Where(item => ids.Contains(item.MaterialId) && item.DepartmentId == batch.Product.DepartmentId).ToListAsync();
        var reservations = await db.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Include(item => item.MaterialBatch).ThenInclude(item => item.Material)
            .Where(item => item.ProductionScheduleProductId == batchId && item.ProductionUnstartRepairId == null && item.ReleasedAt == null && ids.Contains(item.MaterialBatch.MaterialId))
            .OrderBy(item => item.Id).ToListAsync();
        if (reservations.Any(item => item.DeletedAt.HasValue || item.MaterialBatch.DeletedAt.HasValue || item.Quantity < 0))
            blockers.Add("Issued reservations include consumed, deleted or invalid stock. Their stock effect must be reviewed separately.");
        var productionWarehouses = await db.Warehouses.IgnoreAutoIncludes().AsNoTracking().Where(item => item.DepartmentId == batch.Product.DepartmentId
            && item.Type == WarehouseType.Production).ToListAsync();
        var productionWarehouse = productionWarehouses.Count == 1 ? productionWarehouses[0].Id : Guid.Empty;
        if (productionWarehouse == Guid.Empty || reservations.Any(item => item.WarehouseId != productionWarehouse))
            blockers.Add("A single matching production warehouse is required.");

        var cards = await db.BinCardInformation.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Include(item => item.MaterialBatch).Where(item => item.RequisitionId == requisition.Id).OrderBy(item => item.Id).ToListAsync();
        var sourceIds = cards.Select(item => item.WarehouseId).Distinct().ToList();
        var sourceId = sourceIds.Count == 1 ? sourceIds[0] ?? Guid.Empty : Guid.Empty;
        var sourceType = kind == IssuedMaterialRepairKind.Bom ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage;
        if (sourceId == Guid.Empty || !await db.Warehouses.AnyAsync(item => item.Id == sourceId && item.Type == sourceType))
            blockers.Add("The original source warehouse cannot be proven from the issue bin cards.");
        if (cards.Any(item => item.DeletedAt.HasValue || item.MaterialBatch == null || item.MaterialBatch.DeletedAt.HasValue
            || item.UoMId != item.MaterialBatch.UoMId || !ids.Contains(item.MaterialBatch.MaterialId)))
            blockers.Add("The issue bin-card evidence contains missing, deleted or mismatched material units.");

        var shelves = await db.ShelfMaterialBatches.IgnoreAutoIncludes().Include(item => item.MaterialBatch).ThenInclude(item => item.Material)
            .Include(item => item.WarehouseLocationShelf).ThenInclude(item => item.WarehouseLocationRack).ThenInclude(item => item.WarehouseLocation)
            .Where(item => ids.Contains(item.MaterialBatch.MaterialId)
                && item.MaterialBatch.DeletedAt == null && item.MaterialBatch.Material.DeletedAt == null
                && item.WarehouseLocationShelf.DeletedAt == null && item.WarehouseLocationShelf.WarehouseLocationRack.DeletedAt == null
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.DeletedAt == null
                && item.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == sourceId)
            .OrderBy(item => item.MaterialBatch.DateReceived).ThenBy(item => item.MaterialBatchId).ThenBy(item => item.Id).ToListAsync();
        if (shelves.Any(shelf => shelf.Quantity < 0 || shelf.MaterialBatch.ConsumedQuantity < 0
            || shelf.MaterialBatch.TotalQuantity < shelf.MaterialBatch.ConsumedQuantity || shelf.UoMId != shelf.MaterialBatch.UoMId))
            blockers.Add("Source stock contains invalid balances or mismatched units. Reconcile it before repairing the issue.");
        var sourceBalances = await db.BinCardInformation.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(card => card.WarehouseId == sourceId && ids.Contains(card.MaterialBatch.MaterialId))
            .GroupBy(card => card.MaterialBatch.MaterialId)
            .Select(group => new { MaterialId = group.Key, Balance = group.Sum(card => card.QuantityReceived - card.QuantityIssued) }).ToListAsync();
        foreach (var materialId in ids)
            if (sourceBalances.Where(balance => balance.MaterialId == materialId).Sum(balance => balance.Balance)
                != shelves.Where(shelf => shelf.MaterialBatch.MaterialId == materialId).Sum(shelf => shelf.Quantity))
                blockers.Add("The source warehouse bin-card balance does not reconcile with its physical shelves.");
        var lotIds = shelves.Select(item => item.MaterialBatchId).Distinct().ToList();
        var pending = await db.MaterialBatchReservedQuantities.IgnoreAutoIncludes()
            .Where(item => lotIds.Contains(item.MaterialBatchId) && item.ProductionScheduleProductId != batchId
                && !db.Requisitions.Any(req => req.ProductionScheduleProductId == item.ProductionScheduleProductId
                    && req.RequisitionType == RequisitionType.Stock && req.Status == RequestStatus.Completed
                    && req.Items.Any(line => line.MaterialId == item.MaterialBatch.MaterialId)))
            .GroupBy(item => item.MaterialBatchId).Select(group => new { Id = group.Key, Quantity = group.Sum(item => item.Quantity) }).ToListAsync();
        var remainingByLot = shelves.GroupBy(item => item.MaterialBatchId).ToDictionary(group => group.Key,
            group => Math.Max(0, group.Sum(item => item.Quantity) - pending.Where(item => item.Id == group.Key).Sum(item => item.Quantity)));
        var allReserved = await db.MaterialBatchReservedQuantities.IgnoreAutoIncludes().Where(item => lotIds.Contains(item.MaterialBatchId))
            .GroupBy(item => item.MaterialBatchId).Select(group => new { Id = group.Key, Quantity = group.Sum(item => item.Quantity) }).ToListAsync();
        foreach (var group in shelves.GroupBy(item => item.MaterialBatchId))
        {
            var lot = group.First().MaterialBatch;
            remainingByLot[group.Key] = Math.Min(remainingByLot[group.Key], Math.Max(0,
                lot.TotalQuantity - lot.ConsumedQuantity - allReserved.Where(item => item.Id == lot.Id).Sum(item => item.Quantity)));
        }
        var history = await db.IssuedMaterialRepairs.AsNoTracking().Where(item => item.ProductionScheduleProductId == batchId && item.Kind == kind)
            .Include(item => item.Lines).OrderByDescending(item => item.AppliedAt).ThenBy(item => item.Id).ToListAsync();
        var sourceShelves = await db.WarehouseLocationShelves.IgnoreAutoIncludes().Include(shelf => shelf.WarehouseLocationRack)
            .ThenInclude(rack => rack.WarehouseLocation).Where(shelf => shelf.DeletedAt == null
                && shelf.WarehouseLocationRack.DeletedAt == null && shelf.WarehouseLocationRack.WarehouseLocation.DeletedAt == null
                && shelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == sourceId).ToListAsync();
        var allocations = new List<Allocation>();
        foreach (var item in requisition.Items.OrderBy(item => item.MaterialId))
        {
            var unitLinks = links.Where(link => link.MaterialId == item.MaterialId).ToList();
            if (unitLinks.Count != 1) { blockers.Add($"{item.Material.Code}: one department material unit is required."); continue; }
            var link = unitLinks[0];
            var reserved = reservations.Where(reservation => reservation.MaterialBatch.MaterialId == item.MaterialId).ToList();
            var before = reserved.Sum(reservation => reservation.Quantity);
            var effective = item.Quantity + history.SelectMany(repair => repair.Lines).Where(line => line.RequisitionItemId == item.Id).Sum(line => line.Adjustment);
            var materialCards = cards.Where(card => card.MaterialBatch?.MaterialId == item.MaterialId).ToList();
            if (reserved.Count == 0 || before != effective || materialCards.Sum(card => card.QuantityIssued - card.QuantityReceived) != effective
                || reserved.Any(reservation => reservation.UoMId != link.UoMId || reservation.MaterialBatch.UoMId != link.UoMId)
                || item.UoMId != link.UoMId || targets.Any(target => target.MaterialId == item.MaterialId && target.Unit.HasValue && target.Unit != link.UoMId))
                blockers.Add($"{item.Material.Code}: approved quantities, repairs, reservations, units and issue bin cards do not reconcile.");
            foreach (var lot in reserved.GroupBy(reservation => reservation.MaterialBatchId))
                if (lot.Sum(reservation => reservation.Quantity) != materialCards.Where(card => card.MaterialBatchId == lot.Key).Sum(card => card.QuantityIssued - card.QuantityReceived))
                    blockers.Add($"{item.Material.Code}: the issued lots do not reconcile with the production reservations.");
            var targetQuantity = targets.Where(target => target.MaterialId == item.MaterialId).Sum(target => target.Quantity);
            var row = new IssuedMaterialRepairRowDto { MaterialId = item.MaterialId, Code = item.Material.Code, Name = item.Material.Name,
                Unit = link.UoM.Symbol, ApprovedQuantity = item.Quantity, BeforeQuantity = before, TargetQuantity = targetQuantity, Adjustment = targetQuantity - before };
            preview.Rows.Add(row);
            var eligible = shelves.Where(shelf => shelf.MaterialBatch.MaterialId == item.MaterialId && shelf.Quantity > 0
                && shelf.UoMId == link.UoMId && shelf.MaterialBatch.UoMId == link.UoMId && shelf.MaterialBatch.Status == BatchStatus.Available
                && !MaterialValidity.IsExpired(shelf.MaterialBatch, DateTime.UtcNow))
                .OrderBy(shelf => MaterialValidity.EffectiveDate(shelf.MaterialBatch) ?? DateTime.MaxValue)
                .ThenBy(shelf => shelf.MaterialBatch.DateReceived).ThenBy(shelf => shelf.Id).ToList();
            row.AvailableQuantity = eligible.GroupBy(shelf => shelf.MaterialBatchId).Sum(group => Math.Min(group.Sum(shelf => shelf.Quantity), remainingByLot[group.Key]));
            if (row.Adjustment > 0)
            {
                var needed = row.Adjustment;
                foreach (var shelf in eligible)
                {
                    var quantity = Math.Min(needed, Math.Min(shelf.Quantity, remainingByLot[shelf.MaterialBatchId]));
                    if (quantity <= 0) continue;
                    remainingByLot[shelf.MaterialBatchId] -= quantity;
                    allocations.Add(new Allocation(shelf, null, quantity));
                    row.Lots.Add(new() { MaterialBatchId = shelf.MaterialBatchId, ShelfId = shelf.WarehouseLocationShelfId, BatchNumber = shelf.MaterialBatch.BatchNumber, Adjustment = quantity });
                    needed -= quantity;
                    if (needed == 0) break;
                }
                if (needed > 0) blockers.Add($"{item.Material.Code}: insufficient eligible stock for the additional requirement.");
            }
            else if (row.Adjustment < 0)
            {
                var toReturn = -row.Adjustment;
                foreach (var reservation in reserved.Where(reservation => reservation.DeletedAt == null && reservation.Quantity > 0))
                {
                    if (sourceShelves.All(shelf => shelf.Id != reservation.WarehouseLocationShelfId)) continue;
                    var matches = shelves.Where(shelf => shelf.MaterialBatchId == reservation.MaterialBatchId
                        && shelf.WarehouseLocationShelfId == reservation.WarehouseLocationShelfId && shelf.UoMId == reservation.UoMId).ToList();
                    if (matches.Count > 1) { blockers.Add($"{item.Material.Code}: duplicate source shelf rows require review."); continue; }
                    // An issue can empty and soft-delete a shelf allocation. A genuine return creates
                    // a new allocation at that same verified physical shelf, preserving the old row.
                    var shelf = matches.FirstOrDefault() ?? allocations.Select(allocation => allocation.Shelf).FirstOrDefault(shelf =>
                        shelf.MaterialBatchId == reservation.MaterialBatchId && shelf.WarehouseLocationShelfId == reservation.WarehouseLocationShelfId)
                        ?? new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatchId = reservation.MaterialBatchId,
                            MaterialBatch = reservation.MaterialBatch, WarehouseLocationShelfId = reservation.WarehouseLocationShelfId.GetValueOrDefault(),
                            UoMId = reservation.UoMId, Quantity = 0 };
                    var quantity = Math.Min(toReturn, reservation.Quantity);
                    allocations.Add(new Allocation(shelf, reservation, -quantity));
                    row.Lots.Add(new() { MaterialBatchId = shelf.MaterialBatchId, ShelfId = shelf.WarehouseLocationShelfId, BatchNumber = shelf.MaterialBatch.BatchNumber, Adjustment = -quantity });
                    toReturn -= quantity;
                    if (toReturn == 0) break;
                }
                if (toReturn > 0) blockers.Add($"{item.Material.Code}: a verified source shelf is required for every returned lot.");
            }
        }
        preview.History = history.Select(repair => new IssuedMaterialRepairHistoryDto { Id = repair.Id, ActorId = repair.ActorId,
            AppliedAt = repair.AppliedAt, Reason = repair.Reason, Rows = repair.Lines.OrderBy(line => line.MaterialId).Select(line => {
                return new IssuedMaterialRepairRowDto { MaterialId = line.MaterialId, Code = line.MaterialCode, Name = line.MaterialName, Unit = line.UnitSymbol,
                    ApprovedQuantity = line.ApprovedQuantity, BeforeQuantity = line.BeforeQuantity, TargetQuantity = line.TargetQuantity, Adjustment = line.Adjustment };
            }).ToList() }).ToList();
        foreach (var row in preview.Rows)
            row.Lots = row.Lots.GroupBy(lot => new { lot.MaterialBatchId, lot.ShelfId, lot.BatchNumber })
                .OrderBy(group => group.Key.MaterialBatchId).ThenBy(group => group.Key.ShelfId)
                .Select(group => new IssuedMaterialRepairLotDto { MaterialBatchId = group.Key.MaterialBatchId,
                    ShelfId = group.Key.ShelfId, BatchNumber = group.Key.BatchNumber, Adjustment = group.Sum(lot => lot.Adjustment) }).ToList();
        preview.HasChanges = preview.Rows.Any(row => row.Adjustment != 0);
        preview.CanApply = preview.HasChanges && blockers.Count == 0;
        preview.PreviewToken = Token(new { BatchId = batch.Id, batch.BatchNumber, batch.BatchSize, batch.ProductPackingId, kind, RequisitionId = requisition.Id,
            Rows = preview.Rows, Blockers = blockers, History = history.Select(item => item.Id),
            Reservations = reservations.Select(item => new { item.Id, item.MaterialBatchId, item.WarehouseId, item.WarehouseLocationShelfId, item.Quantity, item.UoMId, item.DeletedAt }),
            Shelves = shelves.Select(item => new { item.Id, item.Quantity, item.UoMId, item.MaterialBatch.TotalQuantity, item.MaterialBatch.ConsumedQuantity, item.MaterialBatch.QuantityAssigned,
                item.MaterialBatch.Status, item.MaterialBatch.ExpiryDate, item.MaterialBatch.RetestDate }),
            Pending = pending.OrderBy(item => item.Id), AllReserved = allReserved.OrderBy(item => item.Id), SourceBalances = sourceBalances.OrderBy(item => item.MaterialId),
            SourceShelves = sourceShelves.Select(shelf => shelf.Id).Order() });
        return new Plan(batch, requisition, sourceId, productionWarehouse, reservations, preview, allocations);
    }
}
