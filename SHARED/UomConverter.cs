namespace SHARED;

/// <summary>
/// Converts a quantity between units of the same dimension.
///
/// A price is quoted per some unit (<c>PriceUoM</c>) while the line's quantity is stored
/// in another. Multiplying the two without reconciling the units is wrong by whatever
/// factor separates them: 5 mg at 0.1 per kg is 0.0000005, not 0.5.
///
/// Deliberately conservative. It returns <c>null</c> rather than guessing when either
/// symbol is unknown or the two belong to different dimensions - converting a length
/// into a mass needs density data the system does not hold. Callers decide what an
/// unconvertible pair means; they must not substitute a raw multiplication silently.
///
/// The factor table mirrors the client's (see `convertUnits` in oryx-next/src/lib/utils.ts).
/// Keep the two in step.
/// </summary>
public static class UomConverter
{
    private enum Dimension
    {
        Mass,
        Volume,
        Length,
        Area,
        SolidVolume,
    }

    // Symbol (lowercased) -> the dimension it belongs to and its size relative to that
    // dimension's smallest unit.
    private static readonly Dictionary<string, (Dimension Dimension, decimal Factor)> Units = new()
    {
        ["mg"] = (Dimension.Mass, 1m),
        ["g"] = (Dimension.Mass, 1_000m),
        ["kg"] = (Dimension.Mass, 1_000_000m),
        ["ml"] = (Dimension.Volume, 1m),
        ["l"] = (Dimension.Volume, 1_000m),
        ["m"] = (Dimension.Length, 1m),
        ["km"] = (Dimension.Length, 1_000m),
        ["m²"] = (Dimension.Area, 1m),
        ["m³"] = (Dimension.SolidVolume, 1m),
    };

    /// <summary>
    /// Expresses <paramref name="quantity"/> (given in <paramref name="fromSymbol"/>) in
    /// <paramref name="toSymbol"/>. Null when the units cannot be reconciled.
    /// </summary>
    public static decimal? Convert(decimal quantity, string fromSymbol, string toSymbol)
    {
        if (string.IsNullOrWhiteSpace(fromSymbol) || string.IsNullOrWhiteSpace(toSymbol))
            return null;

        var from = fromSymbol.Trim();
        var to = toSymbol.Trim();

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return quantity;

        if (
            !Units.TryGetValue(from.ToLowerInvariant(), out var source)
            || !Units.TryGetValue(to.ToLowerInvariant(), out var target)
            || source.Dimension != target.Dimension
        )
        {
            return null;
        }

        return quantity * source.Factor / target.Factor;
    }

    /// <summary>
    /// What <paramref name="quantity"/> of a material is worth at <paramref name="price"/>
    /// per <paramref name="priceUoM"/>. Null when the units cannot be reconciled.
    /// </summary>
    public static decimal? LineValue(
        decimal price,
        decimal quantity,
        string quantityUoM,
        string priceUoM
    )
    {
        var quantityInPriceUoM = Convert(quantity, quantityUoM, priceUoM);

        return quantityInPriceUoM.HasValue ? price * quantityInPriceUoM.Value : null;
    }

    /// <summary>
    /// True when the quantity amounts to less than 1% of a single priced unit - the
    /// signature of a price quoted in the wrong unit, or a quantity entered in the wrong
    /// one. 5 mg against a price per kg is 0.000005 of a kilogram.
    /// </summary>
    public static bool IsNegligibleAgainstPriceUoM(
        decimal quantity,
        string quantityUoM,
        string priceUoM
    )
    {
        var quantityInPriceUoM = Convert(quantity, quantityUoM, priceUoM);

        return quantityInPriceUoM is > 0 and < 0.01m;
    }
}
