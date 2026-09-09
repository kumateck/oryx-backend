using DOMAIN.Entities.Customers;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class CustomerRepository
{
    private async Task<Result<Guid>> ResolveQuotationCurrency(
        Customer customer,
        IReadOnlyCollection<CreateCustomerQuotationItemRequest> items,
        DateTime asOf)
    {
        if (customer.CurrencyId.HasValue) return customer.CurrencyId.Value;

        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        var packingIds = items.Select(item => item.ProductPackingId).Distinct().ToList();
        var agreements = await context.CustomerPricingAgreements.AsNoTracking()
            .Where(item => item.CustomerId == customer.Id
                && productIds.Contains(item.ProductId)
                && packingIds.Contains(item.ProductPackingId)
                && item.EffectiveFrom <= asOf
                && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= asOf))
            .ToListAsync();

        var currencies = new HashSet<Guid>();
        foreach (var line in items)
        {
            var matches = agreements.Where(item => item.ProductId == line.ProductId
                && item.ProductPackingId == line.ProductPackingId).ToList();
            if (matches.Count > 1)
                return Error.Conflict(
                    "CustomerPricing.Ambiguous",
                    "Multiple active pricing agreements match a quotation item.");
            if (matches.Count == 0)
                return Error.Validation(
                    "CustomerQuotation.CurrencyRequired",
                    "Set the customer's preferred currency or create an active pricing agreement for every quotation item.");
            currencies.Add(matches[0].CurrencyId);
        }

        return currencies.Count == 1
            ? currencies.Single()
            : Error.Conflict(
                "CustomerPricing.Currency",
                "Active pricing agreements must use one currency for the quotation.");
    }
}
