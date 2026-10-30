namespace DOMAIN.Entities.Products;

/// <summary>
/// How a packing style counts. Units are listed outermost first: each row's quantity counts the unit on the
/// next row and the last row counts base packing units (tablets, bottles…). The shipper is the first unit and
/// the pack is the level whose count per shipper equals <see cref="ProductPacking.PackPerShipper"/>.
/// Example: Shipper 80, Outer Carton 20, Inner Carton 10 with 80 packs per shipper makes the outer carton
/// the pack, holding 20 × 10 = 200 tablets.
/// </summary>
public static class PackingLayout
{
    /// <summary>
    /// Base units in one pack. One when the style has no unit breakdown (a base unit is a pack);
    /// null when the units never add up to <paramref name="packsPerShipper"/>, so base units cannot be turned into packs.
    /// </summary>
    public static decimal? BaseUnitsPerPack(IReadOnlyList<decimal> quantitiesOutermostFirst, decimal packsPerShipper)
    {
        if (quantitiesOutermostFirst.Count == 0) return 1m;
        if (packsPerShipper <= 0 || quantitiesOutermostFirst.Any(quantity => quantity <= 0)) return null;
        var perShipper = 1m;
        for (var level = 1; level <= quantitiesOutermostFirst.Count; level++)
        {
            perShipper *= quantitiesOutermostFirst[level - 1];
            if (perShipper == packsPerShipper)
                return quantitiesOutermostFirst.Skip(level).Aggregate(1m, (product, quantity) => product * quantity);
        }
        return null;
    }

    public static decimal? BaseUnitsPerPack(IEnumerable<ProductPackingList> units, decimal packsPerShipper) =>
        BaseUnitsPerPack(units.OrderBy(unit => unit.Order).Select(unit => unit.Quantity).ToList(), packsPerShipper);

    /// <summary>Whole packs from a count of base units, and the base units left over that do not fill a pack.</summary>
    public static (decimal Packs, decimal Leftover) ToPacks(decimal baseUnits, decimal baseUnitsPerPack)
    {
        var packs = decimal.Floor(baseUnits / baseUnitsPerPack);
        return (packs, baseUnits - packs * baseUnitsPerPack);
    }

    /// <summary>Packs as full shippers and loose packs.</summary>
    public static (decimal Shippers, decimal Loose) ShipperSplit(decimal packs, decimal packsPerShipper)
    {
        var shippers = decimal.Floor(packs / packsPerShipper);
        return (shippers, packs - shippers * packsPerShipper);
    }
}
