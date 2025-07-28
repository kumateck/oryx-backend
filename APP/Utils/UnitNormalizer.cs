namespace APP.Utils;

public static class UnitNormalizer
{
    private static readonly Dictionary<string, List<(string Symbol, decimal Factor)>> UnitScales = new()
    {
        ["mg"] = new List<(string, decimal)>
        {
            ("kg", 1_000_000),
            ("g", 1_000),
            ("mg", 1)
        },
        ["ml"] = new List<(string, decimal)>
        {
            ("l", 1_000),
            ("ml", 1)
        },
        ["m"] = new List<(string, decimal)>
        {
            ("km", 1_000),
            ("m", 1)
        },
        ["m²"] = new List<(string, decimal)>
        {
            ("m²", 1)
        },
        ["m³"] = new List<(string, decimal)>
        {
            ("m³", 1)
        }
    };

    public static (decimal Value, string Symbol) GetBestScaled(decimal quantity, string symbol)
    {
        if (!UnitScales.TryGetValue(symbol.ToLower(), out var scales))
            return (quantity, symbol); // Fallback

        foreach (var (scaledSymbol, factor) in scales)
        {
            if (quantity >= factor)
            {
                return (Math.Round(quantity / factor, 2), scaledSymbol);
            }
        }

        return (quantity, symbol);
    }

    public static bool IsScalable(string symbol) => UnitScales.ContainsKey(symbol.ToLower());
}