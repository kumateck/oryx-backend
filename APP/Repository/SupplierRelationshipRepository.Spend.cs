using DOMAIN.Entities.Payments;
using DOMAIN.Entities.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class SupplierRelationshipRepository
{
    public async Task<Result<SupplierSpendSummaryDto>> GetSpendSummary(
        Guid supplierId, DateTime from, DateTime to)
    {
        var supplier = await RequireSupplier(supplierId);
        if (!supplier.IsSuccess) return supplier.Error;
        var period = ValidatePeriod(from, to);
        if (!period.IsSuccess) return period.Error;
        var start = from.Date;
        var endExclusive = to.Date.AddDays(1);

        var billingIds = await context.BillingSheets.AsNoTracking()
            .Where(item => item.SupplierId == supplierId).Select(item => item.Id).ToListAsync();
        var shipmentIds = await context.ShipmentInvoices.AsNoTracking()
            .Where(item => item.SupplierId == supplierId).Select(item => item.Id).ToListAsync();
        var purchaseInvoiceIds = await context.PurchaseOrderInvoices.AsNoTracking()
            .Where(item => item.PurchaseOrder.SupplierId == supplierId)
            .Select(item => item.Id).ToListAsync();

        var payments = await context.Payments.AsNoTracking().Include(item => item.Currency)
            .Where(item => item.Approved && item.PaymentDate >= start && item.PaymentDate < endExclusive
                && (item.PayableType == PayableType.BillingSheet && billingIds.Contains(item.PayableId)
                    || item.PayableType == PayableType.ShipmentInvoice && shipmentIds.Contains(item.PayableId)
                    || item.PayableType == PayableType.PurchaseOrderInvoice
                        && purchaseInvoiceIds.Contains(item.PayableId)))
            .ToListAsync();
        var baseCurrency = await context.Currencies.AsNoTracking()
            .SingleOrDefaultAsync(item => item.IsBaseCurrency);
        var report = new SupplierSpendSummaryDto
        {
            SupplierId = supplierId, From = start, To = to.Date,
            BaseCurrencyId = baseCurrency?.Id, BaseCurrencyName = baseCurrency?.Name,
        };
        if (baseCurrency is null)
            report.DataQualityWarnings.Add("No base currency is configured; base-currency totals are unavailable.");

        var currencyIds = payments.Select(item => item.CurrencyId).Distinct().ToList();
        var rates = await context.ExchangeRates.AsNoTracking()
            .Where(item => currencyIds.Contains(item.CurrencyId) && item.EffectiveDate < endExclusive)
            .OrderByDescending(item => item.EffectiveDate).ToListAsync();
        var lines = new List<SpendLine>();
        foreach (var payment in payments)
        {
            var rate = payment.CurrencyId == baseCurrency?.Id ? 1m : rates
                .FirstOrDefault(item => item.CurrencyId == payment.CurrencyId
                    && item.EffectiveDate <= payment.PaymentDate)?.RateToBase;
            if (!rate.HasValue)
                report.DataQualityWarnings.Add(
                    $"Payment {payment.Reference} has no {payment.Currency.Name} rate as of {payment.PaymentDate:yyyy-MM-dd}.");
            lines.Add(new SpendLine(payment.PaymentDate, payment.CurrencyId,
                payment.Currency.Name, payment.Amount, rate.HasValue ? payment.Amount * rate : null));
        }

        report.Months =
        [
            .. lines.GroupBy(item => new DateTime(item.Date.Year, item.Date.Month, 1))
                .OrderBy(group => group.Key).Select(group => new SupplierMonthlySpendDto
                {
                    Month = group.Key, TotalBase = group.Sum(item => item.BaseAmount ?? 0m),
                    OriginalCurrencies =
                    [
                        .. group.GroupBy(item => new { item.CurrencyId, item.CurrencyName })
                            .Select(currency => new SupplierCurrencySpendDto
                            {
                                CurrencyId = currency.Key.CurrencyId,
                                CurrencyName = currency.Key.CurrencyName,
                                Amount = currency.Sum(item => item.Amount),
                            })
                    ],
                })
        ];
        report.TotalBase = report.Months.Sum(item => item.TotalBase);
        report.DataQualityWarnings = report.DataQualityWarnings.Distinct().ToList();
        return report;
    }

    private sealed record SpendLine(
        DateTime Date, Guid CurrencyId, string CurrencyName, decimal Amount, decimal? BaseAmount);
}
