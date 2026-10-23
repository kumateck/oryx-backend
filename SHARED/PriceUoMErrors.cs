namespace SHARED;

/// <summary>
/// The refusal every write path returns when a price arrives without a unit the line can be
/// costed in (see <see cref="UomConverter.CanPrice"/>). One code, so clients can recognise it.
/// </summary>
public static class PriceUoMErrors
{
    public const string Code = "Price.PriceUoM";

    public static Error Unusable(string materialName, string quantityUoM, string priceUoM)
    {
        var material = string.IsNullOrWhiteSpace(materialName) ? "A line" : materialName;
        var unit = string.IsNullOrWhiteSpace(quantityUoM) ? "its own unit" : quantityUoM;
        return string.IsNullOrWhiteSpace(priceUoM)
            ? Error.Validation(Code, $"{material} has a price but no price unit. Give the unit the price is quoted per, for example kg.")
            : Error.Validation(Code, $"{material} is counted in {unit}, so it cannot be priced per '{priceUoM}'. Give a price unit that converts to {unit}.");
    }
}
