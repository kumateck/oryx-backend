using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Production;

public sealed partial class IssuedMaterialRepairService
{
    private async Task<IssuedMaterialRepair> Post(Plan plan, ApplyIssuedMaterialRepairRequest request)
    {
        var actor = identity.UserId.GetValueOrDefault();
        var now = DateTime.UtcNow;
        var audit = new IssuedMaterialRepair { Id = request.RequestId, ProductionScheduleProductId = plan.Batch.Id,
            RequisitionId = plan.Requisition.Id, Kind = plan.Preview.Kind, ActorId = actor, AppliedAt = now,
            PreviewToken = request.PreviewToken, Reason = request.Reason.Trim() };
        foreach (var row in plan.Preview.Rows)
        {
            var item = plan.Requisition.Items.Single(item => item.MaterialId == row.MaterialId);
            audit.Lines.Add(new() { Id = Guid.NewGuid(), RequisitionItemId = item.Id, MaterialId = row.MaterialId,
                MaterialCode = row.Code, MaterialName = row.Name, UnitSymbol = row.Unit,
                UoMId = item.UoMId.GetValueOrDefault(), ApprovedQuantity = item.Quantity, BeforeQuantity = row.BeforeQuantity,
                TargetQuantity = row.TargetQuantity, Adjustment = row.Adjustment });
        }
        var materialIds = plan.Preview.Rows.Select(row => row.MaterialId).ToList();
        var lotIds = plan.Allocations.Select(allocation => allocation.Shelf.MaterialBatchId).Distinct().ToList();
        var assigned = await db.ShelfMaterialBatches.IgnoreAutoIncludes().Where(shelf => lotIds.Contains(shelf.MaterialBatchId))
            .GroupBy(shelf => shelf.MaterialBatchId).Select(group => new { Id = group.Key, Quantity = group.Sum(shelf => shelf.Quantity) })
            .ToDictionaryAsync(item => item.Id, item => item.Quantity);
        var balances = await db.BinCardInformation.IgnoreQueryFilters().IgnoreAutoIncludes()
            .Where(card => card.WarehouseId == plan.SourceWarehouseId && materialIds.Contains(card.MaterialBatch.MaterialId))
            .GroupBy(card => card.MaterialBatch.MaterialId)
            .Select(group => new { MaterialId = group.Key, Balance = group.Sum(card => card.QuantityReceived - card.QuantityIssued) }).ToDictionaryAsync(item => item.MaterialId, item => item.Balance);
        foreach (var allocation in plan.Allocations)
        {
            var shelf = allocation.Shelf;
            var batch = shelf.MaterialBatch;
            var shelfBefore = shelf.Quantity;
            var assignedBefore = batch.QuantityAssigned;
            if (db.Entry(shelf).State == EntityState.Detached) db.ShelfMaterialBatches.Add(shelf);
            shelf.Quantity -= allocation.Adjustment;
            assigned[batch.Id] = assigned.GetValueOrDefault(batch.Id) - allocation.Adjustment;
            batch.QuantityAssigned = assigned[batch.Id];
            MaterialBatchReservedQuantity reservation;
            if (allocation.Adjustment > 0)
            {
                reservation = new() { Id = Guid.NewGuid(), MaterialBatchId = batch.Id, WarehouseId = plan.ProductionWarehouseId,
                    ProductionScheduleProductId = plan.Batch.Id, WarehouseLocationShelfId = shelf.WarehouseLocationShelfId,
                    UoMId = shelf.UoMId.GetValueOrDefault(), Quantity = allocation.Adjustment };
                db.MaterialBatchReservedQuantities.Add(reservation);
            }
            else
            {
                reservation = allocation.Reservation;
                reservation.Quantity += allocation.Adjustment;
                // Keep a zero reservation live: deletion means consumption in the existing lifecycle.
            }
            var movement = new MassMaterialBatchMovement { Id = Guid.NewGuid(), BatchId = batch.Id,
                FromWarehouseId = allocation.Adjustment > 0 ? plan.SourceWarehouseId : plan.ProductionWarehouseId,
                ToWarehouseId = allocation.Adjustment > 0 ? plan.ProductionWarehouseId : plan.SourceWarehouseId,
                Quantity = Math.Abs(allocation.Adjustment), MovedAt = now, MovedById = actor };
            db.MassMaterialBatchMovements.Add(movement);
            db.MaterialBatchEvents.Add(new() { Id = Guid.NewGuid(), BatchId = batch.Id, Quantity = Math.Abs(allocation.Adjustment),
                UserId = actor, Type = EventType.Moved });
            balances[batch.MaterialId] = balances.GetValueOrDefault(batch.MaterialId) - allocation.Adjustment;
            var card = new BinCardInformation { Id = Guid.NewGuid(), MaterialBatchId = batch.Id, WarehouseId = plan.SourceWarehouseId,
                UoMId = batch.UoMId, ProductId = plan.Batch.ProductId, ProductBatchNumber = plan.Batch.BatchNumber,
                RequisitionId = plan.Requisition.Id, RequisitionCode = plan.Requisition.Code,
                QuantityIssued = Math.Max(0, allocation.Adjustment), QuantityReceived = Math.Max(0, -allocation.Adjustment),
                BalanceQuantity = balances[batch.MaterialId], Description = $"Post-issue {plan.Preview.Kind} repair {audit.Id}" };
            db.BinCardInformation.Add(card);
            audit.Allocations.Add(new() { Id = Guid.NewGuid(), MaterialId = batch.MaterialId, MaterialBatchId = batch.Id,
                ShelfId = shelf.WarehouseLocationShelfId, ReservationId = reservation.Id, MovementId = movement.Id,
                BinCardId = card.Id, Adjustment = allocation.Adjustment, ShelfBefore = shelfBefore, ShelfAfter = shelf.Quantity,
                AssignedBefore = assignedBefore, AssignedAfter = batch.QuantityAssigned });
        }
        var activity = await db.ProductionActivities.SingleAsync(item => item.ProductionScheduleProductId == plan.Batch.Id);
        // A consumption step that loaded before this correction must reload its stock snapshot.
        db.Entry(activity).Property(item => item.UpdatedAt).IsModified = true;
        var message = $"Post-issue {plan.Preview.Kind} repair {audit.Id}; {audit.Reason}";
        db.ProductionActivityLogs.Add(new() { Id = Guid.NewGuid(), ProductionActivityId = activity.Id, UserId = actor, Timestamp = now,
            Message = message.Length > 1000 ? message[..1000] : message });
        return audit;
    }
}
