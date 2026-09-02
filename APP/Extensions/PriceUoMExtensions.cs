using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Extensions;

/// <summary>
/// Resolves the unit a price was quoted in (<c>PriceUoM</c>) for read models.
///
/// A price without its UoM cannot be interpreted: a quantity in <c>g</c> priced per
/// <c>kg</c> is off by a factor of 1000. <c>PriceUoM</c> is therefore persisted next to
/// every price, and these helpers exist only to backfill rows written before the award
/// flow carried it through.
///
/// Two rules that are easy to get wrong, and were, in four hand-rolled copies of this
/// logic:
///
/// 1. A purchase order id does NOT identify the awarded supplier. When a quotation is
///    awarded, every LOSING supplier's quotation line for that material is stamped with
///    the winner's PurchaseOrderId so the reassign-supplier picker can find them. A
///    lookup keyed on the purchase order alone can return a competitor's PriceUoM next
///    to the winner's price, so the query below also scopes by the purchase order's own
///    SupplierId and by <see cref="SupplierQuotationItemStatus.Processed"/>.
///
/// 2. The persisted value always wins. It reflects the latest purchase-order revision,
///    whereas the quotation is a historical record. Overwriting the persisted value
///    with the quotation's - or worse, with a null - silently reverts revisions.
/// </summary>
public static class PriceUoMExtensions
{
    public readonly record struct PriceUoMKey(Guid PurchaseOrderId, Guid MaterialId, Guid UoMId);

    /// <summary>
    /// Builds the fallback lookup for a batch of purchase orders in a single query.
    /// </summary>
    public static async Task<IReadOnlyDictionary<PriceUoMKey, string>> BuildQuotationPriceUoMLookup(
        this ApplicationDbContext context,
        IReadOnlyCollection<Guid> purchaseOrderIds,
        CancellationToken cancellationToken = default
    )
    {
        if (purchaseOrderIds is null || purchaseOrderIds.Count == 0)
        {
            return new Dictionary<PriceUoMKey, string>();
        }

        var quotationItems = await context
            .SupplierQuotationItems.AsNoTracking()
            .Where(q =>
                q.PurchaseOrderId.HasValue
                && purchaseOrderIds.Contains(q.PurchaseOrderId.Value)
                && q.Status == SupplierQuotationItemStatus.Processed
                && q.PriceUoM != null
                && q.PriceUoM != ""
                // Awarded supplier only - see rule 1 above.
                && context.PurchaseOrders.Any(po =>
                    po.Id == q.PurchaseOrderId.Value
                    && po.SupplierId == q.SupplierQuotation.SupplierId
                )
            )
            .Select(q => new
            {
                PurchaseOrderId = q.PurchaseOrderId.Value,
                q.MaterialId,
                q.UoMId,
                q.PriceUoM,
            })
            .ToListAsync(cancellationToken);

        return quotationItems
            .DistinctBy(q => (q.PurchaseOrderId, q.MaterialId, q.UoMId))
            .ToDictionary(
                q => new PriceUoMKey(q.PurchaseOrderId, q.MaterialId, q.UoMId),
                q => q.PriceUoM
            );
    }

    /// <summary>
    /// Returns the price UoM to expose for a line: the persisted value when there is
    /// one, otherwise the awarded quotation's. Never returns blank over a real value.
    /// </summary>
    public static string ResolvePriceUoM(
        this IReadOnlyDictionary<PriceUoMKey, string> lookup,
        string persistedPriceUoM,
        Guid? purchaseOrderId,
        Guid? materialId,
        Guid? uomId
    )
    {
        if (!string.IsNullOrWhiteSpace(persistedPriceUoM))
        {
            return persistedPriceUoM;
        }

        if (lookup is null || !purchaseOrderId.HasValue || !materialId.HasValue || !uomId.HasValue)
        {
            return persistedPriceUoM;
        }

        var key = new PriceUoMKey(purchaseOrderId.Value, materialId.Value, uomId.Value);

        return lookup.TryGetValue(key, out var fallback) && !string.IsNullOrWhiteSpace(fallback)
            ? fallback
            : persistedPriceUoM;
    }
}
