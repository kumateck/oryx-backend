using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Costing;

/// <summary>Activation follows approval and the effective date without a scheduler or master-data rewrite.</summary>
internal static class EffectiveCostingPrice
{
    internal sealed record Selection(bool HasRevision, Guid? RevisionId, decimal Price, Guid? CurrencyId);

    internal static async Task<Selection> Resolve(ApplicationDbContext context, Guid productId, Guid? packingId, DateTime asOf)
    {
        var division = await context.Products.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(product => product.Id == productId && product.DeletedAt == null)
            .Select(product => (DOMAIN.Entities.Products.Division?)product.Division).FirstOrDefaultAsync();
        if (!division.HasValue) return new(false, null, 0, null);
        var revision = await CostingPriceListService.EffectiveRevision(context, division.Value, asOf)
            .Select(list => (Guid?)list.Id).FirstOrDefaultAsync();
        if (!revision.HasValue) return new(false, null, 0, null);
        var line = packingId.HasValue ? await context.CostingPriceListLines.AsNoTracking()
            .Where(line => line.CostingPriceListId == revision && line.ProductId == productId && line.ProductPackingId == packingId)
            .Select(line => new { line.Price, line.CurrencyId }).FirstOrDefaultAsync() : null;
        // Missing or unpriced styles on an effective revision never borrow another style's or a legacy price.
        return new(true, revision, line?.Price ?? 0, line?.CurrencyId);
    }

    /// <summary>Loads only the requested products' snapshot prices once for a report; no per-row queries.</summary>
    internal static async Task<Func<Guid, Guid?, DateTime, Selection>> Load(ApplicationDbContext context, IEnumerable<Guid> productIds, DateTime through)
    {
        var ids = productIds.Distinct().ToList();
        var products = await context.Products.IgnoreQueryFilters().IgnoreAutoIncludes().AsNoTracking()
            .Where(product => ids.Contains(product.Id)).Select(product => new { product.Id, product.Division }).ToListAsync();
        var divisions = products.Select(product => product.Division).Distinct().ToList();
        var revisions = await context.CostingPriceLists.AsNoTracking().Where(list => divisions.Contains(list.Division)
            && list.Status == DOMAIN.Entities.Costing.CostingPriceListStatus.Approved && list.EffectiveFrom <= through && list.ApprovedAt <= through)
            .Select(list => new { list.Id, list.Division, list.EffectiveFrom, list.ApprovedAt, list.RevisionNumber }).ToListAsync();
        var revisionIds = revisions.Select(list => list.Id).ToList();
        var lines = await context.CostingPriceListLines.AsNoTracking().Where(line => revisionIds.Contains(line.CostingPriceListId) && ids.Contains(line.ProductId))
            .Select(line => new { line.CostingPriceListId, line.ProductId, line.ProductPackingId, line.Price, line.CurrencyId }).ToListAsync();
        var prices = lines.ToDictionary(line => (line.CostingPriceListId, line.ProductId, line.ProductPackingId));
        var scopes = products.ToDictionary(product => product.Id, product => product.Division);
        return (product, packing, asOf) => {
            if (!scopes.TryGetValue(product, out var division)) return new(false,null,0,null);
            var revision = revisions.Where(list => list.Division == division && list.EffectiveFrom <= asOf && list.ApprovedAt <= asOf)
                .OrderByDescending(list => list.EffectiveFrom).ThenByDescending(list => list.RevisionNumber).FirstOrDefault();
            if (revision is null) return new(false,null,0,null);
            var line = packing.HasValue ? prices.GetValueOrDefault((revision.Id,product,packing.Value)) : null;
            return new(true,revision.Id,line?.Price ?? 0,line?.CurrencyId);
        };
    }
}
