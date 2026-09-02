using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Invoices;
using DOMAIN.Entities.Payments;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class CustomerRepository
{
    public async Task<Result<CustomerCreditStatusDto>> GetAvailableCredit(
        Guid customerId, decimal additionalOrderValue = 0)
    {
        if (additionalOrderValue < 0)
            return Error.Validation("CustomerCredit.AdditionalValue", "Additional order value cannot be negative.");
        var customer = await context.Customers.AsNoTracking().Include(item => item.Currency)
            .FirstOrDefaultAsync(item => item.Id == customerId);
        if (customer is null) return Error.NotFound("Customer.NotFound", "Customer not found.");
        if (customer.CreditLimit.HasValue && !customer.CurrencyId.HasValue)
            return Error.Validation("CustomerCredit.CurrencyRequired", "A preferred currency is required for credit control.");

        var invoiceAmounts = await context.InvoiceAmounts.AsNoTracking()
            .Where(item => item.Invoice.CustomerId == customerId && item.Invoice.Status == InvoiceStatus.Approved)
            .Select(item => new { item.InvoiceId, item.CurrencyId, item.Currency.Name, item.Currency.Symbol, item.Amount })
            .ToListAsync();
        var invoiceIds = invoiceAmounts.Select(item => item.InvoiceId).Distinct().ToList();
        var payments = await context.Payments.AsNoTracking().Where(item => item.Approved
                && item.PayableType == PayableType.CustomerInvoice && invoiceIds.Contains(item.PayableId))
            .GroupBy(item => new { InvoiceId = item.PayableId, item.CurrencyId })
            .Select(group => new { group.Key.InvoiceId, group.Key.CurrencyId, Amount = group.Sum(x => x.Amount) })
            .ToListAsync();

        var paid = payments.ToDictionary(item => (item.InvoiceId, item.CurrencyId), item => item.Amount);
        var invoiceCurrencyTotals = invoiceAmounts.GroupBy(item => new
            { item.InvoiceId, item.CurrencyId, item.Name, item.Symbol })
            .Select(group => new
            {
                group.Key.InvoiceId, group.Key.CurrencyId, group.Key.Name, group.Key.Symbol,
                Amount = group.Sum(item => item.Amount),
            });
        var balances = invoiceCurrencyTotals.Select(item => new
            {
                item.CurrencyId, item.Name, item.Symbol,
                Document = item.Amount,
                Paid = Math.Min(item.Amount, paid.GetValueOrDefault((item.InvoiceId, item.CurrencyId))),
            })
            .GroupBy(item => new { item.CurrencyId, item.Name, item.Symbol })
            .Select(group => new PayableBalanceDto
            {
                CurrencyId = group.Key.CurrencyId, CurrencyName = group.Key.Name,
                CurrencySymbol = group.Key.Symbol, DocumentTotal = group.Sum(item => item.Document),
                AmountPaid = group.Sum(item => item.Paid),
                OutstandingBalance = group.Sum(item => item.Document - item.Paid),
            }).ToList();

        decimal? outstandingPreferred = null;
        if (customer.CurrencyId.HasValue)
        {
            var preferredRate = await GetRateToBase(customer.CurrencyId.Value, DateTime.UtcNow);
            if (!preferredRate.IsSuccess) return preferredRate.Error;
            outstandingPreferred = 0m;
            foreach (var balance in balances)
            {
                var sourceRate = await GetRateToBase(balance.CurrencyId, DateTime.UtcNow);
                if (!sourceRate.IsSuccess) return sourceRate.Error;
                outstandingPreferred += balance.OutstandingBalance * sourceRate.Value / preferredRate.Value;
            }
        }

        decimal? available = customer.CreditLimit.HasValue
            ? customer.CreditLimit.Value - (outstandingPreferred ?? 0m) : null;
        return new CustomerCreditStatusDto
        {
            CustomerId = customer.Id, CreditLimit = customer.CreditLimit,
            PreferredCurrencyId = customer.CurrencyId, PreferredCurrencyName = customer.Currency?.Name,
            OutstandingInPreferredCurrency = outstandingPreferred, AvailableCredit = available,
            AdditionalOrderValue = additionalOrderValue,
            IsWithinCreditLimit = !available.HasValue || additionalOrderValue <= available.Value,
            OutstandingByCurrency = balances,
        };
    }

    public async Task<Result<bool>> IsWithinCreditLimit(Guid customerId, decimal additionalOrderValue)
    {
        var result = await GetAvailableCredit(customerId, additionalOrderValue);
        return result.IsSuccess ? result.Value.IsWithinCreditLimit : result.Error;
    }

    private async Task<Result<decimal>> GetRateToBase(Guid currencyId, DateTime asOf)
    {
        var currency = await context.Currencies.AsNoTracking().FirstOrDefaultAsync(item => item.Id == currencyId);
        if (currency is null) return Error.NotFound("Currency.NotFound", "Currency not found.");
        if (currency.IsBaseCurrency) return 1m;
        var rate = await context.ExchangeRates.AsNoTracking()
            .Where(item => item.CurrencyId == currencyId && item.EffectiveDate <= asOf)
            .OrderByDescending(item => item.EffectiveDate).Select(item => (decimal?)item.RateToBase)
            .FirstOrDefaultAsync();
        return rate.HasValue ? rate.Value : Error.Validation("ExchangeRate.Missing",
            $"No exchange rate exists for {currency.Name} as of {asOf:yyyy-MM-dd}.");
    }
}
