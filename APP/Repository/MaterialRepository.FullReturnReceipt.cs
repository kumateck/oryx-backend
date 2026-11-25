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
    private Task<Result> ReceiveFullReturn(SupplyMaterialBatchRequest request, Guid actor) =>
        ProductionMaterialTransaction.Run(context, async () =>
        {
            var note = await context.MaterialReturnNotes.IgnoreQueryFilters().IgnoreAutoIncludes()
                .Include(row => row.ProductionScheduleProduct).ThenInclude(row => row.Product)
                .Include(row => row.FullReturns).ThenInclude(row => row.MaterialBatchReservedQuantity).ThenInclude(row => row.UoM)
                .Include(row => row.FullReturns).ThenInclude(row => row.MaterialBatchReservedQuantity).ThenInclude(row => row.MaterialBatch).ThenInclude(row => row.Material)
                .Include(row => row.FullReturns).ThenInclude(row => row.MaterialBatchReservedQuantity).ThenInclude(row => row.MaterialBatch).ThenInclude(row => row.UoM)
                .Include(row => row.FullReturns).ThenInclude(row => row.MaterialBatchReservedQuantity).ThenInclude(row => row.Warehouse)
                .SingleOrDefaultAsync(row => row.Id == request.MaterialReturnNoteId && row.DeletedAt == null);
            var lines = note?.FullReturns.Where(row => row.DeletedAt == null
                && row.MaterialBatchReservedQuantity.MaterialBatchId == request.MaterialBatchId).ToList();
            if (note is null || lines is null || lines.Count == 0)
                return Error.NotFound("FullReturn.NotFound", "The material is not on this full return note.");
            var lot = lines[0].MaterialBatchReservedQuantity.MaterialBatch;
            var key = lot.Material.Kind == MaterialKind.Raw
                ? PermissionKeys.CanAssignRawMaterialsStockToShelves : PermissionKeys.CanAssignPackagingMaterialsStockToShelves;
            async Task<bool> Allowed() => (await CurrentPermissionSet.Load(context, actor)).Contains(key);
            var department = await context.Users.Where(row => row.Id == actor && !row.IsDisabled).Select(row => row.DepartmentId).SingleOrDefaultAsync();
            if (!await Allowed() || !department.HasValue || department != note.ProductionScheduleProduct.Product.DepartmentId)
                return Error.Forbidden("FullReturn.ReceiptPermission", "You cannot receive this material return in your department.");
            if (!note.ProductionScheduleProduct.Cancelled || note.Status == MaterialReturnStatus.Completed
                || lines.Any(row => row.Returned || row.MaterialBatchReservedQuantity.ReleasedAt.HasValue))
                return Error.Conflict("FullReturn.Received", "This return is already received or is no longer awaiting receipt.");
            if (request.ShelfMaterialBatches is not { Count: > 0 }
                || request.ShelfMaterialBatches.Any(row => row.Quantity <= 0 || row.UomId.HasValue && row.UomId != lot.UoMId)
                || request.ShelfMaterialBatches.Select(row => row.WarehouseLocationShelfId).Distinct().Count() != request.ShelfMaterialBatches.Count)
                return Error.Validation("FullReturn.Shelves", "Choose distinct shelves and positive quantities in this lot's physical stock unit.");
            var density = lines[0].MaterialBatchReservedQuantity.DensityGramsPerMilliliter ?? await MaterialDensity.Load(context, lot.MaterialId);
            var quantities = lines.Select(row => MaterialDensity.Convert(row.MaterialBatchReservedQuantity.Quantity,
                row.MaterialBatchReservedQuantity.UoM.Symbol, lot.UoM.Symbol, density)).ToList();
            var total = request.ShelfMaterialBatches.Sum(row => row.Quantity);
            if (quantities.Any(row => row is null) || total != quantities.Sum())
                return Error.Validation("FullReturn.Quantity", "Receive exactly this lot's outstanding full-return quantity. No extra or partial receipt is allowed.");
            var issueEvidence = await context.BinCardInformation.IgnoreQueryFilters().IgnoreAutoIncludes().Where(row => row.DeletedAt == null
                && row.MaterialBatchId == lot.Id && context.Requisitions.IgnoreQueryFilters().Any(req => req.Id == row.RequisitionId
                    && req.ProductionScheduleProductId == note.ProductionScheduleProductId))
                .Select(row => new { row.QuantityIssued, row.QuantityReceived, Unit = row.UoMId.HasValue ? row.UoM.Symbol : row.MaterialBatch.UoM.Symbol }).ToListAsync();
            var issued = issueEvidence.Select(row => MaterialDensity.Convert(row.QuantityIssued - row.QuantityReceived, row.Unit, lot.UoM.Symbol, density)).ToList();
            if (issued.Any(row => row is null) || issued.Sum() != total)
                return Error.Conflict("FullReturn.IssueEvidence", "The return quantity is not backed by this batch's issued stock. Unissued reservations cannot be received as returns.");
            var destinationIds = lines.Select(row => row.DestinationWarehouseId).Distinct().ToList();
            var floorIds = lines.Select(row => row.MaterialBatchReservedQuantity.WarehouseId).Distinct().ToList();
            if (destinationIds.Count != 1 || floorIds.Count != 1
                || lines.Any(row => row.MaterialBatchReservedQuantity.Warehouse.DepartmentId != department
                    || row.MaterialBatchReservedQuantity.Warehouse.Type != WarehouseType.Production))
                return Error.Conflict("FullReturn.Warehouse", "Return sources and destinations are ambiguous. Reconcile the return note first.");
            var type = lot.Material.Kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage;
            var shelfIds = request.ShelfMaterialBatches.Select(row => row.WarehouseLocationShelfId).ToList();
            var shelves = await context.WarehouseLocationShelves.IgnoreAutoIncludes()
                .Where(row => shelfIds.Contains(row.Id) && row.WarehouseLocationRack.DeletedAt == null
                    && row.WarehouseLocationRack.WarehouseLocation.DeletedAt == null
                    && row.WarehouseLocationRack.WarehouseLocation.Warehouse.DeletedAt == null
                    && row.WarehouseLocationRack.WarehouseLocation.WarehouseId == destinationIds[0]
                    && row.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId == department
                    && row.WarehouseLocationRack.WarehouseLocation.Warehouse.Type == type).Select(row => row.Id).ToListAsync();
            if (shelves.Count != shelfIds.Count)
                return Error.Validation("FullReturn.ShelfWarehouse", "Receive into active shelves of the return note's warehouse in your department.");
            var movement = await context.MassMaterialBatchMovements.Where(row => row.BatchId == lot.Id
                && (row.FromWarehouseId == floorIds[0] || row.ToWarehouseId == floorIds[0])).Select(row => new { row.Quantity, row.FromWarehouseId, row.ToWarehouseId }).ToListAsync();
            var consumed = await context.MaterialBatchEvents.Where(row => row.BatchId == lot.Id && row.Type == EventType.Consumed
                && row.ConsumptionWarehouseId == floorIds[0]).SumAsync(row => row.Quantity);
            var physical = movement.Sum(row => (row.ToWarehouseId == floorIds[0] ? row.Quantity : 0) - (row.FromWarehouseId == floorIds[0] ? row.Quantity : 0)) - consumed;
            if (physical < total || lot.TotalQuantity - lot.ConsumedQuantity - lot.QuantityAssigned < total)
                return Error.Conflict("FullReturn.FloorBalance", "The outstanding return exceeds physical production-floor stock. Reconcile consumption and movements.");
            var existing = await context.ShelfMaterialBatches.Where(row => row.MaterialBatchId == lot.Id
                && shelfIds.Contains(row.WarehouseLocationShelfId)).ToListAsync();
            if (existing.Any(row => row.UoMId != lot.UoMId || row.Quantity < 0))
                return Error.Conflict("FullReturn.ShelfUnit", "The destination shelf's stock unit or quantity needs reconciliation.");
            var now = DateTime.UtcNow;
            var ledger = await StockTransferLedger.Prepare(context, [new(lot, destinationIds[0], total, 0,
                $"Full return · {note.BatchNumber} · {note.Id}", actor, now, density)]);
            if (ledger.IsFailure) return ledger.Error;
            if (!await Allowed()) return Error.Forbidden("FullReturn.ReceiptPermission", "Your current permissions do not allow this receipt.");
            foreach (var assignment in request.ShelfMaterialBatches)
            {
                var shelf = existing.SingleOrDefault(row => row.WarehouseLocationShelfId == assignment.WarehouseLocationShelfId);
                if (shelf is null) context.ShelfMaterialBatches.Add(new() { MaterialBatchId = lot.Id, UoMId = lot.UoMId,
                    WarehouseLocationShelfId = assignment.WarehouseLocationShelfId, Quantity = assignment.Quantity, Note = assignment.Note });
                else shelf.Quantity += assignment.Quantity;
            }
            context.BinCardInformation.AddRange(ledger.Value);
            context.MassMaterialBatchMovements.Add(new() { BatchId = lot.Id, FromWarehouseId = floorIds[0], ToWarehouseId = destinationIds[0],
                Quantity = total, MovedAt = now, MovedById = actor, MovementType = MovementType.ToWarehouse });
            context.MaterialBatchEvents.Add(new() { BatchId = lot.Id, Quantity = total, Type = EventType.Moved, UserId = actor, CreatedAt = now,
                ProductionScheduleProductId = note.ProductionScheduleProductId });
            foreach (var line in lines) line.Returned = true;
            if (note.FullReturns.Where(row => row.DeletedAt == null).All(row => row.Returned)) note.Status = MaterialReturnStatus.Completed;
            lot.ReturnDate = now;
            lot.QuantityAssigned += total;
            // Assigned stock follows physical shelves. A return never changes total or consumed stock.
            await context.SaveChangesAsync();
            return Result.Success();
        });
}
