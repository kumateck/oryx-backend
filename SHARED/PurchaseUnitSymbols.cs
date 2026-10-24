namespace SHARED;

/// <summary>Canonical purchase/price symbols, matching the client quantity families.</summary>
public static class PurchaseUnitSymbols
{
    public static string Normalize(string value) => value?.Trim().ToLowerInvariant() switch
    {
        "mg" => "mg", "g" => "g", "kg" => "kg", "ml" => "ml", "l" => "L",
        _ => value?.Trim(),
    };

    public static List<string> Expand(IEnumerable<string> symbols) => symbols
        .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
        .SelectMany(symbol => Normalize(symbol) switch
        {
            "mg" or "g" or "kg" => new[] { "mg", "g", "kg" },
            "ml" or "L" => new[] { "ml", "L" },
            var counted => new[] { counted },
        }).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
