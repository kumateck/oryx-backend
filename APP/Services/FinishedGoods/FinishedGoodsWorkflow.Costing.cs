using APP.Utils;
using DOMAIN.Entities.FinishedGoods;
using DOMAIN.Entities.ProductionOrders;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FinishedGoods;

public sealed partial class FinishedGoodsWorkflow
{
    public Task<Result> ConfirmCosting(Guid id, ConfirmFinishedGoodsCostingRequest request) => Write(PermissionKeys.CanConfirmFinishedGoodsCosting, async () =>
    {
        var allocation = await Allocation(id); var problem = Ready(allocation); if (problem is not null) return problem;
        if (allocation.DispatchedAt.HasValue || allocation.CostingConfirmedAt.HasValue)
            return Error.Conflict("FinishedGoods.CostingLocked", "The saved costing basis is immutable once confirmed or dispatched.");
        if (!request.ApprovedBasisConfirmed || request.Lines is null || request.Lines.Count != allocation.Products.Count
            || request.Lines.Select(l => new { l.ProductId, l.ProductPackingId }).Distinct().Count() != request.Lines.Count
            || request.Lines.Any(l => l.ManufacturingCostPerPack <= 0 || string.IsNullOrWhiteSpace(l.ApprovedCostBasisReference)
                || l.ApprovedCostBasisReference.Length > 700 || !allocation.Products.Any(p => p.ProductId == l.ProductId && p.ProductPackingId == l.ProductPackingId)))
            return Error.Validation("FinishedGoods.Costing", "Confirm an approved manufacturing cost per complete pack, currency and source reference for every allocated product packing style.");
        var currencies = request.Lines.Select(l => l.CurrencyId).Distinct().ToList();
        if (await db.Currencies.CountAsync(c => currencies.Contains(c.Id)) != currencies.Count)
            return Error.Validation("FinishedGoods.CostCurrency", "Choose valid currencies for the approved cost basis.");
        allocation.CostingLines = request.Lines.Select(l => new FinishedGoodsCostingLine { ProductId = l.ProductId, ProductPackingId = l.ProductPackingId,
            ManufacturingCostPerPack = l.ManufacturingCostPerPack, CurrencyId = l.CurrencyId, ApprovedCostBasisReference = l.ApprovedCostBasisReference.Trim() }).ToList();
        allocation.CostingConfirmedAt = DateTime.UtcNow; allocation.CostingConfirmedById = db.CurrentActorId;
        return await Save(PermissionKeys.CanConfirmFinishedGoodsCosting);
    });

    public Task<Result> RecordSettlement(Guid id, RecordProductRecallSettlementRequest request) => Write(PermissionKeys.CanRecordProductRecallSettlement, async () =>
    {
        var recall = await db.FinishedGoodsRecalls.Where(r => r.Id == id).Select(r => new {
            r.BatchManufacturingRecordId, ProductId = r.BatchManufacturingRecord.ProductionScheduleProduct.ProductId }).FirstOrDefaultAsync();
        if (recall is null) return Error.NotFound("Recall.NotFound", "Recall not found.");
        if (request.Kind is not ("Credit" or "Refund" or "Replacement") || string.IsNullOrWhiteSpace(request.Reference)
            || request.Reference.Length > 100 || string.IsNullOrWhiteSpace(request.Evidence) || request.Evidence.Length > 2000)
            return Error.Validation("Recall.Settlement", "Record the approved financial resolution type, unique external document reference and evidence.");
        var invoice = await db.Invoices.Include(i => i.Amounts).Include(i => i.ProformaInvoice)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && !i.Voided && i.ProformaInvoice.AllocateProductionOrder.Products
                .Any(p => p.FulfilledQuantities.Any(q => q.FinishedGoodsTransferNote.BatchManufacturingRecordId == recall.BatchManufacturingRecordId)));
        if (invoice is null) return Error.Validation("Recall.SettlementInvoice", "Choose an issued invoice for a consignment containing this recalled batch.");
        if (await db.FinishedGoodsRecallSettlements.AnyAsync(s => s.FinishedGoodsRecallId == id && s.Reference == request.Reference.Trim()))
            return Error.Conflict("Recall.SettlementExists", "This financial resolution reference has already been recorded.");
        var invoiced = invoice.Amounts.Where(a => a.CurrencyId == request.CurrencyId).Sum(a => a.Amount);
        var resolved = await db.FinishedGoodsRecallSettlements.Where(s => s.InvoiceId == invoice.Id && s.CurrencyId == request.CurrencyId && s.Kind == request.Kind).SumAsync(s => s.Amount);
        if (!invoice.Amounts.Any(a => a.CurrencyId == request.CurrencyId) || request.Amount < 0 || (request.Kind == "Replacement" && request.Amount != 0) || (request.Kind != "Replacement" && (request.Amount <= 0 || request.Amount + resolved > invoiced)))
            return Error.Validation("Recall.SettlementAmount", "Use the invoice currency and an amount within its unresolved invoiced value for this resolution type.");
        if (request.Kind == "Replacement" && (!request.ReplacementProductionOrderId.HasValue || !await db.ProductionOrders.AnyAsync(o =>
            o.Id == request.ReplacementProductionOrderId && o.Approved && o.CustomerId == invoice.CustomerId && o.Products.Any(p => p.ProductId == recall.ProductId))))
            return Error.Validation("Recall.Replacement", "Link the approved replacement order for the same customer and recalled product.");
        db.FinishedGoodsRecallSettlements.Add(new FinishedGoodsRecallSettlement {
            FinishedGoodsRecallId = id, InvoiceId = invoice.Id, Kind = request.Kind, CurrencyId = request.CurrencyId, Amount = request.Amount,
            Reference = request.Reference.Trim(), Evidence = request.Evidence.Trim(), ReplacementProductionOrderId = request.ReplacementProductionOrderId,
            CreatedAt = DateTime.UtcNow, CreatedById = db.CurrentActorId,
        });
        // This is evidence of the Finance document, not a second posting into the receivables ledger.
        // Physical recoveries and available stock are deliberately unaffected by a credit or refund.
        return await Save(PermissionKeys.CanRecordProductRecallSettlement);
    });
}
