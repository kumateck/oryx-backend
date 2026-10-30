using System.Globalization;
using SHARED;

namespace APP.Services.Documents;

/// <summary>
/// Numbers, money, quantities and dates written the way the desktop app prints them
/// (shared/format/dates.ts, shared/procurement/price.ts, shared/units/quantity.ts), so the same
/// order reads the same on paper and in an email.
/// </summary>
public static class DocumentText
{
    private static readonly CultureInfo Numbers = CultureInfo.GetCultureInfo("en-US");

    /// <summary>A number the system worked out is shown with at most this many decimals.</summary>
    private const int CalculatedDecimals = 4;

    // Quantities are stored in the smallest unit of their family and shown in the largest unit
    // that keeps the number at 1 or more.
    private static readonly (string Symbol, decimal Factor)[] Mass = [("mg", 1m), ("g", 1_000m), ("kg", 1_000_000m)];
    private static readonly (string Symbol, decimal Factor)[] Volume = [("ml", 1m), ("L", 1_000m)];
    private static readonly (string Symbol, decimal Factor)[][] Families = [Mass, Volume];

    public const string Missing = "–";

    /// <summary>"2 Oct 2026", or null when there is no date.</summary>
    public static string Date(DateTime? value) =>
        value?.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>A number as it was entered: every digit kept.</summary>
    public static string Entered(decimal value) => value.ToString("#,##0.############", Numbers);

    /// <summary>A number the system worked out: at most four decimals, but a real amount is never shown as zero.</summary>
    public static string Calculated(decimal value)
    {
        const decimal smallest = 0.0001m;
        if (value != 0 && Math.Abs(value) < smallest)
            return value < 0 ? $"> -{smallest.ToString(Numbers)}" : $"< {smallest.ToString(Numbers)}";
        return Math.Round(value, CalculatedDecimals).ToString("#,##0.####", Numbers);
    }

    /// <summary>A stored quantity in the largest unit of its family that keeps it at 1 or more ("650 kg", not "650,000,000 mg").</summary>
    public static string Quantity(decimal value, string symbol, bool calculated = true)
    {
        var unit = symbol?.Trim() ?? "";
        string Show(decimal amount, string shownUnit)
        {
            var number = calculated ? Calculated(amount) : Entered(amount);
            return shownUnit.Length > 0 ? $"{number} {shownUnit}" : number;
        }

        var family = FamilyOf(unit);
        if (family is null)
            return Show(value, unit);

        var inSmallest = value * family.Single(step => step.Symbol.Equals(unit, StringComparison.OrdinalIgnoreCase)).Factor;
        var best = family.Reverse().FirstOrDefault(step => Math.Abs(inSmallest) >= step.Factor);
        if (best.Symbol is null)
            best = family[0];
        return Show(inSmallest / best.Factor, best.Symbol);
    }

    /// <summary>Money to two decimals, except amounts too small for that, which are shown in full rather than as 0.00.</summary>
    public static string Money(decimal? amount, string currency)
    {
        if (amount is null)
            return Missing;
        var value = amount.Value;
        var number = value != 0 && Math.Abs(value) < 0.01m
            ? value.ToString("0.##########", Numbers)
            : value.ToString("#,##0.00", Numbers);
        return string.IsNullOrWhiteSpace(currency) ? number : $"{currency.Trim()} {number}";
    }

    /// <summary>A quoted unit price exactly as the supplier gave it, with the unit it is per: "GH₵ 12.5 / kg".</summary>
    public static string UnitPrice(decimal price, string currency, string priceUnit)
    {
        var text = string.IsNullOrWhiteSpace(currency) ? Entered(price) : $"{currency.Trim()} {Entered(price)}";
        return string.IsNullOrWhiteSpace(priceUnit) ? text : $"{text} / {priceUnit.Trim()}";
    }

    /// <summary>
    /// What a line costs: the quantity converted to the unit the price is quoted per, times the price.
    /// A counted unit (pcs, pack) with no price unit is priced per piece. Null when it cannot be
    /// worked out, so a document never states a wrong amount.
    /// </summary>
    public static decimal? LineCost(decimal price, decimal quantity, string unit, string priceUnit)
    {
        if (!string.IsNullOrWhiteSpace(priceUnit))
            return UomConverter.LineValue(price, quantity, unit, priceUnit);
        var perPiece = price * quantity;
        return IsCountedUnit(unit) && perPiece > 0 ? perPiece : null;
    }

    /// <summary>The currency as a supplier's documents name it: its symbol, else its name.</summary>
    public static string Currency(string symbol, string name) =>
        !string.IsNullOrWhiteSpace(symbol) ? symbol.Trim() : string.IsNullOrWhiteSpace(name) ? null : name.Trim();

    private static bool IsCountedUnit(string symbol) =>
        !string.IsNullOrWhiteSpace(symbol) && FamilyOf(symbol.Trim()) is null;

    private static (string Symbol, decimal Factor)[] FamilyOf(string symbol) =>
        Families.FirstOrDefault(steps => steps.Any(step => step.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)));
}
