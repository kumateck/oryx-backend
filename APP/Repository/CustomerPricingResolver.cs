using DOMAIN.Entities.Customers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Repository;

/// <summary>
/// Single source of truth for "what price applies to this customer/product/packing":
/// the customer's active <see cref="CustomerPricingAgreement"/> when exactly one exists
/// for the given date, else the caller-supplied default (normally the product's list
/// price). Shared by quotations, production orders, and production schedule reporting
/// so they can never disagree on the resolved price.
/// Every resolution carries the currency of the price it chose: the agreement's currency, or
/// the list price's currency. Prices are never converted; callers that need one currency must
/// reject a mismatch instead.
/// </summary>
internal static class CustomerPricingResolver
{
    internal static async Task<CustomerPricingResolution> ResolveAsync(
        ApplicationDbContext context,
        Guid customerId,
        Guid productId,
        Guid? productPackingId,
        decimal defaultPrice,
        Guid? defaultCurrencyId,
        DateTime asOf
    )
    {
        if (!productPackingId.HasValue)
            return Resolve([], defaultPrice, defaultCurrencyId);

        var matches = await context
            .CustomerPricingAgreements.AsNoTracking()
            .Where(item =>
                item.CustomerId == customerId
                && item.ProductId == productId
                && item.ProductPackingId == productPackingId.Value
                && item.EffectiveFrom <= asOf
                && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= asOf)
            )
            .Take(2)
            .ToListAsync();

        return Resolve(matches, defaultPrice, defaultCurrencyId);
    }

    /// <summary>
    /// Same precedence as <see cref="ResolveAsync"/>, applied to an already-fetched batch of
    /// agreements -- for callers (e.g. reports) that need to price many rows without running
    /// one query per row.
    /// </summary>
    internal static IReadOnlyCollection<CustomerPricingAgreement> ActiveMatches(
        IEnumerable<CustomerPricingAgreement> agreements,
        Guid customerId,
        Guid productId,
        Guid productPackingId,
        DateTime asOf
    ) =>
        agreements
            .Where(item =>
                item.CustomerId == customerId
                && item.ProductId == productId
                && item.ProductPackingId == productPackingId
                && item.EffectiveFrom <= asOf
                && (!item.EffectiveTo.HasValue || item.EffectiveTo.Value >= asOf)
            )
            .ToList();

    internal static CustomerPricingResolution Resolve(
        IReadOnlyCollection<CustomerPricingAgreement> activeMatches,
        decimal defaultPrice,
        Guid? defaultCurrencyId
    ) =>
        activeMatches.Count switch
        {
            > 1 => new CustomerPricingResolution(defaultPrice, defaultCurrencyId, false, true),
            1 => new CustomerPricingResolution(
                activeMatches.First().AgreedPrice,
                activeMatches.First().CurrencyId,
                true,
                false
            ),
            _ => new CustomerPricingResolution(defaultPrice, defaultCurrencyId, false, false),
        };

    /// <summary>
    /// The currency of a product's current list price. Rows saved before list prices recorded a
    /// currency are in the base currency (the migration that added the column backfilled them so).
    /// </summary>
    internal static Guid? ListPriceCurrency(DOMAIN.Entities.Products.Product product, Guid? baseCurrencyId)
        => product?.PriceCurrencyId ?? baseCurrencyId;

    /// <summary>Every currency by id, for labelling resolved prices in reports.</summary>
    internal static Task<Dictionary<Guid, DOMAIN.Entities.Currencies.CurrencyDto>> CurrencyLabelsAsync(ApplicationDbContext context)
        => context.Currencies.AsNoTracking().ToDictionaryAsync(currency => currency.Id, currency =>
            new DOMAIN.Entities.Currencies.CurrencyDto
            {
                Id = currency.Id, Name = currency.Name, Symbol = currency.Symbol,
                Description = currency.Description, IsBaseCurrency = currency.IsBaseCurrency,
            });

    internal static DOMAIN.Entities.Currencies.CurrencyDto Label(
        IReadOnlyDictionary<Guid, DOMAIN.Entities.Currencies.CurrencyDto> labels, Guid? currencyId)
        => currencyId.HasValue && labels.TryGetValue(currencyId.Value, out var label) ? label : null;

    internal static Task<Guid?> BaseCurrencyIdAsync(ApplicationDbContext context)
        => context.Currencies.AsNoTracking().Where(currency => currency.IsBaseCurrency)
            .OrderBy(currency => currency.CreatedAt).Select(currency => (Guid?)currency.Id).FirstOrDefaultAsync();
}

internal sealed record CustomerPricingResolution(
    decimal UnitPrice,
    Guid? CurrencyId,
    bool FromAgreement,
    bool Ambiguous
);
