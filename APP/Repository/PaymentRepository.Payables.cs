using DOMAIN.Entities.Payments;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class PaymentRepository
{
    private async Task<Result<List<PaymentBalanceQuery.Total>>> GetPayableTotals(
        PayableType type,
        Guid id
    )
    {
        return type switch
        {
            PayableType.BillingSheet => await GetBillingSheetTotals(id),
            PayableType.ShipmentInvoice => await GetShipmentInvoiceTotals(id),
            PayableType.PurchaseOrderInvoice => await GetPurchaseOrderInvoiceTotals(id),
            PayableType.CustomerInvoice => await GetCustomerInvoiceTotals(id),
            _ => Error.Validation("Payment.PayableType", "Unsupported payable type."),
        };
    }

    private async Task<Result<List<PaymentBalanceQuery.Total>>> GetBillingSheetTotals(Guid id)
    {
        var entity = await context.BillingSheets.AsNoTracking()
            .Include(item => item.Charges).ThenInclude(item => item.Currency)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (entity is null)
            return Error.NotFound("BillingSheet.NotFound", "Billing sheet not found.");
        if (entity.Charges.Any(item => !item.CurrencyId.HasValue))
            return Error.Validation(
                "BillingSheet.Currency",
                "Every billing-sheet charge needs a currency before payments can be recorded."
            );
        return entity.Charges.Select(item => new PaymentBalanceQuery.Total(
            item.CurrencyId!.Value, item.Currency.Name, item.Currency.Symbol, item.Amount
        )).ToList();
    }

    private async Task<Result<List<PaymentBalanceQuery.Total>>> GetShipmentInvoiceTotals(Guid id)
    {
        var entity = await context.ShipmentInvoices.AsNoTracking()
            .Include(item => item.Currency)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (entity is null)
            return Error.NotFound("ShipmentInvoice.NotFound", "Shipment invoice not found.");
        if (!entity.CurrencyId.HasValue)
            return Error.Validation(
                "ShipmentInvoice.Currency",
                "Shipment invoice needs a currency before payments can be recorded."
            );
        return new List<PaymentBalanceQuery.Total>
        {
            new(entity.CurrencyId.Value, entity.Currency.Name, entity.Currency.Symbol, entity.TotalCost),
        };
    }

    private async Task<Result<List<PaymentBalanceQuery.Total>>> GetPurchaseOrderInvoiceTotals(Guid id)
    {
        var entity = await context.PurchaseOrderInvoices.AsNoTracking()
            .Include(item => item.Charges).ThenInclude(item => item.Currency)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (entity is null)
            return Error.NotFound("PurchaseOrderInvoice.NotFound", "Purchase-order invoice not found.");
        if (entity.Charges.Any(item => !item.CurrencyId.HasValue))
            return Error.Validation(
                "PurchaseOrderInvoice.Currency",
                "Every purchase-order invoice charge needs a currency before payments can be recorded."
            );
        return entity.Charges.Select(item => new PaymentBalanceQuery.Total(
            item.CurrencyId!.Value, item.Currency.Name, item.Currency.Symbol, item.Amount
        )).ToList();
    }

    private async Task<Result<List<PaymentBalanceQuery.Total>>> GetCustomerInvoiceTotals(Guid id)
    {
        var entity = await context.Invoices.AsNoTracking()
            .Include(item => item.Amounts).ThenInclude(item => item.Currency)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (entity is null)
            return Error.NotFound("Invoice.NotFound", "Customer invoice not found.");
        if (entity.Amounts.Count == 0)
            return Error.Validation(
                "Invoice.Amount",
                "Customer invoice has no frozen financial amounts."
            );
        return entity.Amounts.Select(item => new PaymentBalanceQuery.Total(
            item.CurrencyId, item.Currency.Name, item.Currency.Symbol, item.Amount
        )).ToList();
    }
}
