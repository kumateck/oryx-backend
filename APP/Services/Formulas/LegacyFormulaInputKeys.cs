using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

#nullable enable
namespace APP.Services.Formulas;

/// <summary>Input aliases share the configured weight values in formula repairs.</summary>
internal static class LegacyFormulaInputKeys
{
    private static string Text(JsonObject value, string key) => LegacyFormulaRepairDefinition.Text(value, key);
    private static string Canonical(string key) => key.Equals("wt", StringComparison.OrdinalIgnoreCase) ? "weight" : key;
    private static string Suffix(string key)
    {
        var canonical = Canonical(key);
        return canonical.Length == 0 ? "" : char.ToUpperInvariant(canonical[0]) + canonical[1..];
    }
    private static IEnumerable<string> Keys(JsonArray? inputs) => inputs?.OfType<JsonObject>()
        .Select(input => Text(input, "key")) ?? [];
    private static JsonArray? Inputs(JsonObject config, int? sample) => sample is null
        ? config["standardInputs"] as JsonArray
        : (config["sampleInputs"] as JsonArray)?.ElementAtOrDefault(sample.Value - 1) as JsonArray;

    private static readonly string[] MeasurementAliases = ["absorbance", "peakAreas", "titreValue"];

    private static string? WeightKey(JsonObject config, int? sample) =>
        Keys(Inputs(config, sample)).FirstOrDefault(key => Canonical(key).Equals("weight", StringComparison.OrdinalIgnoreCase));

    private static bool Disabled(JsonObject config, string key) =>
        config[key] is JsonValue value && value.TryGetValue<bool>(out var enabled) && !enabled;

    private static bool ConcentrationEnabled(JsonObject config) =>
        LegacyFormulaRepairDefinition.Number(config, "variableKeyFormat", 0) == 1 &&
        Text(config, "concentrationUnit") is "mg/mL" or "%" &&
        !Keys(Inputs(config, null)).Concat((config["sampleInputs"] as JsonArray ?? [])
            .OfType<JsonArray>().SelectMany(Keys)).Any(key => Canonical(key).Equals("conc", StringComparison.OrdinalIgnoreCase));

    public static string Normalize(JsonObject config, string expression)
    {
        if (LegacyFormulaRepairDefinition.Number(config, "variableKeyFormat", 0) != 1) return expression;
        var standard = WeightKey(config, null);
        var firstSample = WeightKey(config, 1);
        if (ConcentrationEnabled(config))
        {
            if (standard is not null) expression = Regex.Replace(expression, @":standardConc\b", ":standard" + Suffix(standard));
            if (firstSample is not null) expression = Regex.Replace(expression, @":sample(\d*)Conc\b",
                match => ":sample" + match.Groups[1].Value + Suffix(firstSample));
        }

        if (config["measurementAliases"] is not JsonArray selected) return expression;
        var standardKeys = Keys(Inputs(config, null)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sampleKeys = (config["sampleInputs"] as JsonArray ?? [])
            .OfType<JsonArray>().SelectMany(Keys).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in MeasurementAliases)
        {
            if (!selected.OfType<JsonValue>().Any(value => value.TryGetValue<string>(out var key) && key == alias)) continue;
            var suffix = Suffix(alias);
            if (standard is not null && !Disabled(config, "standardInput") && !Disabled(config, "standardInTable") && !standardKeys.Contains(alias))
                expression = Regex.Replace(expression, ":standard" + suffix + @"\b", ":standard" + Suffix(standard));
            if (firstSample is not null && !Disabled(config, "sampleInput") && !sampleKeys.Contains(alias))
                expression = Regex.Replace(expression, @":sample([0-9]*)" + suffix + @"\b", match =>
                {
                    var index = match.Groups[1].Value;
                    if (index.Length == 0) return ":sample" + Suffix(firstSample);
                    if (!int.TryParse(index, NumberStyles.None, CultureInfo.InvariantCulture, out var sample) || sample < 1)
                        return match.Value;
                    var weight = WeightKey(config, sample);
                    return weight is null ? match.Value : ":sample" + sample + Suffix(weight);
                });
        }
        return expression;
    }

    public static string Expand(JsonObject config, string expression, int sample)
    {
        if (LegacyFormulaRepairDefinition.Number(config, "variableKeyFormat", 0) != 1) return expression;
        foreach (var key in Keys(Inputs(config, sample)))
            expression = Regex.Replace(expression, ":sample" + Regex.Escape(Suffix(key)) + @"\b", ":sample" + sample + Suffix(key));
        return expression;
    }

    public static bool TryBinding(JsonObject config, string key, out int? sample, out string inputKey)
    {
        sample = null; inputKey = "";
        if (LegacyFormulaRepairDefinition.Number(config, "variableKeyFormat", 0) != 1) return false;
        var match = Regex.Match(key, @"^(standard|sample(\d*))([A-Z][A-Za-z0-9]*)$");
        if (!match.Success) return false;
        if (match.Groups[1].Value != "standard")
        {
            if (match.Groups[2].Value.Length == 0) sample = 1;
            else if (int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0) sample = number;
            else return false;
        }
        var found = Keys(Inputs(config, sample)).FirstOrDefault(candidate => Suffix(candidate) == match.Groups[3].Value);
        if (found is null) return false;
        inputKey = found;
        return true;
    }
}
