using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Products;
using SHARED;

namespace APP.Repository;

public partial class CustomerRepository
{
    /// <summary>
    /// The currency a new quotation is raised in: the customer's preferred currency when set,
    /// else the one currency every line's price is in (its agreement's currency, or its list
    /// price's currency). Prices are never converted, so lines priced in different currencies
    /// are rejected rather than mixed.
    /// </summary>
    private async Task<Result<Guid>> ResolveQuotationCurrency(
        Customer customer,
        IReadOnlyCollection<CreateCustomerQuotationItemRequest> items,
        IReadOnlyDictionary<Guid, Product> products,
        Guid? baseCurrencyId,
        DateTime asOf)
    {
        if (customer.CurrencyId.HasValue) return customer.CurrencyId.Value;

        var currencies = new HashSet<Guid>();
        foreach (var line in items)
        {
            var product = products[line.ProductId];
            var resolution = await CustomerPricingResolver.ResolveAsync(context, customer.Id, product.Id,
                line.ProductPackingId, product.Price, CustomerPricingResolver.ListPriceCurrency(product, baseCurrencyId), asOf);
            if (resolution.Ambiguous)
                return Error.Conflict(
                    "CustomerPricing.Ambiguous",
                    "Multiple active pricing agreements match a quotation item.");
            if (!resolution.CurrencyId.HasValue)
                return Error.Validation(
                    "CustomerQuotation.CurrencyRequired",
                    $"The list price of {product.Name} has no currency. Set it in Finance > Costing or set the customer's preferred currency.");
            currencies.Add(resolution.CurrencyId.Value);
        }

        return currencies.Count == 1
            ? currencies.Single()
            : Error.Conflict(
                "CustomerPricing.Currency",
                "The prices for this quotation are in more than one currency. Set the customer's preferred currency and price every item in it.");
    }
}
