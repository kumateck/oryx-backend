using DOMAIN.Entities.Products;

namespace APP.Utils;

/// <summary>
/// Finished goods are stored as yield: the exact base units produced (tablets, bottles, tubes).
/// Customer orders, packing lists and invoices count packs, because prices are per pack. This is
/// the one place the two are converted, and the desktop app applies the same rule
/// (oryx-live-desktop: shared/packing/packs.ts, packingLayout).
///
/// Units per pack is read from the packing style's unit rows:
/// <list type="bullet">
/// <item>a row naming the base unit itself ("Tube = 25" on a Tube style) gives its fill, not a count, and is ignored;</item>
/// <item>a row in a mass or volume unit (mg, g, ml…) describes contents, not a count, and is ignored;</item>
/// <item>the shipper row comes first (rows carry no reliable order), the rest keep their stored order;</item>
/// <item>the pack is the first level whose running product from the shipper equals PackPerShipper,
/// and units per pack is the product of the counted rows below it (1 when the pack is the base unit);</item>
/// <item>a style with no counted rows counts one base unit per pack;</item>
/// <item>a style whose rows never reach PackPerShipper cannot be converted.</item>
/// </list>
/// Callers must load <c>PackingLists</c> with each row's <c>Uom</c>.
/// </summary>
public static class PackConversion
{
    private static readonly HashSet<string> MeasureSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        "mg", "g", "kg", "mcg", "µg", "ug", "ml", "l", "cl", "dl",
    };

    /// <summary>Base units in one pack, or null when the packing style cannot say.</summary>
    public static decimal? UnitsPerPack(ProductPacking packing)
    {
        if (packing is null)
            return null;

        var counted = (packing.PackingLists ?? [])
            .Where(row => row.UomId != packing.BasePackingUomId)
            .Where(row => !MeasureSymbols.Contains(row.Uom?.Symbol?.Trim() ?? ""))
            .OrderBy(row => IsShipper(row.Uom?.Name) ? 0 : 1)
            .ThenBy(row => row.Order)
            .Select(row => row.Quantity)
            .ToList();

        if (counted.Count == 0)
            return 1m;
        if (packing.PackPerShipper <= 0 || counted.Any(quantity => quantity <= 0))
            return null;

        var downToHere = 1m;
        for (var level = 1; level <= counted.Count; level++)
        {
            downToHere *= counted[level - 1];
            if (downToHere != packing.PackPerShipper)
                continue;

            var perPack = 1m;
            foreach (var quantity in counted.Skip(level))
                perPack *= quantity;
            return perPack;
        }

        return null;
    }

    /// <summary>The yield in a number of packs.</summary>
    public static decimal ToYield(decimal packs, decimal unitsPerPack) => packs * unitsPerPack;

    /// <summary>Whether a yield is a whole number of packs.</summary>
    public static bool IsWholePacks(decimal yield, decimal unitsPerPack) =>
        unitsPerPack > 0 && yield >= 0 && yield % unitsPerPack == 0;

    /// <summary>Why a packing style cannot convert, for an error message the store can act on.</summary>
    public static string CannotConvert(ProductPacking packing) =>
        $"The packing style \"{packing?.Name ?? "unknown"}\" does not say how many units fill a pack: its unit rows do not "
        + $"add up to {packing?.PackPerShipper ?? 0} packs per shipper. Correct the packing style on the product first.";

    private static bool IsShipper(string name) =>
        name?.Trim().StartsWith("shipper", StringComparison.OrdinalIgnoreCase) == true;
}

/// <summary>Units per pack for stock that is already in the store.</summary>
public static class FinishedGoodsUnits
{
    /// <summary>
    /// The units in one pack of a transfer note: the figure saved on the note when it was raised, so a later
    /// edit to the packing style cannot change what the stock means. Notes raised before that figure was
    /// saved fall back to the packing style (load <c>ProductPacking.PackingLists.Uom</c>).
    /// </summary>
    public static decimal? UnitsPerPack(DOMAIN.Entities.Materials.Batch.FinishedGoodsTransferNote note) =>
        note is null ? null : note.QuantityPerPack > 0 ? note.QuantityPerPack : PackConversion.UnitsPerPack(note.ProductPacking);
}
