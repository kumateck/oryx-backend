using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.FinishedGoods;
using DOMAIN.Entities.Payments;
using DOMAIN.Entities.ProductionOrders;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FinishedGoods;

public sealed partial class FinishedGoodsWorkflow
{
    public Task<Result> VoidInvoice(Guid invoiceId, RecallEvidenceRequest request) => Write(PermissionKeys.CanVoidFinishedGoodsInvoice, async () =>
    {
        var invoice = await db.Invoices.Include(i => i.ProformaInvoice).FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice is null || invoice.Voided) return Error.Validation("FinishedGoods.Invoice", "Choose an issued invoice that has not been voided.");
        var allocation = await Allocation(invoice.ProformaInvoice.AllocateProductionOrderId);
        if (allocation.DispatchedAt.HasValue || await db.Payments.AnyAsync(p => p.PayableType == PayableType.CustomerInvoice && p.PayableId == invoiceId && p.Status != PaymentStatus.Rejected))
            return Error.Validation("FinishedGoods.InvoiceLocked", "A dispatched or paid invoice requires a financial credit/reversal; it cannot be voided.");
        if (string.IsNullOrWhiteSpace(request.Evidence)) return Error.Validation("FinishedGoods.Reason", "Record the finance-authorised reason and evidence for voiding this invoice.");
        invoice.Voided = true; invoice.VoidedAt = DateTime.UtcNow; invoice.VoidedById = db.CurrentActorId; invoice.VoidReason = request.Evidence.Trim();
        var staleWaybills = await db.ProductionOrderWaybills.Where(w => w.AllocateProductionOrderId == allocation.Id && !w.Voided).ToListAsync();
        foreach (var waybill in staleWaybills) { waybill.Voided = true; waybill.VoidReason = $"Linked invoice voided: {request.Evidence.Trim()}"; }
        allocation.WaybillSentToCustomerAt = null;
        allocation.CommercialClearedAt = null;
        invoice.ProformaInvoice.IssuedInvoiceId = null;
        invoice.ProformaInvoice.IssuedInvoiceCode = null;
        invoice.ProformaInvoice.InvoiceIssuedAt = null;
        return await Save(PermissionKeys.CanVoidFinishedGoodsInvoice);
    });

    public Task<Result> Cancel(Guid allocationId, RecallEvidenceRequest request) => Write(PermissionKeys.CanCancelFinishedGoodsAllocation, async () =>
    {
        var allocation = await Allocation(allocationId);
        if (allocation is null || allocation.DispatchedAt.HasValue || allocation.DeliveredAt.HasValue || allocation.CancelledAt.HasValue)
            return Error.Validation("FinishedGoods.CancelState", "Only an undeparted packing list can be cancelled.");
        if (string.IsNullOrWhiteSpace(request.Evidence)) return Error.Validation("FinishedGoods.UnloadEvidence", "Record the reason and confirm the goods were counted back into their batch location, including unloading any truck.");
        if (await db.Invoices.AnyAsync(i => i.ProformaInvoice.AllocateProductionOrderId == allocationId && !i.Voided))
            return Error.Validation("FinishedGoods.Invoice", "Finance must void the undeparted invoice before cancelling its packing list.");
        await db.ProductionOrders.Include(o => o.Products).ThenInclude(p => p.FulfilledQuantities).SingleAsync(o => o.Id == allocation.ProductionOrderId);
        foreach (var product in allocation.Products)
        {
            var ordered = allocation.ProductionOrder.Products.Single(p => p.ProductId == product.ProductId && p.ProductPackingId == product.ProductPackingId);
            foreach (var line in product.FulfilledQuantities)
            {
                var note = line.FinishedGoodsTransferNote;
                if (!note.RequiresStockReconciliation && (line.YieldQuantity <= 0 || note.ReservedQuantity < line.YieldQuantity))
                    return Error.Validation("FinishedGoods.Reconcile", "Reconcile this legacy allocation's pack/yield quantities before cancellation.");
                var fulfilled = ordered.FulfilledQuantities.FirstOrDefault(q => q.FinishedGoodsTransferNoteId == line.FinishedGoodsTransferNoteId);
                if (fulfilled is not null) { fulfilled.Quantity -= line.Quantity; fulfilled.YieldQuantity -= line.YieldQuantity; }
                if (!note.RequiresStockReconciliation) note.AllocatedQuantity -= line.YieldQuantity; ordered.Fulfilled = false;
            }
        }
        allocation.ProductionOrder.Status = allocation.ProductionOrder.Products.Any(p => p.FulfilledQuantities.Sum(q => q.Quantity) > 0)
            ? ProductionOrderStatus.PartialPackingReady : ProductionOrderStatus.Pending;
        var waybill = await db.ProductionOrderWaybills.FirstOrDefaultAsync(w => w.AllocateProductionOrderId == allocationId);
        if (waybill is not null) { waybill.Voided = true; waybill.VoidReason = request.Evidence.Trim(); }
        allocation.CancelledAt = DateTime.UtcNow; allocation.CancelledById = db.CurrentActorId;
        allocation.CancellationReason = request.Evidence.Trim(); allocation.Status = AllocateProductionOrderStatus.Cancelled;
        var refreshed = await RefreshOrderCounts(allocation.ProductionOrderId, allocation.Id);
        if (refreshed.IsFailure) return refreshed;
        return await Save(PermissionKeys.CanCancelFinishedGoodsAllocation);
    });

    public Task<Result> DisposeOnSite(Guid id, DisposeRecalledStockRequest request) => Write(PermissionKeys.CanDisposeRecalledFinishedGoodsStock, async () =>
    {
        var recall = await db.FinishedGoodsRecalls.SingleOrDefaultAsync(r => r.Id == id);
        var note = await db.FinishedGoodsTransferNotes.SingleOrDefaultAsync(n => n.Id == request.TransferNoteId);
        if (recall is null || recall.Closed || note is null || note.RequiresStockReconciliation || note.BatchManufacturingRecordId != recall.BatchManufacturingRecordId || !note.IsOnHold)
            return Error.Validation("Recall.Stock", "Choose stock held under this open recall.");
        if (request.Quantity <= 0 || request.Quantity > note.PhysicalQuantity - note.ReservedQuantity - note.QuarantinedReturnQuantity || string.IsNullOrWhiteSpace(request.Evidence))
            return Error.Validation("Recall.StockDisposition", "Cancel/unload affected reservations first, then record the unreserved quantity destroyed and QA disposal evidence. Recovered returns use their own disposition task.");
        note.AdjustmentQuantity -= request.Quantity;
        await FinishedGoodsStock.Post(db, note, -request.Quantity, "FgRecallStockDisposal", Guid.NewGuid().ToString(), db.CurrentActorId.Value, request.Evidence.Trim());
        return await Save(PermissionKeys.CanDisposeRecalledFinishedGoodsStock);
    });

    public Task<Result> Reconcile(Guid id, ReconcileFinishedGoodsStockRequest request) => Write(PermissionKeys.CanReconcileFinishedGoodsStock, async () =>
    {
        var note = await db.FinishedGoodsTransferNotes.SingleOrDefaultAsync(n => n.Id == id);
        if (note is null || !note.IsApproved || !note.RequiresStockReconciliation)
            return Error.Validation("FinishedGoods.ReconciliationState", "Choose an unreconciled historical receipt.");
        if (request.CountedPhysicalYield < 0 || request.VerifiedUnitsPerPack <= 0 || request.VerifiedPacksPerShipper < 0 || request.VerifiedPacksPerShipper > 1000000
            || string.IsNullOrWhiteSpace(request.VerifiedPackingStyleName) || request.VerifiedPackingStyleName.Length > 1000 || string.IsNullOrWhiteSpace(request.Evidence) || request.Evidence.Length > 500
            || request.Departures is null || request.Departures.Select(d => d.AllocationId).Distinct().Count() != request.Departures.Count)
            return Error.Validation("FinishedGoods.Count", "Record the physical count, verified base units per pack and signed reconciliation evidence.");
        var allocations = await db.AllocateProductionOrders.Include(a => a.ProductionOrder).ThenInclude(o => o.Products).ThenInclude(p => p.FulfilledQuantities)
            .Include(a => a.Products).ThenInclude(p => p.FulfilledQuantities).AsSplitQuery()
            .Where(a => a.Products.Any(p => p.FulfilledQuantities.Any(q => q.FinishedGoodsTransferNoteId == id))).ToListAsync();
        foreach (var departure in request.Departures)
        {
            var allocation = allocations.FirstOrDefault(a => a.Id == departure.AllocationId && !a.CancelledAt.HasValue);
            if (allocation is null || departure.DepartedAt == default || departure.DepartedAt > DateTime.UtcNow || (allocation.DeliveredAt.HasValue && departure.DepartedAt > allocation.DeliveredAt) || string.IsNullOrWhiteSpace(departure.Evidence))
                return Error.Validation("FinishedGoods.DepartureEvidence", "Every historical departure needs its allocation, actual date and evidence.");
            if (allocation.DispatchedAt.HasValue && allocation.DispatchedAt != departure.DepartedAt)
                return Error.Conflict("FinishedGoods.DepartureImmutable", "An already-evidenced departure cannot be rewritten.");
            allocation.DispatchedAt = DateTime.SpecifyKind(departure.DepartedAt, DateTimeKind.Utc);
            allocation.ExitReference = departure.Evidence.Trim();
        }
        if (allocations.Any(a => a.DeliveredAt.HasValue && !a.DispatchedAt.HasValue))
            return Error.Validation("FinishedGoods.LegacyDelivery", "Provide departure evidence for historical delivered consignments; their old status alone is insufficient.");
        decimal allocated = 0, dispatched = 0;
        foreach (var allocation in allocations.Where(a => !a.CancelledAt.HasValue))
        foreach (var product in allocation.Products)
        foreach (var line in product.FulfilledQuantities.Where(q => q.FinishedGoodsTransferNoteId == id))
        {
            if (line.Quantity <= 0) return Error.Validation("FinishedGoods.LegacyQuantity", "Resolve invalid historical pack quantities before reconciliation.");
            line.UnitsPerPack = request.VerifiedUnitsPerPack;
            line.YieldQuantity = line.Quantity * request.VerifiedUnitsPerPack;
            allocated += line.YieldQuantity;
            if (allocation.DispatchedAt.HasValue) dispatched += line.YieldQuantity;
            var ordered = allocation.ProductionOrder.Products.FirstOrDefault(p => p.ProductId == product.ProductId && p.ProductPackingId == product.ProductPackingId);
            var fulfilled = ordered?.FulfilledQuantities.FirstOrDefault(q => q.FinishedGoodsTransferNoteId == id);
            if (fulfilled is not null) { fulfilled.UnitsPerPack = request.VerifiedUnitsPerPack; fulfilled.YieldQuantity = fulfilled.Quantity * request.VerifiedUnitsPerPack; }
        }
        if (request.CountedPhysicalYield < allocated - dispatched)
            return Error.Validation("FinishedGoods.Reservations", "The count is below the reserved on-site quantity. Resolve or cancel affected packing lists first.");
        var before = $"received={note.QuantityReceived}; adjustment={note.AdjustmentQuantity}; allocated={note.AllocatedQuantity}; dispatched={note.DispatchedQuantity}; units/pack={note.QuantityPerPack}";
        note.QuantityPerPack = request.VerifiedUnitsPerPack;
        note.PacksPerShipperSnapshot = request.VerifiedPacksPerShipper;
        note.PackingStyleSnapshot = request.VerifiedPackingStyleName.Trim();
        note.AllocatedQuantity = allocated; note.DispatchedQuantity = dispatched;
        note.AdjustmentQuantity = request.CountedPhysicalYield + dispatched - note.QuantityReceived - note.ReleasedReturnQuantity - note.QuarantinedReturnQuantity;
        foreach (var orderId in allocations.Select(a => a.ProductionOrderId).Distinct()) {
            var refreshed = await RefreshOrderCounts(orderId);
            if (refreshed.IsFailure) return refreshed;
        }
        note.RequiresStockReconciliation = false;
        var batchNotes = await db.FinishedGoodsTransferNotes.IgnoreAutoIncludes().Where(n => n.BatchManufacturingRecordId == note.BatchManufacturingRecordId).ToListAsync();
        var oldNet = await db.ProductBinCardInformation.Where(c => c.BatchId == note.BatchManufacturingRecordId).SumAsync(c => c.QuantityReceived - c.QuantityIssued);
        var correction = batchNotes.Sum(n => n.PhysicalQuantity) - oldNet;
        await FinishedGoodsStock.Post(db, note, correction, "FgReconciliation", id.ToString(), db.CurrentActorId.Value,
            $"Before {before}. Count={request.CountedPhysicalYield}. Evidence: {request.Evidence.Trim()}");
        return await Save(PermissionKeys.CanReconcileFinishedGoodsStock);
    });

    private async Task<Result> RefreshOrderCounts(Guid orderId, Guid? excludedAllocation = null)
    {
        var order = await db.ProductionOrders.Include(o => o.Products).ThenInclude(p => p.FulfilledQuantities).SingleAsync(o => o.Id == orderId);
        var lists = await db.AllocateProductionOrders.IgnoreAutoIncludes().AsSplitQuery().Include(a => a.Products).ThenInclude(p => p.FulfilledQuantities)
            .Where(a => a.ProductionOrderId == orderId && !a.CancelledAt.HasValue && a.Id != excludedAllocation).ToListAsync();
        var active = lists.Where(a => !a.CancelledAt.HasValue).SelectMany(a => a.Products).ToList();
        if (active.Any(p => !order.Products.Any(o => o.ProductId == p.ProductId && o.ProductPackingId == p.ProductPackingId)))
            return Error.Validation("FinishedGoods.OrderLink", "A historical packing list has no matching product packing style on its order. Resolve that document link before reconciliation.");
        foreach (var product in order.Products)
        {
            var sources = active.Where(p => p.ProductId == product.ProductId && p.ProductPackingId == product.ProductPackingId)
                .SelectMany(p => p.FulfilledQuantities).GroupBy(q => q.FinishedGoodsTransferNoteId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var old in product.FulfilledQuantities.Where(q => !sources.ContainsKey(q.FinishedGoodsTransferNoteId)).ToList()) product.FulfilledQuantities.Remove(old);
            foreach (var (noteId, lines) in sources)
            {
                var target = product.FulfilledQuantities.FirstOrDefault(q => q.FinishedGoodsTransferNoteId == noteId);
                if (target is null) { target = new ProductionOrderProductQuantity { FinishedGoodsTransferNoteId = noteId }; product.FulfilledQuantities.Add(target); }
                target.Quantity = lines.Sum(q => q.Quantity); target.YieldQuantity = lines.Sum(q => q.YieldQuantity);
                target.UnitsPerPack = lines.Select(q => q.UnitsPerPack).Distinct().Count() == 1 ? lines[0].UnitsPerPack : 0;
            }
            product.Fulfilled = product.RemainingQuantity <= 0;
        }
        order.Status = order.Products.All(p => p.Fulfilled) ? ProductionOrderStatus.FullPackingReady
            : order.Products.Any(p => p.FulfilledQuantities.Sum(q => q.Quantity) > 0) ? ProductionOrderStatus.PartialPackingReady : ProductionOrderStatus.Pending;
        return Result.Success();
    }
}
