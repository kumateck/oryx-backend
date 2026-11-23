using APP.Utils;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.FinishedGoods;

public sealed partial class FinishedGoodsWorkflow
{
    public async Task<Result<FinishedGoodsWaybillPrintDto>> WaybillPrint(Guid id)
    {
        var documents = await Documents(id);
        if (documents.IsFailure) return documents.Error;
        if (!documents.Value.WaybillId.HasValue) return Error.NotFound("FinishedGoods.Waybill", "Waybill not found.");
        var customer = await db.AllocateProductionOrders.IgnoreAutoIncludes().Where(a => a.Id == id)
            .Select(a => new { Name = a.ProductionOrder.Customer.Name, a.ProductionOrder.Customer.Phone, a.ProductionOrder.Code, a.DispatchedAt, a.ExitReference }).SingleAsync();
        var lines = await db.AllocateProductionOrders.IgnoreAutoIncludes().AsNoTracking().Where(a => a.Id == id)
            .SelectMany(a => a.Products).SelectMany(p => p.FulfilledQuantities, (p, q) => new FinishedGoodsWaybillLineDto {
                Product = p.Product.Name, PackingStyle = q.FinishedGoodsTransferNote.PackingStyleSnapshot ?? p.ProductPacking.Name,
                Batch = q.FinishedGoodsTransferNote.BatchManufacturingRecord.BatchNumber, Expiry = q.FinishedGoodsTransferNote.BatchManufacturingRecord.ExpiryDate,
                Packs = q.Quantity, BaseUnits = q.YieldQuantity,
            }).ToListAsync();
        documents.Value.CostingLines = []; documents.Value.CostingConfirmedAt = null; documents.Value.InvoiceHistory = [];
        return new FinishedGoodsWaybillPrintDto { Documents = documents.Value, Customer = customer.Name, CustomerPhone = customer.Phone,
            OrderCode = customer.Code, DepartedAt = customer.DispatchedAt, ExitReference = customer.ExitReference, Lines = lines };
    }
    public async Task<List<FinishedGoodsSettlementOptionDto>> SettlementOptions(Guid id)
    {
        var batch = await db.FinishedGoodsRecalls.Where(r => r.Id == id).Select(r => (Guid?)r.BatchManufacturingRecordId).SingleOrDefaultAsync();
        if (!batch.HasValue) return [];
        return await db.Invoices.IgnoreAutoIncludes().AsNoTracking().Where(i => !i.Voided && i.ProformaInvoice.AllocateProductionOrder.Products
            .Any(p => p.FulfilledQuantities.Any(q => q.FinishedGoodsTransferNote.BatchManufacturingRecordId == batch)))
            .OrderBy(i => i.Code).Select(i => new FinishedGoodsSettlementOptionDto { InvoiceId = i.Id, Code = i.Code, Customer = i.Customer.Name,
                Currencies = i.Amounts.Select(a => new FinishedGoodsSettlementCurrencyDto { Id = a.CurrencyId, Label = a.Currency.Symbol, InvoicedAmount = a.Amount }).ToList() }).ToListAsync();
    }
    public async Task<List<FinishedGoodsBatchOptionDto>> ReplacementOrders(Guid id, Guid invoiceId, string search)
    {
        var recall = await db.FinishedGoodsRecalls.Where(r => r.Id == id).Select(r => new { r.BatchManufacturingRecordId, ProductId = r.BatchManufacturingRecord.ProductionScheduleProduct.ProductId }).SingleOrDefaultAsync();
        if (recall is null) return [];
        var customer = await db.Invoices.Where(i => i.Id == invoiceId && !i.Voided && i.ProformaInvoice.AllocateProductionOrder.Products.Any(p => p.FulfilledQuantities.Any(q => q.FinishedGoodsTransferNote.BatchManufacturingRecordId == recall.BatchManufacturingRecordId)))
            .Select(i => (Guid?)i.CustomerId).SingleOrDefaultAsync();
        if (!customer.HasValue) return [];
        var query = db.ProductionOrders.IgnoreAutoIncludes().Where(o => o.CustomerId == customer && o.Approved && o.Products.Any(p => p.ProductId == recall.ProductId));
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(o => o.Code.ToLower().Contains(search.ToLower()));
        return await query.OrderByDescending(o => o.CreatedAt).ThenBy(o => o.Id).Select(o => new FinishedGoodsBatchOptionDto { Id = o.Id, BatchNumber = o.Code, ProductName = o.Customer.Name }).Take(50).ToListAsync();
    }
}
public sealed class FinishedGoodsWaybillPrintDto
{
    public FinishedGoodsDocumentsDto Documents { get; set; }
    public string Customer { get; set; }
    public string CustomerPhone { get; set; }
    public string OrderCode { get; set; }
    public DateTime? DepartedAt { get; set; }
    public string ExitReference { get; set; }
    public List<FinishedGoodsWaybillLineDto> Lines { get; set; } = [];
}
public sealed class FinishedGoodsWaybillLineDto
{
    public string Product { get; set; }
    public string PackingStyle { get; set; }
    public string Batch { get; set; }
    public DateTime? Expiry { get; set; }
    public decimal Packs { get; set; }
    public decimal BaseUnits { get; set; }
}
public sealed class FinishedGoodsSettlementOptionDto
{
    public Guid InvoiceId { get; set; }
    public string Code { get; set; }
    public string Customer { get; set; }
    public List<FinishedGoodsSettlementCurrencyDto> Currencies { get; set; } = [];
}
public sealed class FinishedGoodsSettlementCurrencyDto { public Guid Id { get; set; } public string Label { get; set; } public decimal InvoicedAmount { get; set; } }
