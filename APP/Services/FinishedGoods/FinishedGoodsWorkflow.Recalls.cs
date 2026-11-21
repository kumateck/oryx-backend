using APP.Services.Production;
using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.FinishedGoods;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FinishedGoods;

public sealed partial class FinishedGoodsWorkflow
{
    public Task<Result> Hold(Guid noteId, FinishedGoodsHoldRequest request) => Write(
        request.Hold ? PermissionKeys.CanHoldFinishedGoods : PermissionKeys.CanReleaseFinishedGoodsHold, async () =>
    {
        var note = await db.FinishedGoodsTransferNotes.FirstOrDefaultAsync(n => n.Id == noteId);
        if (note is null) return Error.NotFound("FinishedGoods.Note", "Transfer note not found.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000) return Error.Validation("FinishedGoods.Reason", "Record the QA decision and evidence.");
        if (!request.Hold && await db.FinishedGoodsRecalls.AnyAsync(r => r.BatchManufacturingRecordId == note.BatchManufacturingRecordId))
            return Error.Validation("FinishedGoods.RecallHold", "Recalled stock must remain blocked. Recall closure does not authorise resale.");
        // A quality hold follows the batch, not just one transfer or packing style.
        var notes = await db.FinishedGoodsTransferNotes.Where(n => n.BatchManufacturingRecordId == note.BatchManufacturingRecordId).ToListAsync();
        foreach (var batchNote in notes) { batchNote.IsOnHold = request.Hold; batchNote.HoldReason = request.Reason.Trim(); }
        db.InventoryLedgers.Add(new DOMAIN.Entities.InventoryLedgers.InventoryLedger {
            TransactionType = request.Hold ? "FgHold" : "FgRelease", ReferenceId = Guid.NewGuid().ToString(), FinishedGoodsTransferNoteId = noteId,
            Notes = request.Reason.Trim(), PostTransactionBalance = note.PhysicalQuantity, CreatedById = db.CurrentActorId, CreatedAt = DateTime.UtcNow,
        });
        return await Save(request.Hold ? PermissionKeys.CanHoldFinishedGoods : PermissionKeys.CanReleaseFinishedGoodsHold);
    });

    public Task<Result> Recall(CreateFinishedGoodsRecallRequest request) => Write(PermissionKeys.CanCreateProductRecall, async () =>
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 100 || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 2000
            || string.IsNullOrWhiteSpace(request.RegulatoryClassification) || request.RegulatoryClassification.Length > 100 || string.IsNullOrWhiteSpace(request.AuthorityReference) || request.AuthorityReference.Length > 1000)
            return Error.Validation("Recall.Header", "Record the recall number, reason, human-assigned classification and authority/notification reference.");
        var notes = await db.FinishedGoodsTransferNotes.Where(n => n.BatchManufacturingRecordId == request.BatchManufacturingRecordId).ToListAsync();
        if (notes.Count == 0) return Error.NotFound("Recall.Batch", "No finished goods transfers exist for this batch.");
        if (await db.FinishedGoodsRecalls.AnyAsync(r => r.BatchManufacturingRecordId == request.BatchManufacturingRecordId || r.Code == request.Code.Trim()))
            return Error.Conflict("Recall.Exists", "A recall already exists for this batch or number.");
        db.FinishedGoodsRecalls.Add(new FinishedGoodsRecall {
            BatchManufacturingRecordId = request.BatchManufacturingRecordId, Code = request.Code.Trim(), Reason = request.Reason.Trim(),
            RegulatoryClassification = request.RegulatoryClassification.Trim(), AuthorityReference = request.AuthorityReference.Trim(),
            CreatedById = db.CurrentActorId, CreatedAt = DateTime.UtcNow,
        });
        foreach (var note in notes) { note.IsOnHold = true; note.HoldReason = $"Recall {request.Code.Trim()}: {request.Reason.Trim()}"; }
        return await Save(PermissionKeys.CanCreateProductRecall);
    });

    public Task<Result> UpdateRecallDecision(Guid id, UpdateFinishedGoodsRecallDecisionRequest request) => Write(PermissionKeys.CanUpdateProductRecallDecision, async () =>
    {
        var recall = await db.FinishedGoodsRecalls.SingleOrDefaultAsync(r => r.Id == id);
        if (recall is null || recall.Closed) return Error.Validation("Recall.State", "Update the authority decision on an open recall; reopen a closed case first.");
        if (string.IsNullOrWhiteSpace(request.RegulatoryClassification) || request.RegulatoryClassification.Length > 100 || string.IsNullOrWhiteSpace(request.AuthorityReference)
            || request.AuthorityReference.Length > 1000 || string.IsNullOrWhiteSpace(request.Evidence) || request.Evidence.Length > 700)
            return Error.Validation("Recall.AuthorityDecision", "Record the human-assigned classification, authority notification reference and supporting evidence.");
        var evidence = $"Previous: {recall.RegulatoryClassification} / {recall.AuthorityReference}. Updated: {request.RegulatoryClassification.Trim()} / {request.AuthorityReference.Trim()}. Evidence: {request.Evidence.Trim()}";
        var decisionId = Guid.NewGuid().ToString("N"); var parts = (int)Math.Ceiling(evidence.Length / 850m);
        for (var part = 0; part < parts; part++) db.InventoryLedgers.Add(new DOMAIN.Entities.InventoryLedgers.InventoryLedger {
            TransactionType = "FgRecallDecision", ReferenceId = id.ToString(), CreatedAt = DateTime.UtcNow, CreatedById = db.CurrentActorId,
            Notes = $"Authority decision {decisionId} ({part + 1}/{parts}): " + evidence.Substring(part * 850, Math.Min(850, evidence.Length - part * 850)),
        });
        recall.RegulatoryClassification = request.RegulatoryClassification.Trim(); recall.AuthorityReference = request.AuthorityReference.Trim();
        return await Save(PermissionKeys.CanUpdateProductRecallDecision);
    });

    public async Task<Paginateable<IEnumerable<FinishedGoodsRecallSummaryDto>>> Recalls(string search, int page = 1, int pageSize = 30, DOMAIN.Entities.Products.Division? division = null)
    {
        var query = db.FinishedGoodsRecalls.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.WhereSearch(search, r => r.Code, r => r.Reason, r => r.BatchManufacturingRecord.BatchNumber);
        if (division.HasValue) query = query.Where(r => r.BatchManufacturingRecord.ProductionScheduleProduct.Product.Division == division.Value);
        var rows = query.OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id).Select(r => new FinishedGoodsRecallSummaryDto {
            Id = r.Id, Code = r.Code, BatchManufacturingRecordId = r.BatchManufacturingRecordId, BatchNumber = r.BatchManufacturingRecord.BatchNumber,
            ProductName = r.BatchManufacturingRecord.ProductionScheduleProduct.Product.Name, Reason = r.Reason, RegulatoryClassification = r.RegulatoryClassification,
            AuthorityReference = r.AuthorityReference, Closed = r.Closed, CreatedAt = r.CreatedAt, ClosedAt = r.ClosedAt, ClosureReport = r.ClosureReport,
        });
        return await PaginationHelper.GetPaginatedResultAsync(rows, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), row => row);
    }

    public async Task<Result<FinishedGoodsRecallDetailDto>> RecallDetail(Guid id)
    {
        var recall = await db.FinishedGoodsRecalls.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking().Where(r => r.DeletedAt == null).Include(r => r.BatchManufacturingRecord).ThenInclude(b => b.ProductionScheduleProduct)
            .ThenInclude(p => p.Product).Include(r => r.Contacts).Include(r => r.Recoveries).AsSplitQuery().FirstOrDefaultAsync(r => r.Id == id);
        if (recall is null) return Error.NotFound("Recall.NotFound", "Recall not found.");
        var batchId = recall.BatchManufacturingRecordId;
        var allocations = await db.AllocateProductionOrders.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking().AsSplitQuery()
            .Where(a => a.Products.Any(p => p.FulfilledQuantities.Any(q => q.FinishedGoodsTransferNote.BatchManufacturingRecordId == batchId)))
            .Include(a => a.ProductionOrder).ThenInclude(o => o.Customer)
            .Include(a => a.Products).ThenInclude(p => p.FulfilledQuantities).ThenInclude(q => q.FinishedGoodsTransferNote)
            .ToListAsync();
        var ids = allocations.Select(a => a.Id).ToList();
        var waybills = await db.ProductionOrderWaybills.IgnoreQueryFilters().AsNoTracking().Where(w => ids.Contains(w.AllocateProductionOrderId)).OrderBy(w => w.Voided).ThenByDescending(w => w.CreatedAt).ToListAsync();
        var notes = await db.FinishedGoodsTransferNotes.IgnoreAutoIncludes().AsNoTracking().Where(n => n.BatchManufacturingRecordId == batchId).ToListAsync();
        return new FinishedGoodsRecallDetailDto {
            TransferVariance = notes.Where(n => n.IsApproved).Sum(n => Math.Max(0, n.TotalQuantity - n.QuantityReceived)),
            History = await db.InventoryLedgers.AsNoTracking().Where(l => l.ReferenceId == id.ToString() && (l.TransactionType == "FgRecallClose" || l.TransactionType == "FgRecallReopen" || l.TransactionType == "FgRecallDecision"))
                .OrderBy(l => l.CreatedAt).ThenBy(l => l.Id).Select(l => new FinishedGoodsRecallHistoryDto { Id = l.Id, Event = l.TransactionType,
                    Evidence = l.Notes, Actor = l.CreatedBy.FirstName + " " + l.CreatedBy.LastName, At = l.CreatedAt }).ToListAsync(),
            Settlements = await db.FinishedGoodsRecallSettlements.AsNoTracking().Where(s => s.FinishedGoodsRecallId == id)
                .OrderBy(s => s.CreatedAt).Select(s => new FinishedGoodsRecallSettlementDto { Id = s.Id, InvoiceCode = s.Invoice.Code,
                    Kind = s.Kind, Amount = s.Amount, Currency = s.Currency.Symbol, Reference = s.Reference, Evidence = s.Evidence,
                    ReplacementProductionOrderId = s.ReplacementProductionOrderId, CreatedAt = s.CreatedAt }).ToListAsync(),
            Id = id, Code = recall.Code, BatchManufacturingRecordId = batchId, BatchNumber = recall.BatchManufacturingRecord.BatchNumber,
            ProductName = recall.BatchManufacturingRecord.ProductionScheduleProduct.Product.Name, Reason = recall.Reason,
            RegulatoryClassification = recall.RegulatoryClassification, AuthorityReference = recall.AuthorityReference, Closed = recall.Closed,
            CreatedAt = recall.CreatedAt, ClosedAt = recall.ClosedAt, ClosureReport = recall.ClosureReport, UnrecoverableYield = recall.UnrecoverableYield, UnrecoverableReason = recall.UnrecoverableReason,
            Stocks = notes.Select(n => new FinishedGoodsRecallStockDto { TransferNoteId = n.Id, TransferNoteNumber = n.TransferNoteNumber,
                PackingStyle = n.PackingStyleSnapshot, UnitsPerPack = n.QuantityPerPack, Physical = n.PhysicalQuantity,
                Reserved = n.ReservedQuantity, QuarantinedReturns = n.QuarantinedReturnQuantity }).ToList(),
            OnSiteYield = notes.Sum(n => n.PhysicalQuantity), DispatchedYield = notes.Sum(n => n.DispatchedQuantity),
            RecoveredYield = recall.Recoveries.Sum(r => r.Quantity),
            Consignments = allocations.SelectMany(a => a.Products.SelectMany(p => p.FulfilledQuantities)
                .Where(q => q.FinishedGoodsTransferNote.BatchManufacturingRecordId == batchId).Select(q => {
                    var waybill = waybills.FirstOrDefault(w => w.AllocateProductionOrderId == a.Id);
                    var contact = recall.Contacts.FirstOrDefault(c => c.AllocationId == a.Id);
                    return new FinishedGoodsTraceDto {
                        AllocationId = a.Id, TransferNoteId = q.FinishedGoodsTransferNoteId, TransferNoteNumber = q.FinishedGoodsTransferNote.TransferNoteNumber,
                        BatchNumber = recall.BatchManufacturingRecord.BatchNumber, ProductName = recall.BatchManufacturingRecord.ProductionScheduleProduct.Product.Name,
                        PackingStyle = q.FinishedGoodsTransferNote.PackingStyleSnapshot, Customer = a.ProductionOrder.Customer.Name,
                        Contact = $"{a.ProductionOrder.Customer.Email} / {a.ProductionOrder.Customer.Phone}", DeliveryAddress = waybill?.DeliveryAddress ?? a.ProductionOrder.Customer.ShippingAddress,
                        Stage = a.CancelledAt.HasValue ? "Cancelled on site" : a.DeliveredAt.HasValue ? "Delivered" : a.DispatchedAt.HasValue ? "In transit" : a.LoadedAt.HasValue ? "Loaded on site" : "Reserved on site",
                        Packs = q.Quantity, Yield = q.YieldQuantity, Waybill = waybill?.Code, Vehicle = a.LoadedVehicle ?? waybill?.VehicleRegistration,
                        DispatchedAt = a.DispatchedAt, DeliveredAt = a.DeliveredAt, Notified = contact is not null, Acknowledged = contact?.AcknowledgedAt is not null, EffectivenessConfirmed = contact?.EffectivenessConfirmed == true,
                        RecoveredYield = recall.Recoveries.Where(r => r.AllocationId == a.Id && r.FinishedGoodsTransferNoteId == q.FinishedGoodsTransferNoteId).Sum(r => r.Quantity),
                    };
                })).ToList(),
            Recoveries = recall.Recoveries.Select(r => new FinishedGoodsRecallRecoveryDto { Id = r.Id, AllocationId = r.AllocationId,
                FinishedGoodsTransferNoteId = r.FinishedGoodsTransferNoteId, Quantity = r.Quantity, Evidence = r.Evidence, Condition = r.Condition,
                Disposed = r.Disposed, DispositionEvidence = r.DispositionEvidence }).ToList(),
        };
    }

    public Task<Result> Contact(Guid id, RecordRecallContactRequest request) => Write(PermissionKeys.CanNotifyProductRecall, async () =>
    {
        var recall = await db.FinishedGoodsRecalls.FirstOrDefaultAsync(r => r.Id == id);
        if (recall is null || recall.Closed) return Error.Validation("Recall.State", "Choose an open recall.");
        if (string.IsNullOrWhiteSpace(request.Evidence)) return Error.Validation("Recall.ContactEvidence", "Record notification/follow-up evidence.");
        if (!await db.AllocateProductionOrders.AnyAsync(a => a.Id == request.AllocationId && a.Products.Any(p => p.FulfilledQuantities.Any(q => q.FinishedGoodsTransferNote.BatchManufacturingRecordId == recall.BatchManufacturingRecordId))))
            return Error.Validation("Recall.Consignment", "This consignment does not contain the recalled batch.");
        var contact = await db.FinishedGoodsRecallContacts.SingleOrDefaultAsync(c => c.FinishedGoodsRecallId == id && c.AllocationId == request.AllocationId);
        if (contact is null) { contact = new FinishedGoodsRecallContact { FinishedGoodsRecallId = id, AllocationId = request.AllocationId, NotifiedAt = DateTime.UtcNow }; db.FinishedGoodsRecallContacts.Add(contact); }
        contact.Evidence = string.IsNullOrWhiteSpace(contact.Evidence) ? request.Evidence.Trim() : contact.Evidence + "\n" + request.Evidence.Trim();
        if (request.Acknowledged) contact.AcknowledgedAt ??= DateTime.UtcNow;
        if (request.EffectivenessConfirmed) contact.EffectivenessConfirmed = true;
        return await Save(PermissionKeys.CanNotifyProductRecall);
    });

    public Task<Result> Recover(Guid id, RecordRecallRecoveryRequest request) => Write(PermissionKeys.CanRecordProductRecallRecovery, async () =>
    {
        var recall = await db.FinishedGoodsRecalls.FirstOrDefaultAsync(r => r.Id == id);
        if (recall is null || recall.Closed) return Error.Validation("Recall.State", "Choose an open recall.");
        var allocation = await Allocation(request.AllocationId);
        var source = allocation?.Products.SelectMany(p => p.FulfilledQuantities).Where(q => q.FinishedGoodsTransferNoteId == request.FinishedGoodsTransferNoteId).ToList();
        var note = source?.FirstOrDefault()?.FinishedGoodsTransferNote;
        if (note is null || note.RequiresStockReconciliation || !allocation.DispatchedAt.HasValue || note.BatchManufacturingRecordId != recall.BatchManufacturingRecordId)
            return Error.Validation("Recall.Recovery", "Select an affected dispatched consignment and batch.");
        if (string.IsNullOrWhiteSpace(request.Evidence)) return Error.Validation("Recall.Receipt", "Record the recovery receipt reference and evidence.");
        if (await db.FinishedGoodsRecallRecoveries.AnyAsync(r => r.FinishedGoodsRecallId == id && r.AllocationId == request.AllocationId
            && r.FinishedGoodsTransferNoteId == request.FinishedGoodsTransferNoteId && r.Evidence == request.Evidence.Trim()))
            return Error.Conflict("Recall.ReceiptExists", "This recovery receipt has already been recorded for this consignment and batch.");
        var recovered = await db.FinishedGoodsRecallRecoveries.Where(r => r.FinishedGoodsRecallId == id && r.AllocationId == request.AllocationId
            && r.FinishedGoodsTransferNoteId == request.FinishedGoodsTransferNoteId).SumAsync(r => r.Quantity);
        if (request.Quantity <= 0 || request.Quantity + recovered > source.Sum(q => q.YieldQuantity)
            || string.IsNullOrWhiteSpace(request.Evidence) || string.IsNullOrWhiteSpace(request.Condition))
            return Error.Validation("Recall.RecoveryCount", "Record a positive recovered quantity no greater than the unrecovered dispatched quantity, with receipt evidence and condition.");
        db.FinishedGoodsRecallRecoveries.Add(new FinishedGoodsRecallRecovery { FinishedGoodsRecallId = id, AllocationId = request.AllocationId,
            FinishedGoodsTransferNoteId = note.Id, Quantity = request.Quantity, Evidence = request.Evidence.Trim(), Condition = request.Condition.Trim() });
        note.QuarantinedReturnQuantity += request.Quantity;
        await FinishedGoodsStock.Post(db, note, request.Quantity, "FgRecallRecovery", Guid.NewGuid().ToString(), db.CurrentActorId.Value, $"Recall {recall.Code}: quarantined recovery. {request.Evidence}");
        return await Save(PermissionKeys.CanRecordProductRecallRecovery);
    });

    public Task<Result> DisposeRecovery(Guid id, Guid recoveryId, RecallEvidenceRequest request) => Write(PermissionKeys.CanDisposeProductRecallRecovery, async () =>
    {
        var recovery = await db.FinishedGoodsRecallRecoveries.Include(r => r.FinishedGoodsRecall).FirstOrDefaultAsync(r => r.Id == recoveryId && r.FinishedGoodsRecallId == id);
        if (recovery is null || recovery.Disposed || recovery.FinishedGoodsRecall.Closed) return Error.Validation("Recall.Disposition", "Select an open, quarantined recovery.");
        if (string.IsNullOrWhiteSpace(request.Evidence)) return Error.Validation("Recall.DispositionEvidence", "Record QA-authorised destruction and its disposal evidence.");
        var note = await db.FinishedGoodsTransferNotes.SingleAsync(n => n.Id == recovery.FinishedGoodsTransferNoteId);
        note.QuarantinedReturnQuantity -= recovery.Quantity;
        recovery.Disposed = true; recovery.DisposedAt = DateTime.UtcNow; recovery.DisposedById = db.CurrentActorId; recovery.DispositionEvidence = request.Evidence.Trim();
        await FinishedGoodsStock.Post(db, note, -recovery.Quantity, "FgRecallDisposal", recovery.Id.ToString(), db.CurrentActorId.Value, request.Evidence.Trim());
        return await Save(PermissionKeys.CanDisposeProductRecallRecovery);
    });

    public Task<Result> CloseRecall(Guid id, CloseFinishedGoodsRecallRequest request) => Write(PermissionKeys.CanCloseProductRecall, async () =>
    {
        var recall = await db.FinishedGoodsRecalls.FirstOrDefaultAsync(r => r.Id == id);
        if (recall is null || recall.Closed) return Error.Validation("Recall.State", "Choose an open recall.");
        if (await db.FinishedGoodsTransferNotes.AnyAsync(n => n.BatchManufacturingRecordId == recall.BatchManufacturingRecordId && !n.IsApproved))
            return Error.Validation("Recall.TransferReceipts", "Account for every generated transfer, including goods still awaiting store acceptance, before closure.");
        if (request.Evidence?.Length > 700)
            return Error.Validation("Recall.Report", "Use a final report reference and summary within 700 characters; retain the detailed signed report under that reference.");
        if (await db.FinishedGoodsTransferNotes.AnyAsync(n => n.BatchManufacturingRecordId == recall.BatchManufacturingRecordId && n.RequiresStockReconciliation))
            return Error.Validation("Recall.StockReconciliation", "Reconcile all historical stock before closing this recall.");
        var detail = (await RecallDetail(id)).Value;
        if (detail.Consignments.Any(c => c.DispatchedAt.HasValue && (!c.Notified || (!c.Acknowledged && !c.EffectivenessConfirmed))) || detail.Recoveries.Any(r => !r.Disposed))
            return Error.Validation("Recall.Reconciliation", "Complete recipient acknowledgements and recovered-stock dispositions before closure.");
        if (detail.OnSiteYield != 0 || request.UnrecoverableYield < 0 || detail.RecoveredYield + request.UnrecoverableYield != detail.DispatchedYield
            || (request.UnrecoverableYield > 0 && string.IsNullOrWhiteSpace(request.UnrecoverableReason)) || string.IsNullOrWhiteSpace(request.Evidence))
            return Error.Validation("Recall.Effectiveness", "Reconcile every dispatched unit and record the effectiveness/final recall report before closure.");
        recall.UnrecoverableYield = request.UnrecoverableYield; recall.UnrecoverableReason = request.UnrecoverableReason?.Trim();
        recall.Closed = true; recall.ClosedAt = DateTime.UtcNow; recall.ClosedById = db.CurrentActorId; recall.ClosureReport = request.Evidence.Trim();
        db.InventoryLedgers.Add(new DOMAIN.Entities.InventoryLedgers.InventoryLedger {
            TransactionType = "FgRecallClose", ReferenceId = id.ToString(), Notes = request.Evidence.Trim(),
            CreatedAt = DateTime.UtcNow, CreatedById = db.CurrentActorId,
        });
        return await Save(PermissionKeys.CanCloseProductRecall);
    });

    public Task<Result> ReopenRecall(Guid id, RecallEvidenceRequest request) => Write(PermissionKeys.CanReopenProductRecall, async () =>
    {
        var recall = await db.FinishedGoodsRecalls.SingleOrDefaultAsync(r => r.Id == id);
        if (recall is null || !recall.Closed) return Error.Validation("Recall.State", "Choose a closed recall to reopen.");
        if (string.IsNullOrWhiteSpace(request.Evidence) || request.Evidence.Length > 700)
            return Error.Validation("Recall.ReopenEvidence", "Record the QA reason and evidence within 700 characters.");
        recall.Closed = false;
        db.InventoryLedgers.Add(new DOMAIN.Entities.InventoryLedgers.InventoryLedger {
            TransactionType = "FgRecallReopen", ReferenceId = id.ToString(), Notes = request.Evidence.Trim(),
            CreatedAt = DateTime.UtcNow, CreatedById = db.CurrentActorId,
        });
        return await Save(PermissionKeys.CanReopenProductRecall);
    });
}
