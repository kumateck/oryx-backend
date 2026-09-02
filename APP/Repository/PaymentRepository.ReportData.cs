using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Payments;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

public partial class PaymentRepository
{
    private async Task<(List<RawLine> Lines, List<string> Warnings)> GetApLines(DateTime asOf)
    {
        var lines = new List<RawLine>();
        var warnings = new List<string>();
        var paid = await LoadPaymentSums(PayableType.BillingSheet, PayableType.ShipmentInvoice, PayableType.PurchaseOrderInvoice);

        var sheets = await context.BillingSheets.AsNoTracking().Include(x => x.Supplier)
            .Include(x => x.Charges).ThenInclude(x => x.Currency).ToListAsync();
        foreach (var sheet in sheets)
        {
            if (!sheet.SupplierId.HasValue)
            {
                warnings.Add($"Billing sheet {sheet.Code} has no supplier and was excluded.");
                continue;
            }
            foreach (var charge in sheet.Charges)
            {
                if (!charge.CurrencyId.HasValue)
                {
                    warnings.Add($"Billing sheet {sheet.Code} has a charge without currency and that charge was excluded.");
                    continue;
                }
                AddRaw(lines, paid, PayableType.BillingSheet, sheet.Id, sheet.Code, sheet.DueDate,
                    sheet.SupplierId.Value, sheet.Supplier?.Name, charge.CurrencyId.Value, charge.Currency?.Name, charge.Amount);
            }
        }

        var shipments = await context.ShipmentInvoices.AsNoTracking().Include(x => x.Supplier)
            .Include(x => x.Currency).ToListAsync();
        foreach (var invoice in shipments)
        {
            if (!invoice.SupplierId.HasValue || !invoice.CurrencyId.HasValue)
            {
                warnings.Add($"Shipment invoice {invoice.Code} is missing supplier or currency and was excluded.");
                continue;
            }
            AddRaw(lines, paid, PayableType.ShipmentInvoice, invoice.Id, invoice.Code, invoice.DueDate,
                invoice.SupplierId.Value, invoice.Supplier?.Name, invoice.CurrencyId.Value, invoice.Currency?.Name, invoice.TotalCost);
        }

        var purchaseInvoices = await context.PurchaseOrderInvoices.AsNoTracking()
            .Include(x => x.PurchaseOrder).ThenInclude(x => x.Supplier)
            .Include(x => x.Charges).ThenInclude(x => x.Currency).ToListAsync();
        foreach (var invoice in purchaseInvoices)
        foreach (var charge in invoice.Charges)
        {
            if (!charge.CurrencyId.HasValue)
            {
                warnings.Add($"Purchase-order invoice {invoice.Code} has a charge without currency and that charge was excluded.");
                continue;
            }
            AddRaw(lines, paid, PayableType.PurchaseOrderInvoice, invoice.Id, invoice.Code, invoice.DueDate,
                invoice.PurchaseOrder.SupplierId, invoice.PurchaseOrder.Supplier?.Name,
                charge.CurrencyId.Value, charge.Currency?.Name, charge.Amount);
        }

        return (Collapse(lines), warnings);
    }

    private async Task<(List<RawLine> Lines, List<string> Warnings)> GetArLines(DateTime asOf)
    {
        var lines = new List<RawLine>();
        var warnings = new List<string>();
        var paid = await LoadPaymentSums(PayableType.CustomerInvoice);
        var invoices = await context.Invoices.AsNoTracking().Include(x => x.Customer)
            .Include(x => x.ProformaInvoice).Include(x => x.Amounts).ThenInclude(x => x.Currency).ToListAsync();
        foreach (var invoice in invoices)
        {
            if (invoice.Amounts.Count == 0)
            {
                warnings.Add($"Customer invoice {invoice.Id} has no frozen amount and was excluded.");
                continue;
            }
            foreach (var amount in invoice.Amounts)
                AddRaw(lines, paid, PayableType.CustomerInvoice, invoice.Id,
                    invoice.ProformaInvoice?.Code ?? invoice.Id.ToString(), invoice.DueDate,
                    invoice.CustomerId, invoice.Customer?.Name, amount.CurrencyId, amount.Currency?.Name, amount.Amount);
        }
        return (Collapse(lines), warnings);
    }

    private async Task<Dictionary<(PayableType Type, Guid Id, Guid CurrencyId), decimal>> LoadPaymentSums(
        params PayableType[] types)
        => await context.Payments.AsNoTracking()
            .Where(item => item.Approved && types.Contains(item.PayableType))
            .GroupBy(item => new { item.PayableType, item.PayableId, item.CurrencyId })
            .Select(group => new
            {
                group.Key.PayableType,
                group.Key.PayableId,
                group.Key.CurrencyId,
                Amount = group.Sum(x => x.Amount),
            })
            .ToDictionaryAsync(x => (x.PayableType, x.PayableId, x.CurrencyId), x => x.Amount);

    private static void AddRaw(
        List<RawLine> lines,
        Dictionary<(PayableType, Guid, Guid), decimal> paid,
        PayableType type,
        Guid id,
        string code,
        DateTime? dueDate,
        Guid partyId,
        string partyName,
        Guid currencyId,
        string currencyName,
        decimal total
    ) => lines.Add(new RawLine(type, id, code, dueDate, partyId, partyName ?? "Unknown",
        currencyId, currencyName ?? "Unknown", total, paid.GetValueOrDefault((type, id, currencyId))));

    private static List<RawLine> Collapse(IEnumerable<RawLine> lines)
        =>
        [
            .. lines.GroupBy(line => new
                {
                    line.PayableType,
                    line.PayableId,
                    line.DocumentCode,
                    line.DueDate,
                    line.PartyId,
                    line.PartyName,
                    line.CurrencyId,
                    line.CurrencyName,
                })
                .Select(group => new RawLine(group.Key.PayableType, group.Key.PayableId, group.Key.DocumentCode,
                    group.Key.DueDate, group.Key.PartyId, group.Key.PartyName, group.Key.CurrencyId,
                    group.Key.CurrencyName, group.Sum(x => x.DocumentTotal), group.Max(x => x.AmountPaid)))
        ];

    private async Task<Currency> GetBaseCurrency()
        => await context.Currencies.AsNoTracking().SingleOrDefaultAsync(item => item.IsBaseCurrency);

    private sealed record RawLine(
        PayableType PayableType,
        Guid PayableId,
        string DocumentCode,
        DateTime? DueDate,
        Guid PartyId,
        string PartyName,
        Guid CurrencyId,
        string CurrencyName,
        decimal DocumentTotal,
        decimal AmountPaid
    )
    {
        public decimal Outstanding => Math.Max(0m, DocumentTotal - AmountPaid);
    }
}
