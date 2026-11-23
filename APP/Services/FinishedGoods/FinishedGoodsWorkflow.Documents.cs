using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FinishedGoods;

public sealed partial class FinishedGoodsWorkflow
{
    public async Task<Result<FinishedGoodsDocumentsDto>> Documents(Guid allocationId)
    {
        if (!await db.AllocateProductionOrders.AnyAsync(a => a.Id == allocationId))
            return Error.NotFound("FinishedGoods.Allocation", "Packing list not found.");
        var proforma = await db.ProformaInvoices.IgnoreAutoIncludes().AsNoTracking()
            .Where(p => p.AllocateProductionOrderId == allocationId).OrderByDescending(p => p.CreatedAt)
            .Select(p => new { p.Id, p.Code, p.CustomerPoNumber, p.CustomerPoVerifiedAt }).FirstOrDefaultAsync();
        var invoice = await db.Invoices.IgnoreAutoIncludes().AsNoTracking()
            .Where(i => i.ProformaInvoice.AllocateProductionOrderId == allocationId && !i.Voided)
            .OrderByDescending(i => i.CreatedAt).Select(i => new { i.Id, i.Code }).FirstOrDefaultAsync();
        var history = await db.Invoices.IgnoreAutoIncludes().AsNoTracking()
            .Where(i => i.ProformaInvoice.AllocateProductionOrderId == allocationId).OrderBy(i => i.CreatedAt)
            .Select(i => new FinishedGoodsInvoiceHistoryDto { Id = i.Id, Code = i.Code, Voided = i.Voided,
                VoidedAt = i.VoidedAt, VoidReason = i.VoidReason, CreatedAt = i.CreatedAt }).ToListAsync();
        var waybill = await db.ProductionOrderWaybills.IgnoreAutoIncludes().AsNoTracking()
            .Where(w => w.AllocateProductionOrderId == allocationId && !w.Voided)
            .Select(w => new { w.Id, w.Code, w.VehicleRegistration, w.DriverName, w.DriverPhone, w.Transporter, w.DeliveryAddress, w.StorageConditions, w.SealNumber }).FirstOrDefaultAsync();
        var costing = await db.AllocateProductionOrders.IgnoreAutoIncludes().AsNoTracking().Where(a => a.Id == allocationId)
            .Select(a => new { a.CostingConfirmedAt, Lines = a.CostingLines.Select(l => new FinishedGoodsCostBasisDto {
                ProductId = l.ProductId, ProductPackingId = l.ProductPackingId, ManufacturingCostPerPack = l.ManufacturingCostPerPack,
                CurrencyId = l.CurrencyId, CurrencyLabel = db.Currencies.Where(c => c.Id == l.CurrencyId).Select(c => c.Symbol).FirstOrDefault(), ApprovedCostBasisReference = l.ApprovedCostBasisReference }).ToList() }).SingleAsync();
        return new FinishedGoodsDocumentsDto {
            InvoiceHistory = history, CostingConfirmedAt = costing.CostingConfirmedAt, CostingLines = costing.Lines,
            ProformaId = proforma?.Id, ProformaCode = proforma?.Code, CustomerPoNumber = proforma?.CustomerPoNumber,
            CustomerPoVerifiedAt = proforma?.CustomerPoVerifiedAt, InvoiceId = invoice?.Id, InvoiceCode = invoice?.Code,
            WaybillId = waybill?.Id, WaybillCode = waybill?.Code, Vehicle = waybill?.VehicleRegistration,
            Driver = waybill?.DriverName, DriverPhone = waybill?.DriverPhone, Transporter = waybill?.Transporter,
            DeliveryAddress = waybill?.DeliveryAddress, StorageConditions = waybill?.StorageConditions, Seal = waybill?.SealNumber,
        };
    }
    public Task<List<FinishedGoodsBatchOptionDto>> RecallBatches(string search)
    {
        var query = db.BatchManufacturingRecords.IgnoreAutoIncludes().AsNoTracking()
            .Where(b => db.FinishedGoodsTransferNotes.Any(n => n.BatchManufacturingRecordId == b.Id));
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(b => b.BatchNumber.ToLower().Contains(search.ToLower())
            || b.ProductionScheduleProduct.Product.Name.ToLower().Contains(search.ToLower()));
        return query.OrderBy(b => b.BatchNumber).ThenBy(b => b.Id).Select(b => new FinishedGoodsBatchOptionDto {
            Id = b.Id, BatchNumber = b.BatchNumber, ProductName = b.ProductionScheduleProduct.Product.Name }).Take(50).ToListAsync();
    }
    public Task<List<FinishedGoodsReconciliationAllocationDto>> ReconciliationOptions(Guid noteId) =>
        db.AllocateProductionOrders.IgnoreAutoIncludes().AsNoTracking()
            .Where(a => a.Products.Any(p => p.FulfilledQuantities.Any(q => q.FinishedGoodsTransferNoteId == noteId)))
            .OrderBy(a => a.CreatedAt).Select(a => new FinishedGoodsReconciliationAllocationDto {
                Id = a.Id, OrderCode = a.ProductionOrder.Code, Customer = a.ProductionOrder.Customer.Name,
                Packs = a.Products.SelectMany(p => p.FulfilledQuantities).Where(q => q.FinishedGoodsTransferNoteId == noteId).Sum(q => q.Quantity),
                DeliveredAt = a.DeliveredAt, DispatchedAt = a.DispatchedAt, LoadedAt = a.LoadedAt, Cancelled = a.CancelledAt.HasValue,
                DepartureEvidence = a.ExitReference,
            }).ToListAsync();

}
public sealed class FinishedGoodsDocumentsDto
{
    public List<FinishedGoodsInvoiceHistoryDto> InvoiceHistory { get; set; } = [];
    public DateTime? CostingConfirmedAt { get; set; }
    public List<FinishedGoodsCostBasisDto> CostingLines { get; set; } = [];
    public Guid? ProformaId { get; set; }
    public string ProformaCode { get; set; }
    public string CustomerPoNumber { get; set; }
    public DateTime? CustomerPoVerifiedAt { get; set; }
    public Guid? InvoiceId { get; set; }
    public string InvoiceCode { get; set; }
    public Guid? WaybillId { get; set; }
    public string WaybillCode { get; set; }
    public string Vehicle { get; set; }
    public string Driver { get; set; }
    public string DriverPhone { get; set; }
    public string Transporter { get; set; }
    public string DeliveryAddress { get; set; }
    public string StorageConditions { get; set; }
    public string Seal { get; set; }
}

public sealed class FinishedGoodsBatchOptionDto { public Guid Id { get; set; } public string BatchNumber { get; set; } public string ProductName { get; set; } }
public sealed class FinishedGoodsReconciliationAllocationDto
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; }
    public string Customer { get; set; }
    public decimal Packs { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? LoadedAt { get; set; }
    public bool Cancelled { get; set; }
    public string DepartureEvidence { get; set; }
}

public sealed class FinishedGoodsCostBasisDto
{
    public Guid ProductId { get; set; }
    public Guid ProductPackingId { get; set; }
    public decimal ManufacturingCostPerPack { get; set; }
    public Guid CurrencyId { get; set; }
    public string CurrencyLabel { get; set; }
    public string ApprovedCostBasisReference { get; set; }
}

public sealed class FinishedGoodsInvoiceHistoryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public bool Voided { get; set; }
    public DateTime? VoidedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string VoidReason { get; set; }
}
