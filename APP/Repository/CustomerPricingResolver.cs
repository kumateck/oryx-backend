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
/// </summary>
internal static class CustomerPricingResolver
{
    internal static async Task<CustomerPricingResolution> ResolveAsync(
        ApplicationDbContext context,
        Guid customerId,
        Guid productId,
        Guid? productPackingId,
        decimal defaultPrice,
        DateTime asOf
    )
    {
        if (!productPackingId.HasValue)
            return Resolve([], defaultPrice);

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

        return Resolve(matches, defaultPrice);
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
        decimal defaultPrice
    ) =>
        activeMatches.Count switch
        {
            > 1 => new CustomerPricingResolution(defaultPrice, null, false, true),
            1 => new CustomerPricingResolution(
                activeMatches.First().AgreedPrice,
                activeMatches.First().CurrencyId,
                true,
                false
            ),
            _ => new CustomerPricingResolution(defaultPrice, null, false, false),
        };
}

internal sealed record CustomerPricingResolution(
    decimal UnitPrice,
    Guid? CurrencyId,
    bool FromAgreement,
    bool Ambiguous
);
